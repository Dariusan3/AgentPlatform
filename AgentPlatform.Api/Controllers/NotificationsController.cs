using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services.Notifications;
using AgentPlatform.Api.Services.Notifications.WebPush;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IWebPushService _push;

    public NotificationsController(
        INotificationService notifications,
        IPushSubscriptionRepository subscriptions,
        IWebPushService push)
    {
        _notifications = notifications;
        _subscriptions = subscriptions;
        _push = push;
    }

    private Guid TenantId => User.GetTenantId();

    /// <summary>Notificarile contului, cele noi primele, plus contorul de necitite.</summary>
    [HttpGet]
    [ProducesResponseType<NotificationListDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListDto>> Get(
        [FromQuery] bool unreadOnly,
        [FromQuery] int limit,
        CancellationToken ct)
        => Ok(await _notifications.GetAsync(TenantId, unreadOnly, limit == 0 ? 20 : limit, ct));

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await _notifications.MarkReadAsync(id, TenantId, ct);
        return NoContent();
    }

    [HttpPatch("read-all")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> MarkAllRead(CancellationToken ct)
        => Ok(await _notifications.MarkAllReadAsync(TenantId, ct));

    [HttpGet("preferences")]
    [ProducesResponseType<List<NotificationPreferenceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<NotificationPreferenceDto>>> GetPreferences(
        CancellationToken ct)
        => Ok(await _notifications.GetPreferencesAsync(TenantId, ct));

    [HttpPut("preferences")]
    [ProducesResponseType<List<NotificationPreferenceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<NotificationPreferenceDto>>> UpdatePreference(
        NotificationPreferenceUpdateDto dto,
        CancellationToken ct)
        => Ok(await _notifications.UpdatePreferenceAsync(TenantId, dto, ct));

    /// <summary>Cheia publica VAPID, ceruta de browser inainte de abonare.</summary>
    [HttpGet("push/config")]
    [ProducesResponseType<PushConfigDto>(StatusCodes.Status200OK)]
    public ActionResult<PushConfigDto> PushConfig()
        => Ok(new PushConfigDto
        {
            Enabled = _push.IsConfigured,
            PublicKey = _push.PublicKey,
        });

    /// <summary>Inregistreaza browserul curent pentru notificari de sistem.</summary>
    [HttpPost("push/subscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Subscribe(
        PushSubscriptionCreateDto dto,
        CancellationToken ct)
    {
        var saved = await _subscriptions.SaveAsync(
            new PushSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                Endpoint = dto.Endpoint,
                P256dh = dto.P256dh,
                Auth = dto.Auth,
                UserAgent = Request.Headers.UserAgent.ToString(),
                CreatedAt = DateTime.UtcNow,
            },
            ct);

        if (!saved)
        {
            return Conflict(new
            {
                error = "Adresa de notificare aparține altui cont.",
                statusCode = StatusCodes.Status409Conflict,
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Scoate browserul curent din lista. Nu cere ca abonamentul sa existe:
    /// dezabonarea repetata trebuie sa fie inofensiva.
    /// </summary>
    [HttpPost("push/unsubscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unsubscribe(
        PushSubscriptionCreateDto dto,
        CancellationToken ct)
    {
        await _subscriptions.DeleteByEndpointAsync(TenantId, dto.Endpoint, ct);
        return NoContent();
    }

    /// <summary>
    /// Trimite o notificare de proba catre browserele abonate, ca sa verifici
    /// ca permisiunile si cheile chiar functioneaza.
    /// </summary>
    [HttpPost("push/test")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SendTest(CancellationToken ct)
    {
        var subscriptions = await _subscriptions.GetByTenantAsync(TenantId, ct);

        foreach (var subscription in subscriptions)
        {
            await _push.SendAsync(
                subscription,
                new WebPushPayload(
                    "Notificare de probă",
                    "Dacă vezi asta, notificările funcționează.",
                    "/dashboard",
                    "info"),
                ct);
        }

        return NoContent();
    }
}
