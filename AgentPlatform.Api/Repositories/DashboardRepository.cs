using AgentPlatform.Api.Data;
using AgentPlatform.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface IDashboardRepository
{
    Task<List<WeeklyDataPoint>> GetWeeklyDataAsync(
        Guid tenantId,
        CancellationToken ct = default);
    Task<decimal> GetConversionRateAsync(Guid tenantId, CancellationToken ct = default);
    Task<int> CountLeadsSinceAsync(Guid tenantId, DateTime since, CancellationToken ct = default);
}

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _db;

    public DashboardRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Ultimele 7 zile, inclusiv cele fara activitate. Gruparea se face in DB,
    /// completarea zilelor lipsa in memorie - altfel ar lipsi zilele goale.
    /// </summary>
    public async Task<List<WeeklyDataPoint>> GetWeeklyDataAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-6);

        var conversationsByDay = await _db.Conversations
            .Where(c => c.TenantId == tenantId && c.StartedAt >= from)
            .GroupBy(c => c.StartedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var leadsByDay = await _db.Leads
            .Where(l => l.TenantId == tenantId && l.CreatedAt >= from)
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var conversations = conversationsByDay.ToDictionary(x => x.Day, x => x.Count);
        var leads = leadsByDay.ToDictionary(x => x.Day, x => x.Count);

        return Enumerable
            .Range(0, 7)
            .Select(offset =>
            {
                var day = from.AddDays(offset);
                return new WeeklyDataPoint
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    Conversations = conversations.GetValueOrDefault(day),
                    Leads = leads.GetValueOrDefault(day),
                };
            })
            .ToList();
    }

    public async Task<decimal> GetConversionRateAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var total = await _db.Conversations.CountAsync(c => c.TenantId == tenantId, ct);
        if (total == 0) return 0;

        var converted = await _db.Conversations.CountAsync(
            c => c.TenantId == tenantId && c.Status == "converted",
            ct);

        return Math.Round((decimal)converted * 100 / total, 1);
    }

    public Task<int> CountLeadsSinceAsync(
        Guid tenantId,
        DateTime since,
        CancellationToken ct = default) =>
        _db.Leads.CountAsync(l => l.TenantId == tenantId && l.CreatedAt >= since, ct);
}
