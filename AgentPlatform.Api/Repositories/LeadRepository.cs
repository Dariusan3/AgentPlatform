using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface ILeadRepository
{
    Task<List<Lead>> GetAllAsync(Guid tenantId, string? status, CancellationToken ct = default);
    Task<Lead?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Lead> CreateAsync(Lead lead, CancellationToken ct = default);
    Task<Lead> UpdateAsync(Lead lead, CancellationToken ct = default);
    Task DeleteAsync(Lead lead, CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, CancellationToken ct = default);
    Task<int> CountByStatusAsync(Guid tenantId, string status, CancellationToken ct = default);
    Task<Lead?> GetByConversationAsync(
        Guid conversationId,
        Guid tenantId,
        CancellationToken ct = default);
}

public class LeadRepository : ILeadRepository
{
    private readonly AppDbContext _db;

    public LeadRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Lead>> GetAllAsync(
        Guid tenantId,
        string? status,
        CancellationToken ct = default)
    {
        var query = _db.Leads.Where(l => l.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        return query.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
    }

    public Task<Lead?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.Leads
            .Include(l => l.Conversation!)
            .ThenInclude(c => c.Messages)
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, ct);

    public async Task<Lead> CreateAsync(Lead lead, CancellationToken ct = default)
    {
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
        return lead;
    }

    public async Task<Lead> UpdateAsync(Lead lead, CancellationToken ct = default)
    {
        _db.Leads.Update(lead);
        await _db.SaveChangesAsync(ct);
        return lead;
    }

    public async Task DeleteAsync(Lead lead, CancellationToken ct = default)
    {
        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Leads.CountAsync(l => l.TenantId == tenantId, ct);

    public Task<int> CountByStatusAsync(
        Guid tenantId,
        string status,
        CancellationToken ct = default) =>
        _db.Leads.CountAsync(l => l.TenantId == tenantId && l.Status == status, ct);

    public Task<Lead?> GetByConversationAsync(
        Guid conversationId,
        Guid tenantId,
        CancellationToken ct = default) =>
        _db.Leads.FirstOrDefaultAsync(
            l => l.ConversationId == conversationId && l.TenantId == tenantId,
            ct);
}
