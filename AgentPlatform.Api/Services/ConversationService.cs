using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services.Notifications;

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
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    /// <summary>Simuleaza un mesaj primit si raspunsul agentului.</summary>
    Task<SimulateResultDto> SimulateInboundAsync(
        SimulateMessageDto dto,
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Un mesaj venit efectiv pe WhatsApp. Intoarce textul de trimis inapoi,
    /// sau null daca nu s-a putut genera niciun raspuns.
    /// </summary>
    Task<string?> HandleInboundWhatsAppAsync(
        AiAgent agent,
        string contactPhone,
        string? contactName,
        string message,
        CancellationToken ct = default);
}

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversations;
    private readonly ILeadRepository _leads;
    private readonly IAiAgentRepository _agents;
    private readonly IPropertyRepository _properties;
    private readonly IAiReplyService _ai;
    private readonly INotificationService _notifications;
    private readonly ILogger<ConversationService> _logger;

    public ConversationService(
        IConversationRepository conversations,
        ILeadRepository leads,
        IAiAgentRepository agents,
        IPropertyRepository properties,
        IAiReplyService ai,
        ILogger<ConversationService> logger,
        INotificationService notifications)
    {
        _conversations = conversations;
        _leads = leads;
        _agents = agents;
        _properties = properties;
        _ai = ai;
        _logger = logger;
        _notifications = notifications;
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

        await _notifications.NotifyAsync(
            tenantId,
            NotificationTypes.LeadQualified,
            "Lead nou",
            $"{created.Name ?? created.Phone} a devenit lead dintr-o conversație WhatsApp.",
            $"/dashboard/leads",
            "success",
            ct);

        return LeadResponseDto.From(created);
    }

    /// <remarks>
    /// Sterge definitiv conversatia si mesajele ei. Leadul creat din ea ramane,
    /// dar pierde legatura cu firul de discutie (FK e ON DELETE SET NULL).
    /// </remarks>
    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var conversation = await _conversations.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Conversation();

        await _conversations.DeleteAsync(conversation, ct);
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

        var (conversation, reply, error) = await ProcessInboundAsync(
            agent, dto.ContactPhone, dto.ContactName, dto.Message, ct);

        var updated = await _conversations.GetByIdAsync(conversation.Id, tenantId, ct);
        var lead = await _leads.GetByConversationAsync(conversation.Id, tenantId, ct);

        return new SimulateResultDto
        {
            Conversation = ConversationResponseDto.From(updated!, true, lead),
            AiReply = reply,
            ReplyError = error,
        };
    }

    public async Task<string?> HandleInboundWhatsAppAsync(
        AiAgent agent,
        string contactPhone,
        string? contactName,
        string message,
        CancellationToken ct = default)
    {
        var (_, reply, error) = await ProcessInboundAsync(
            agent, contactPhone, contactName, message, ct);

        // Twilio nu are ce face cu eroarea; o vedem in loguri si in conversatie
        return error is null ? reply : null;
    }

    /// <summary>
    /// Drumul comun: salveaza mesajul primit, genereaza raspunsul, il salveaza.
    /// Simulatorul si webhookul trec amandoua prin aici, deci testul local
    /// verifica exact codul care va rula in producție.
    /// </summary>
    private async Task<(Conversation Conversation, string? Reply, string? Error)>
        ProcessInboundAsync(
            AiAgent agent,
            string contactPhone,
            string? contactName,
            string message,
            CancellationToken ct)
    {
        // Forma canonica, ca acelasi client sa nu capete doua conversatii
        var phone = PhoneNumber.Normalize(contactPhone);
        var tenantId = agent.TenantId;

        var conversation =
            await _conversations.GetByContactAsync(tenantId, agent.Id, phone, ct);
        var isNewContact = conversation is null;

        conversation ??= await _conversations.CreateAsync(
            new Conversation
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AiAgentId = agent.Id,
                ContactPhone = phone,
                ContactName = contactName?.Trim(),
                Channel = "whatsapp",
                Status = "active",
                LeadScore = 0,
                StartedAt = DateTime.UtcNow,
            },
            ct);

        if (isNewContact)
        {
            await _notifications.NotifyAsync(
                tenantId,
                NotificationTypes.ConversationStarted,
                "Conversație nouă",
                $"{contactName?.Trim() ?? phone} i-a scris agentului {agent.Name}.",
                "/dashboard/conversations",
                "info",
                ct);
        }

        await _conversations.AddMessageAsync(
            new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Role = "user",
                Content = message.Trim(),
                CreatedAt = DateTime.UtcNow,
            },
            ct);

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

            return (conversation, reply, null);
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

            await _notifications.NotifyAsync(
                tenantId,
                NotificationTypes.IntegrationFailure,
                "Un mesaj a rămas fără răspuns",
                $"Agentul {agent.Name} nu a putut răspunde lui {phone}. " +
                "Clientul așteaptă — verifică logurile.",
                "/dashboard/conversations",
                "error",
                ct);

            return (conversation, null, exception switch
            {
                ValidationException validation => validation.Message,
                TaskCanceledException or OperationCanceledException =>
                    "Modelul nu a răspuns la timp. Încearcă din nou.",
                HttpRequestException =>
                    "Nu am putut contacta serviciul AI. Verifică Groq:Endpoint și conexiunea.",
                _ => "Generarea răspunsului a eșuat. Detaliile sunt în logurile serverului.",
            });
        }
    }
}
