namespace AgentPlatform.Api.DTOs;

public class WeeklyDataPoint
{
    /// <summary>Format ISO, "2026-07-26".</summary>
    public string Date { get; set; } = string.Empty;
    public int Conversations { get; set; }
    public int Leads { get; set; }
}

public class DashboardStatsDto
{
    public int ActiveConversations { get; set; }
    public int NewLeads { get; set; }
    public int TotalProperties { get; set; }

    /// <summary>Procent, 0-100, cu o zecimala.</summary>
    public decimal ConversionRate { get; set; }

    public List<WeeklyDataPoint> WeeklyData { get; set; } = [];
    public List<ConversationResponseDto> RecentConversations { get; set; } = [];
}
