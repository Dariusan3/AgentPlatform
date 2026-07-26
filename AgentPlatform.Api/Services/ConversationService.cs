using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;

namespace AgentPlatform.Api.Services;

public interface IConversationService
{
    Task<List<ConversationResponseDto>> GetAllAsync(
        Guid tenantId,
        CancellationToken ct = default);
    Task<ConversationResponseDto> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);
    Task<ConversationResponseDto> CloseAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);
    Task<LeadResponseDto> ConvertToLeadAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>Simuleaza un mesaj primit si raspunsul agentului.</summary>
    Task<SimulateResultDto> SimulateInboundAsync(
        SimulateMessageDto dto,
        Guid tenantId,
        CancellationToken ct = default);
}

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversations;
    private readonly ILeadRepository _leads;
    private readonly IAiAgentRepository _agents;
    private readonly IPropertyRepository _properties;
    private readonly IAiReplyService _ai;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository conversations,
        ILeadRepository leads,
        IAiAgentRepository agents,
        IPropertyRepository properties,
        IAiReplyService ai,
        ILogger<ConversationService> logger)
    {
        _conversations = conversations;
        _leads = leads;
        _agents = agents;
        _properties = properties;
        _ai = ai;
        _logger = logger;
    }

    public async Task<List<ConversationResponseDto>> GetAllAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var conversations = await _conversations.GetAllAsync(tenantId, ct);

        // Lista are nevoie doar de ultimul mesaj pentru previzualizare
        var lastMessages = await _conversations.GetLastMessagesAsync(
            conversations.Select(c => c.Id).ToList(),
            ct);

        return conversations
            .Select(conversation =>
            {
                var dto = ConversationResponseDto.From(conversation, includeMessages: false);
                if (lastMessages.TryGetValue(conversation.Id, out var message))
                {
                    dto.Messages = [MessageResponseDto.From(message)];
                }
                return dto;
            })
            .ToList();
    }

    public async Task<ConversationResponseDto> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var conversation = await _conversations.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Conversation();

        var lead = await _leads.GetByConversationAsync(id, tenantId, ct);

        return ConversationResponseDto.From(conversation, includeMessages: true, lead);
    }

    public async Task<ConversationResponseDto> CloseAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var conversation = await _conversations.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Conversation();

        if (conversation.Status == "closed")
        {
            throw new ValidationException("Conversația este deja închisă.");
        }

        conversation.Status = "closed";
        var updated = await _conversations.UpdateAsync(conversation, ct);

        return ConversationResponseDto.From(updated);
    }

    /// <summary>
    /// Creeaza leadul din conversatie si marcheaza conversatia drept convertita.
    /// Idempotent: daca leadul exista deja, il returneaza fara sa creeze altul.
    /// </summary>
    public async Task<LeadResponseDto> ConvertToLeadAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var conversation = await _conversations.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Conversation();

        var existing = await _leads.GetByConversationAsync(id, tenantId, ct);
        if (existing is not null)
        {
            return LeadResponseDto.From(existing);
        }

        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConversationId = conversation.Id,
            Name = conversation.ContactName,
            Phone = conversation.ContactPhone,
            Status = "new",
            CreatedAt = DateTime.UtcNow,
        };

        var created = await _leads.CreateAsync(lead, ct);

        conversation.Status = "converted";
        await _conversations.UpdateAsync(conversation, ct);

        return LeadResponseDto.From(created);
    }

    public async Task<SimulateResultDto> SimulateInboundAsync(
        SimulateMessageDto dto,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(dto.AiAgentId, tenantId, ct)
            ?? throw NotFoundException.AiAgent();

        if (!agent.IsActive)
        {
            throw new ValidationException(
                $"Agentul „{agent.Name}” e oprit, deci nu răspunde. " +
                "Pornește-l din pagina Agenți AI și încearcă din nou.");
        }

        var phone = dto.ContactPhone.Trim();

        // Firul se continua daca exista deja o conversatie deschisa cu acest contact
        var conversation =
            await _conversations.GetByContactAsync(tenantId, agent.Id, phone, ct)
            ?? await _conversations.CreateAsync(
                new Conversation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AiAgentId = agent.Id,
                    ContactPhone = phone,
                    ContactName = dto.ContactName?.Trim(),
                    Channel = "whatsapp",
                    Status = "active",
                    LeadScore = 0,
                    StartedAt = DateTime.UtcNow,
                },
                ct);

        await _conversations.AddMessageAsync(
            new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Role = "user",
                Content = dto.Message.Trim(),
                CreatedAt = DateTime.UtcNow,
            },
            ct);

        var result = new SimulateResultDto();

        try
        {
            var properties = await _properties.GetAllAsync(tenantId, ct);
            var history = await _conversations.GetByIdAsync(conversation.Id, tenantId, ct);

            var reply = await _ai.GenerateAsync(
                agent,
                properties,
                history?.Messages.OrderBy(m => m.CreatedAt).ToList() ?? [],
                ct);

            await _conversations.AddMessageAsync(
                new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = reply,
                    CreatedAt = DateTime.UtcNow,
                },
                ct);

            result.AiReply = reply;
        }
        catch (Exception exception)
        {
            // Prindem TOT, nu doar ValidationException: o eroare de retea sau un
            // timeout ar da 500, iar Twilio ar reincerca si ar dubla mesajul primit.
            // Mesajul ramane salvat - a ajuns, chiar daca raspunsul a eșuat.
            _logger.LogError(
                exception,
                "Generarea raspunsului a eșuat pentru conversatia {Id}",
                conversation.Id);

            result.ReplyError = exception switch
            {
                ValidationException validation => validation.Message,
                TaskCanceledException or OperationCanceledException =>
                    "Modelul nu a răspuns la timp. Încearcă din nou.",
                HttpRequestException =>
                    "Nu am putut contacta serviciul AI. Verifică Groq:Endpoint și conexiunea.",
                _ => "Generarea răspunsului a eșuat. Detaliile sunt în logurile serverului.",
            };
        }

        var updated = await _conversations.GetByIdAsync(conversation.Id, tenantId, ct);
        var lead = await _leads.GetByConversationAsync(conversation.Id, tenantId, ct);

        result.Conversation = ConversationResponseDto.From(updated!, true, lead);
        return result;
    }
}
