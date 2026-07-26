using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface IAiAgentRepository
{
    Task<List<AiAgent>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<AiAgent?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<AiAgent> CreateAsync(AiAgent agent, CancellationToken ct = default);
    Task<AiAgent> UpdateAsync(AiAgent agent, CancellationToken ct = default);
    Task DeleteAsync(AiAgent agent, CancellationToken ct = default);
    Task<AiAgent?> ToggleActiveAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    /// <summary>Numar de conversatii per agent, intr-un singur query.</summary>
    Task<Dictionary<Guid, int>> GetConversationCountsAsync(
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>Numar de leaduri per agent, prin conversatia din care provin.</summary>
    Task<Dictionary<Guid, int>> GetLeadCountsAsync(
        Guid tenantId,
        CancellationToken ct = default);
}

public class AiAgentRepository : IAiAgentRepository
{
    private readonly AppDbContext _db;

    public AiAgentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<AiAgent>> GetAllAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.AiAgents
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public Task<AiAgent?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.AiAgents.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);

    public async Task<AiAgent> CreateAsync(AiAgent agent, CancellationToken ct = default)
    {
        _db.AiAgents.Add(agent);
        await _db.SaveChangesAsync(ct);
        return agent;
    }

    public async Task<AiAgent> UpdateAsync(AiAgent agent, CancellationToken ct = default)
    {
        _db.AiAgents.Update(agent);
        await _db.SaveChangesAsync(ct);
        return agent;
    }

    public async Task DeleteAsync(AiAgent agent, CancellationToken ct = default)
    {
        _db.AiAgents.Remove(agent);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AiAgent?> ToggleActiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await GetByIdAsync(id, tenantId, ct);
        if (agent is null) return null;

        agent.IsActive = !agent.IsActive;
        await _db.SaveChangesAsync(ct);
        return agent;
    }

    public async Task<Dictionary<Guid, int>> GetConversationCountsAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var rows = await _db.Conversations
            .Where(c => c.TenantId == tenantId && c.AiAgentId != null)
            .GroupBy(c => c.AiAgentId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(row => row.AgentId, row => row.Count);
    }

    public async Task<Dictionary<Guid, int>> GetLeadCountsAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        // leads nu are ai_agent_id, deci trecem prin conversatie
        var rows = await _db.Leads
            .Where(l => l.TenantId == tenantId && l.Conversation!.AiAgentId != null)
            .GroupBy(l => l.Conversation!.AiAgentId!.Value)
            .Select(g => new { AgentId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(row => row.AgentId, row => row.Count);
    }
}
