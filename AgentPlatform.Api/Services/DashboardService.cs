using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Repositories;

namespace AgentPlatform.Api.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(Guid tenantId, CancellationToken ct = default);
}

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboard;
    private readonly IConversationRepository _conversations;
    private readonly IPropertyRepository _properties;

    public DashboardService(
        IDashboardRepository dashboard,
        IConversationRepository conversations,
        IPropertyRepository properties)
    {
        _dashboard = dashboard;
        _conversations = conversations;
        _properties = properties;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        // Sunt query-uri independente: le pornim in paralel, pe conexiuni separate
        // ar fi ideal, dar DbContext nu e thread-safe, deci le facem secvential.
        var activeConversations = await _conversations.CountActiveAsync(tenantId, ct);
        var newLeads = await _dashboard.CountLeadsSinceAsync(
            tenantId,
            DateTime.UtcNow.Date.AddDays(-7),
            ct);
        var totalProperties = await _properties.CountAsync(tenantId, ct);
        var conversionRate = await _dashboard.GetConversionRateAsync(tenantId, ct);
        var weeklyData = await _dashboard.GetWeeklyDataAsync(tenantId, ct);
        var recent = await _conversations.GetRecentAsync(tenantId, 5, ct);

        return new DashboardStatsDto
        {
            ActiveConversations = activeConversations,
            NewLeads = newLeads,
            TotalProperties = totalProperties,
            ConversionRate = conversionRate,
            WeeklyData = weeklyData,
            RecentConversations = recent
                .Select(c => ConversationResponseDto.From(c, includeMessages: false))
                .ToList(),
        };
    }
}
