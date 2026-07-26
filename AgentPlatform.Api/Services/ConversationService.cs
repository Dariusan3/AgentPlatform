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
}

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversations;
    private readonly ILeadRepository _leads;

    public ConversationService(
        IConversationRepository conversations,
        ILeadRepository leads)
    {
        _conversations = conversations;
        _leads = leads;
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
}
