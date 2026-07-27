using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification, CancellationToken ct = default);

    Task<List<Notification>> GetAsync(
        Guid tenantId,
        bool unreadOnly,
        int limit,
        CancellationToken ct = default);

    Task<int> CountUnreadAsync(Guid tenantId, CancellationToken ct = default);

    Task<bool> MarkReadAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<int> MarkAllReadAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Preferintele salvate. Tipurile fara rand lipsesc: valorile implicite vin
    /// din catalog, ca un tip nou sa nu necesite migrare.
    /// </summary>
    Task<List<NotificationPreference>> GetPreferencesAsync(
        Guid tenantId,
        CancellationToken ct = default);

    Task UpsertPreferenceAsync(
        NotificationPreference preference,
        CancellationToken ct = default);

    /// <summary>
    /// Cate notificari de tipul dat s-au creat de la un moment incoace.
    /// </summary>
    /// <remarks>
    /// Fara filtru de tenant: sarcinile periodice ruleaza fara utilizator
    /// autentificat si verifica astfel daca raportul a fost deja trimis.
    /// </remarks>
    Task<int> CountSinceAsync(
        Guid tenantId,
        string type,
        DateTime since,
        CancellationToken ct = default);
}

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Notification> CreateAsync(
        Notification notification,
        CancellationToken ct = default)
    {
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);
        return notification;
    }

    public Task<List<Notification>> GetAsync(
        Guid tenantId,
        bool unreadOnly,
        int limit,
        CancellationToken ct = default) =>
        _db.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.TenantId == tenantId)
            .Where(n => !unreadOnly || n.ReadAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task<int> CountUnreadAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Notifications
            .IgnoreQueryFilters()
            .CountAsync(n => n.TenantId == tenantId && n.ReadAt == null, ct);

    public async Task<bool> MarkReadAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        // Conditia pe tenant e in UPDATE, nu intr-un SELECT separat: altfel doua
        // cereri concurente ar putea citi si scrie randul altui cont.
        var affected = await _db.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.Id == id && n.TenantId == tenantId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);

        return affected > 0;
    }

    public Task<int> MarkAllReadAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.TenantId == tenantId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);

    public Task<List<NotificationPreference>> GetPreferencesAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        _db.NotificationPreferences
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task UpsertPreferenceAsync(
        NotificationPreference preference,
        CancellationToken ct = default)
    {
        var existing = await _db.NotificationPreferences
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.TenantId == preference.TenantId && p.Type == preference.Type,
                ct);

        if (existing is null)
        {
            _db.NotificationPreferences.Add(preference);
        }
        else
        {
            existing.InApp = preference.InApp;
            existing.Push = preference.Push;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountSinceAsync(
        Guid tenantId,
        string type,
        DateTime since,
        CancellationToken ct = default) =>
        _db.Notifications
            .IgnoreQueryFilters()
            .CountAsync(
                n => n.TenantId == tenantId && n.Type == type && n.CreatedAt >= since,
                ct);
}

public interface IPushSubscriptionRepository
{
    /// <summary>
    /// Salveaza abonamentul. Intoarce false daca adresa apartine deja altui cont
    /// si cererea nu dovedeste ca vine chiar de la browserul acela.
    /// </summary>
    Task<bool> SaveAsync(PushSubscription subscription, CancellationToken ct = default);

    Task<List<PushSubscription>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Dezabonare ceruta de utilizator. Sterge doar din propriul cont.</summary>
    Task DeleteByEndpointAsync(
        Guid tenantId,
        string endpoint,
        CancellationToken ct = default);

    /// <summary>
    /// Sterge un abonament pe care serviciul de push l-a declarat expirat.
    /// </summary>
    /// <remarks>
    /// Fara tenant: verdictul vine de la Google sau Mozilla, nu de la un
    /// utilizator, iar randul e deja in mana apelantului.
    /// </remarks>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task TouchAsync(Guid id, CancellationToken ct = default);
}

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly AppDbContext _db;

    public PushSubscriptionRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> SaveAsync(
        PushSubscription subscription,
        CancellationToken ct = default)
    {
        // Acelasi browser reabonat trebuie sa inlocuiasca randul vechi, altfel
        // ar primi fiecare notificare de doua ori. Adresa e unica global, deci
        // cautarea nu poate fi limitata la cont.
        var existing = await _db.PushSubscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Endpoint == subscription.Endpoint, ct);

        if (existing is null)
        {
            _db.PushSubscriptions.Add(subscription);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        if (existing.TenantId != subscription.TenantId && !ProvesControl(existing, subscription))
        {
            // Cineva a trimis adresa altui cont fara sa aiba si cheile lui. Mutat,
            // randul ar duce notificarile noastre in browserul aceluia si l-ar
            // lasa pe el fara ale lui.
            return false;
        }

        existing.TenantId = subscription.TenantId;
        existing.P256dh = subscription.P256dh;
        existing.Auth = subscription.Auth;
        existing.UserAgent = subscription.UserAgent;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Decide daca o cerere pentru o adresa deja alocata vine chiar de la
    /// browserul acela.
    /// </summary>
    /// <remarks>
    /// Cazul legitim exista si trebuie sa mearga: pe un calculator comun, al
    /// doilea utilizator care se autentifica primeste de la browser exact acelasi
    /// abonament — aceeasi adresa si aceleasi chei — pentru ca abonamentul tine
    /// de origine, nu de sesiune. Un atacator care a aflat doar adresa nu poate
    /// produce si cheile, deci nu trece de aici.
    /// </remarks>
    private static bool ProvesControl(PushSubscription existing, PushSubscription incoming) =>
        // Comparatie ordinala: cheile sunt base64url, nu text pentru oameni
        string.Equals(existing.P256dh, incoming.P256dh, StringComparison.Ordinal)
        && string.Equals(existing.Auth, incoming.Auth, StringComparison.Ordinal);

    public Task<List<PushSubscription>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken ct = default) =>
        _db.PushSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(ct);

    public Task DeleteByEndpointAsync(
        Guid tenantId,
        string endpoint,
        CancellationToken ct = default) =>
        _db.PushSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.Endpoint == endpoint)
            .ExecuteDeleteAsync(ct);

    public Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        _db.PushSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync(ct);

    public Task TouchAsync(Guid id, CancellationToken ct = default) =>
        _db.PushSubscriptions
            .IgnoreQueryFilters()
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.LastUsedAt, DateTime.UtcNow),
                ct);
}
