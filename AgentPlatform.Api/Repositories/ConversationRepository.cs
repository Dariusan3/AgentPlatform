using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface IConversationRepository
{
    Task<List<Conversation>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<Conversation?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<Conversation>> GetRecentAsync(
        Guid tenantId,
        int count = 5,
        CancellationToken ct = default);
    Task<int> CountActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, CancellationToken ct = default);
    Task<Conversation> UpdateAsync(Conversation conversation, CancellationToken ct = default);

    /// <summary>Ultimul mesaj al fiecarei conversatii, pentru liste.</summary>
    Task<Dictionary<Guid, Message>> GetLastMessagesAsync(
        IReadOnlyCollection<Guid> conversationIds,
        CancellationToken ct = default);
}

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _db;

    public ConversationRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <remarks>
    /// Lista nu include mesajele: cu zeci de conversatii lungi ar fi un query
    /// enorm pentru ceva ce frontendul nu afiseaza in lista.
    /// </remarks>
    public Task<List<Conversation>> GetAllAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Conversations
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.LastMessageAt ?? c.StartedAt)
            .ToListAsync(ct);

    public Task<Conversation?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default) =>
        _db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

    public Task<List<Conversation>> GetRecentAsync(
        Guid tenantId,
        int count = 5,
        CancellationToken ct = default) =>
        _db.Conversations
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.LastMessageAt ?? c.StartedAt)
            .Take(count)
            .ToListAsync(ct);

    public Task<int> CountActiveAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Conversations.CountAsync(
            c => c.TenantId == tenantId && c.Status == "active",
            ct);

    public Task<int> CountAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Conversations.CountAsync(c => c.TenantId == tenantId, ct);

    public async Task<Conversation> UpdateAsync(
        Conversation conversation,
        CancellationToken ct = default)
    {
        _db.Conversations.Update(conversation);
        await _db.SaveChangesAsync(ct);
        return conversation;
    }

    public async Task<Dictionary<Guid, Message>> GetLastMessagesAsync(
        IReadOnlyCollection<Guid> conversationIds,
        CancellationToken ct = default)
    {
        if (conversationIds.Count == 0) return [];

        var rows = await _db.Messages
            .Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.CreatedAt).First())
            .ToListAsync(ct);

        return rows.ToDictionary(message => message.ConversationId);
    }
}
