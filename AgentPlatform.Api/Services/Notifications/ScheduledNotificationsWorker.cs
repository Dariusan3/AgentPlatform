using AgentPlatform.Api.Data;
using AgentPlatform.Api.Repositories;

namespace AgentPlatform.Api.Services.Notifications;

/// <summary>
/// Notificarile care nu au un eveniment care sa le declanseze: raportul
/// saptamanal si alerta de utilizare.
/// </summary>
/// <remarks>
/// Nu tine minte cand a rulat ultima oara. Intreaba tabela de notificari daca
/// raportul saptamanii curente exista deja — asa repornirea serverului,
/// rularea in paralel pe doua instante sau o pauza de cateva ore nu produc
/// duplicate si nu sar peste nimic.
/// </remarks>
public class ScheduledNotificationsWorker : BackgroundService
{
    /// <summary>Cat de des verificam. Raportul e orar ca granularitate, nu la minut.</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    /// <summary>Ora de luni la care pleaca raportul, ora Romaniei.</summary>
    private const int WeeklyReportHour = 8;

    /// <summary>Procentul din limita de la care avertizam.</summary>
    private const int UsageWarningPercent = 80;

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ScheduledNotificationsWorker> _logger;

    public ScheduledNotificationsWorker(
        IServiceScopeFactory scopes,
        ILogger<ScheduledNotificationsWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Intarziere la pornire: la `dotnet watch` serverul reporneste des, iar
        // o verificare la fiecare repornire ar interoga DB-ul degeaba.
        await Task.Delay(TimeSpan.FromSeconds(30), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Bucla trebuie sa supravietuiasca oricarei erori, altfel
                // rapoartele se opresc definitiv pana la urmatoarea repornire
                _logger.LogError(exception, "Verificarea notificarilor programate a eșuat");
            }

            try
            {
                await Task.Delay(CheckInterval, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Un ciclu complet. Publica, ca sa poata fi declansat si din teste.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var agents = scope.ServiceProvider.GetRequiredService<IAgentRepository>();

        var tenantIds = await agents.GetAllTenantIdsAsync(ct);

        foreach (var tenantId in tenantIds)
        {
            // Scop separat per cont: serviciile citesc tenantul din context, iar
            // AppDbContext acumuleaza entitati urmarite daca il refolosim.
            using var tenantScope = _scopes.CreateScope();
            var provider = tenantScope.ServiceProvider;
            provider.GetRequiredService<ITenantContextSetter>().Override(tenantId);

            try
            {
                await CheckWeeklyReportAsync(provider, tenantId, ct);
                await CheckUsageAsync(provider, tenantId, ct);
            }
            catch (Exception exception)
            {
                // Un cont cu date stricate nu trebuie sa blocheze celelalte conturi
                _logger.LogError(
                    exception, "Notificarile programate au eșuat pentru {TenantId}", tenantId);
            }
        }
    }

    private async Task CheckWeeklyReportAsync(
        IServiceProvider provider,
        Guid tenantId,
        CancellationToken ct)
    {
        var now = RomanianTime.Now;
        if (now.DayOfWeek != DayOfWeek.Monday || now.Hour < WeeklyReportHour) return;

        var repository = provider.GetRequiredService<INotificationRepository>();
        var weekStart = RomanianTime.ToUtc(now.Date.AddDays(-(int)now.DayOfWeek + 1));

        var alreadySent = await repository.CountSinceAsync(
            tenantId, NotificationTypes.WeeklyReport, weekStart, ct);
        if (alreadySent > 0) return;

        var dashboard = provider.GetRequiredService<IDashboardService>();
        var stats = await dashboard.GetStatsAsync(tenantId, ct);

        var conversations = stats.WeeklyData.Sum(day => day.Conversations);
        var leads = stats.WeeklyData.Sum(day => day.Leads);

        var body = conversations == 0
            ? "Săptămâna trecută nu a scris nimeni. Verifică dacă agenții sunt porniți."
            : $"{Plural(conversations, "conversație", "conversații")} și " +
              $"{Plural(leads, "lead nou", "leaduri noi")} în ultimele 7 zile.";

        await provider.GetRequiredService<INotificationService>().NotifyAsync(
            tenantId,
            NotificationTypes.WeeklyReport,
            "Raportul săptămânii",
            body,
            "/dashboard",
            "info",
            ct);
    }

    private async Task CheckUsageAsync(
        IServiceProvider provider,
        Guid tenantId,
        CancellationToken ct)
    {
        var usage = await provider.GetRequiredService<ISettingsService>()
            .GetUsageAsync(tenantId, ct);

        // Planurile nelimitate nu au prag de depasit
        if (usage.MessagesLimit is not { } limit || limit <= 0) return;

        var percent = usage.MessagesCount * 100 / limit;
        if (percent < UsageWarningPercent) return;

        // O singura alerta pe luna: altfel ai primi una la fiecare 15 minute
        var repository = provider.GetRequiredService<INotificationRepository>();
        var monthStart = RomanianTime.ToUtc(
            new DateTime(RomanianTime.Now.Year, RomanianTime.Now.Month, 1));

        var alreadySent = await repository.CountSinceAsync(
            tenantId, NotificationTypes.UsageThreshold, monthStart, ct);
        if (alreadySent > 0) return;

        await provider.GetRequiredService<INotificationService>().NotifyAsync(
            tenantId,
            NotificationTypes.UsageThreshold,
            $"Ai folosit {percent}% din mesajele lunii",
            $"{usage.MessagesCount} din {limit} mesaje. " +
            "Când limita e atinsă, agenții nu mai răspund până luna viitoare.",
            "/dashboard/settings",
            "warning",
            ct);
    }

    private static string Plural(int count, string singular, string plural) =>
        count == 1 ? $"1 {singular}" : $"{count} {plural}";
}

/// <summary>
/// Ora Romaniei, nu ora serverului.
/// </summary>
/// <remarks>
/// „Luni la 8” inseamna 8 dimineata pentru agent. Un server in UTC ar trimite
/// raportul la 11 noaptea duminica, vara.
/// </remarks>
public static class RomanianTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    private static TimeZoneInfo ResolveZone()
    {
        // Identificatorul difera intre Linux/macOS si Windows
        foreach (var id in new[] { "Europe/Bucharest", "GTB Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Incercam urmatorul identificator
            }
        }

        return TimeZoneInfo.Utc;
    }
}
