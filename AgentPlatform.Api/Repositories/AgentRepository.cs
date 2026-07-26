using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<Agent> UpdateAsync(Agent agent, CancellationToken ct = default);
    Task<UsageMetric?> GetUsageAsync(
        Guid tenantId,
        string month,
        CancellationToken ct = default);
}

/// <summary>Tenantul insusi: profil si utilizare. Folosit de SettingsController.</summary>
public class AgentRepository : IAgentRepository
{
    private readonly AppDbContext _db;

    public AgentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Agent?> GetByIdAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Agents.FirstOrDefaultAsync(a => a.Id == tenantId, ct);

    public async Task<Agent> UpdateAsync(Agent agent, CancellationToken ct = default)
    {
        _db.Agents.Update(agent);
        await _db.SaveChangesAsync(ct);
        return agent;
    }

    public Task<UsageMetric?> GetUsageAsync(
        Guid tenantId,
        string month,
        CancellationToken ct = default) =>
        _db.UsageMetrics.FirstOrDefaultAsync(
            u => u.TenantId == tenantId && u.Month == month,
            ct);
}
