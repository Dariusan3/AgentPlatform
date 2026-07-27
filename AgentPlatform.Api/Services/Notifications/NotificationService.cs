using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services.Notifications.WebPush;

namespace AgentPlatform.Api.Services.Notifications;

public interface INotificationService
{
    /// <summary>
    /// Inregistreaza un eveniment si il livreaza pe canalele alese de utilizator.
    /// </summary>
    /// <remarks>
    /// Nu arunca niciodata. E chemata din mijlocul unei conversatii sau al unui
    /// apel, iar o notificare eșuata nu are voie sa rupa lucrul care a
    /// declansat-o — clientul si-ar pierde raspunsul din cauza unui clopotel.
    /// </remarks>
    Task NotifyAsync(
        Guid tenantId,
        string type,
        string title,
        string body,
        string? link = null,
        string severity = "info",
        CancellationToken ct = default);

    Task<NotificationListDto> GetAsync(
        Guid tenantId,
        bool unreadOnly,
        int limit,
        CancellationToken ct = default);

    Task MarkReadAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(Guid tenantId, CancellationToken ct = default);

    Task<List<NotificationPreferenceDto>> GetPreferencesAsync(
        Guid tenantId,
        CancellationToken ct = default);

    Task<List<NotificationPreferenceDto>> UpdatePreferenceAsync(
        Guid tenantId,
        NotificationPreferenceUpdateDto dto,
        CancellationToken ct = default);
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IWebPushService _push;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notifications,
        IPushSubscriptionRepository subscriptions,
        IWebPushService push,
        ILogger<NotificationService> logger)
    {
        _notifications = notifications;
        _subscriptions = subscriptions;
        _push = push;
        _logger = logger;
    }

    public async Task NotifyAsync(
        Guid tenantId,
        string type,
        string title,
        string body,
        string? link = null,
        string severity = "info",
        CancellationToken ct = default)
    {
        try
        {
            if (!NotificationTypes.IsKnown(type))
            {
                // Un tip necunoscut nu ar aparea in setari, deci nu ar putea fi oprit
                _logger.LogError(
                    "Tip de notificare necunoscut: {Type}. Adauga-l in NotificationTypes.",
                    type);
                return;
            }

            var preference = await ResolvePreferenceAsync(tenantId, type, ct);

            if (preference.InApp)
            {
                await _notifications.CreateAsync(
                    new Notification
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Type = type,
                        Title = title,
                        Body = body,
                        Severity = severity,
                        Link = link,
                        CreatedAt = DateTime.UtcNow,
                    },
                    ct);
            }

            if (preference.Push && _push.IsConfigured)
            {
                await SendPushAsync(tenantId, title, body, link, severity, ct);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Notificarea {Type} pentru {TenantId} a eșuat",
                type,
                tenantId);
        }
    }

    private async Task SendPushAsync(
        Guid tenantId,
        string title,
        string body,
        string? link,
        string severity,
        CancellationToken ct)
    {
        var subscriptions = await _subscriptions.GetByTenantAsync(tenantId, ct);
        if (subscriptions.Count == 0) return;

        var payload = new WebPushPayload(title, body, link, severity);

        // In paralel: un browser care nu raspunde nu trebuie sa intarzie restul
        await Task.WhenAll(
            subscriptions.Select(subscription => _push.SendAsync(subscription, payload, ct)));
    }

    /// <summary>
    /// Preferinta salvata, sau valorile implicite din catalog daca nu s-a atins
    /// nimeni de ea.
    /// </summary>
    private async Task<(bool InApp, bool Push)> ResolvePreferenceAsync(
        Guid tenantId,
        string type,
        CancellationToken ct)
    {
        var saved = await _notifications.GetPreferencesAsync(tenantId, ct);
        var match = saved.FirstOrDefault(p => p.Type == type);

        if (match is not null) return (match.InApp, match.Push);

        var info = NotificationTypes.Find(type)!;
        return (info.InAppByDefault, info.PushByDefault);
    }

    public async Task<NotificationListDto> GetAsync(
        Guid tenantId,
        bool unreadOnly,
        int limit,
        CancellationToken ct = default)
    {
        // Limita marginita: un client care cere 100000 ar trage toata tabela
        var safeLimit = Math.Clamp(limit, 1, 100);

        var items = await _notifications.GetAsync(tenantId, unreadOnly, safeLimit, ct);
        var unread = await _notifications.CountUnreadAsync(tenantId, ct);

        return new NotificationListDto
        {
            UnreadCount = unread,
            Items = items.Select(NotificationResponseDto.From).ToList(),
        };
    }

    public async Task MarkReadAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        // Marcarea unei notificari deja citite nu e eroare; una inexistenta da
        var marked = await _notifications.MarkReadAsync(id, tenantId, ct);
        if (marked) return;

        var all = await _notifications.GetAsync(tenantId, unreadOnly: false, 100, ct);
        if (all.All(n => n.Id != id)) throw NotFoundException.Notification();
    }

    public Task<int> MarkAllReadAsync(Guid tenantId, CancellationToken ct = default) =>
        _notifications.MarkAllReadAsync(tenantId, ct);

    public async Task<List<NotificationPreferenceDto>> GetPreferencesAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var saved = await _notifications.GetPreferencesAsync(tenantId, ct);

        // Pornim de la catalog, nu de la ce e in DB: un tip nou apare in setari
        // imediat, cu valorile lui implicite, fara migrare si fara cod in plus.
        return NotificationTypes.All
            .Select(info =>
            {
                var match = saved.FirstOrDefault(p => p.Type == info.Id);
                return new NotificationPreferenceDto
                {
                    Type = info.Id,
                    Group = info.Group,
                    Title = info.Title,
                    Description = info.Description,
                    InApp = match?.InApp ?? info.InAppByDefault,
                    Push = match?.Push ?? info.PushByDefault,
                };
            })
            .ToList();
    }

    public async Task<List<NotificationPreferenceDto>> UpdatePreferenceAsync(
        Guid tenantId,
        NotificationPreferenceUpdateDto dto,
        CancellationToken ct = default)
    {
        if (!NotificationTypes.IsKnown(dto.Type))
        {
            throw ValidationException.ForField(
                nameof(dto.Type),
                $"Tipul de notificare „{dto.Type}” nu există.");
        }

        await _notifications.UpsertPreferenceAsync(
            new NotificationPreference
            {
                TenantId = tenantId,
                Type = dto.Type,
                InApp = dto.InApp,
                Push = dto.Push,
                UpdatedAt = DateTime.UtcNow,
            },
            ct);

        return await GetPreferencesAsync(tenantId, ct);
    }
}
