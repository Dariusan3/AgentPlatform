using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface IVoiceAgentRepository
{
    Task<List<VoiceAgent>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<VoiceAgent?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<VoiceAgent> CreateAsync(VoiceAgent agent, CancellationToken ct = default);
    Task<VoiceAgent> UpdateAsync(VoiceAgent agent, CancellationToken ct = default);
    Task DeleteAsync(VoiceAgent agent, CancellationToken ct = default);
    Task<VoiceAgent?> ToggleActiveAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Agentul care raspunde pe un numar Twilio, fara sa stim tenantul.
    /// Webhookul de apel afla de aici cui aparține apelul.
    /// </summary>
    Task<VoiceAgent?> FindByPhoneNumberAsync(string phone, CancellationToken ct = default);

    Task<VoiceAgent?> GetFirstActiveAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Statistici per agent, doua query-uri agregate in loc de N+1.</summary>
    Task<Dictionary<Guid, (int Calls, int Qualified)>> GetStatsAsync(
        Guid tenantId,
        CancellationToken ct = default);
}

public class VoiceAgentRepository : IVoiceAgentRepository
{
    private readonly AppDbContext _db;

    public VoiceAgentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<VoiceAgent>> GetAllAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.VoiceAgents
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public Task<VoiceAgent?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.VoiceAgents.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);

    public async Task<VoiceAgent> CreateAsync(VoiceAgent agent, CancellationToken ct = default)
    {
        _db.VoiceAgents.Add(agent);
        await _db.SaveChangesAsync(ct);
        return agent;
    }

    public async Task<VoiceAgent> UpdateAsync(VoiceAgent agent, CancellationToken ct = default)
    {
        _db.VoiceAgents.Update(agent);
        await _db.SaveChangesAsync(ct);
        return agent;
    }

    public async Task DeleteAsync(VoiceAgent agent, CancellationToken ct = default)
    {
        _db.VoiceAgents.Remove(agent);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<VoiceAgent?> ToggleActiveAsync(
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

    /// <remarks>
    /// IgnoreQueryFilters: webhookul nu are tenant in context, deci filtrul
    /// global ar returna zero. Comparam pe cifre, ca formatarile sa nu conteze.
    /// </remarks>
    public async Task<VoiceAgent?> FindByPhoneNumberAsync(
        string phone,
        CancellationToken ct = default)
    {
        var target = Services.PhoneNumber.Normalize(phone);
        if (target.Length < 6) return null;

        var candidates = await _db.VoiceAgents
            .IgnoreQueryFilters()
            .Where(a => a.TwilioPhoneNumber != null)
            .ToListAsync(ct);

        return candidates.FirstOrDefault(a =>
            Services.PhoneNumber.Normalize(a.TwilioPhoneNumber!) == target);
    }

    public Task<VoiceAgent?> GetFirstActiveAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.VoiceAgents
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .OrderBy(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<Dictionary<Guid, (int Calls, int Qualified)>> GetStatsAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var rows = await _db.VoiceCalls
            .Where(c => c.TenantId == tenantId && c.VoiceAgentId != null)
            .GroupBy(c => c.VoiceAgentId!.Value)
            .Select(g => new
            {
                AgentId = g.Key,
                Calls = g.Count(),
                Qualified = g.Count(c => c.LeadQualified),
            })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.AgentId, r => (r.Calls, r.Qualified));
    }
}
