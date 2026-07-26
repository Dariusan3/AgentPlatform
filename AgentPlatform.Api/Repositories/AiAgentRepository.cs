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

    /// <summary>
    /// Agentul care deserveste un numar de WhatsApp, fara sa stim tenantul.
    /// Folosit de webhook: abia de aici afla cui aparține mesajul.
    /// </summary>
    Task<AiAgent?> FindByWhatsAppNumberAsync(
        string whatsAppNumber,
        CancellationToken ct = default);

    /// <summary>Primul agent activ al unui tenant. Ruta de rezerva pentru sandbox.</summary>
    Task<AiAgent?> GetFirstActiveAsync(Guid tenantId, CancellationToken ct = default);
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

    /// <remarks>
    /// IgnoreQueryFilters e obligatoriu: webhookul nu are tenant in context, iar
    /// filtrul global ar returna zero rânduri. Comparam pe cifre, ca „+40 721 118 204"
    /// si „whatsapp:+40721118204" sa se potriveasca.
    /// </remarks>
    public async Task<AiAgent?> FindByWhatsAppNumberAsync(
        string whatsAppNumber,
        CancellationToken ct = default)
    {
        var digits = OnlyDigits(whatsAppNumber);
        if (digits.Length < 6) return null;

        var candidates = await _db.AiAgents
            .IgnoreQueryFilters()
            .Where(a => a.WhatsAppNumber != null)
            .ToListAsync(ct);

        return candidates.FirstOrDefault(a => OnlyDigits(a.WhatsAppNumber!) == digits);
    }

    public Task<AiAgent?> GetFirstActiveAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.AiAgents
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .OrderBy(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

    private static string OnlyDigits(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
