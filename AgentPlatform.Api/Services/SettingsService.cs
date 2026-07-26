using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Repositories;

namespace AgentPlatform.Api.Services;

public interface ISettingsService
{
    Task<ProfileResponseDto> GetProfileAsync(Guid tenantId, CancellationToken ct = default);
    Task<ProfileResponseDto> UpdateProfileAsync(
        ProfileUpdateDto dto,
        Guid tenantId,
        CancellationToken ct = default);
    Task<UsageResponseDto> GetUsageAsync(Guid tenantId, CancellationToken ct = default);
    Task UpdatePasswordAsync(
        Guid tenantId,
        string newPassword,
        CancellationToken ct = default);
}

public class SettingsService : ISettingsService
{
    /// <summary>Limitele din pagina de prețuri, ca frontendul sa nu le dubleze.</summary>
    private static readonly Dictionary<string, (int? Messages, int? Leads)> PlanLimits = new()
    {
        ["starter"] = (500, 100),
        ["pro"] = (null, 500),
        ["agency"] = (null, null),
    };

    private readonly IAgentRepository _agents;
    private readonly ISupabaseAdminClient _supabase;

    public SettingsService(IAgentRepository agents, ISupabaseAdminClient supabase)
    {
        _agents = agents;
        _supabase = supabase;
    }

    public async Task<ProfileResponseDto> GetProfileAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(tenantId, ct)
            ?? throw NotFoundException.Account();

        return ProfileResponseDto.From(agent);
    }

    public async Task<ProfileResponseDto> UpdateProfileAsync(
        ProfileUpdateDto dto,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(tenantId, ct)
            ?? throw NotFoundException.Account();

        // Emailul nu se schimba de aici: e adresa de login, gestionata de Supabase Auth
        agent.FullName = dto.FullName?.Trim();
        agent.CompanyName = dto.CompanyName?.Trim();
        agent.Phone = dto.Phone?.Trim();

        var updated = await _agents.UpdateAsync(agent, ct);
        return ProfileResponseDto.From(updated);
    }

    public async Task<UsageResponseDto> GetUsageAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(tenantId, ct)
            ?? throw NotFoundException.Account();

        var month = DateTime.UtcNow.ToString("yyyy-MM");
        var usage = await _agents.GetUsageAsync(tenantId, month, ct);
        var limits = PlanLimits.GetValueOrDefault(agent.Plan, PlanLimits["starter"]);

        return new UsageResponseDto
        {
            Month = month,
            // Luna fara activitate nu are rand in usage_metrics; zero e raspunsul corect
            MessagesCount = usage?.MessagesCount ?? 0,
            TokensUsed = usage?.TokensUsed ?? 0,
            LeadsGenerated = usage?.LeadsGenerated ?? 0,
            MessagesLimit = limits.Messages,
            LeadsLimit = limits.Leads,
            Plan = agent.Plan,
        };
    }

    public async Task UpdatePasswordAsync(
        Guid tenantId,
        string newPassword,
        CancellationToken ct = default)
    {
        if (newPassword.Length < 6)
        {
            throw ValidationException.ForField(
                "newPassword",
                "Parola trebuie să aibă minim 6 caractere.");
        }

        await _supabase.UpdatePasswordAsync(tenantId, newPassword, ct);
    }
}
