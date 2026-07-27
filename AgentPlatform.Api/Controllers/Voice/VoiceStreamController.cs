using AgentPlatform.Api.Data;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services.Notifications;
using AgentPlatform.Api.Services.Voice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers.Voice;

/// <summary>
/// Punctul de conexiune WebSocket pentru Twilio Media Streams.
/// </summary>
/// <remarks>
/// Twilio nu trimite nici JWT, nici semnatura pe WebSocket. Singura verificare
/// posibila e ca `callSid` sa corespunda unui apel creat de webhookul semnat —
/// deci un apel necunoscut e refuzat.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/voice")]
public class VoiceStreamController : ControllerBase
{
    private readonly IVoiceCallRepository _calls;
    private readonly IVoiceAgentRepository _agents;
    private readonly VoiceStreamHandler _handler;
    private readonly ITenantContextSetter _tenant;
    private readonly INotificationService _notifications;
    private readonly ILogger<VoiceStreamController> _logger;

    public VoiceStreamController(
        IVoiceCallRepository calls,
        IVoiceAgentRepository agents,
        VoiceStreamHandler handler,
        ITenantContextSetter tenant,
        INotificationService notifications,
        ILogger<VoiceStreamController> logger)
    {
        _calls = calls;
        _agents = agents;
        _handler = handler;
        _tenant = tenant;
        _notifications = notifications;
        _logger = logger;
    }

    [HttpGet("stream/{callSid}")]
    public async Task Stream(string callSid, CancellationToken ct)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync(
                "Endpointul acceptă doar conexiuni WebSocket.", ct);
            return;
        }

        var call = await _calls.GetByCallSidAsync(callSid, ct);
        if (call is null)
        {
            // Apelul se creeaza doar de webhookul semnat; lipsa lui = cerere nelegitima
            _logger.LogWarning("Stream cerut pentru apel necunoscut {CallSid}", callSid);
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Din acest punct filtrele globale de tenant au ce citi
        _tenant.Override(call.TenantId);

        var agent = call.VoiceAgent
            ?? (call.VoiceAgentId is { } agentId
                ? await _agents.GetByIdAsync(agentId, call.TenantId, ct)
                : null);

        if (agent is null)
        {
            _logger.LogWarning("Apelul {CallSid} nu are agent vocal asociat", callSid);
            HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        // Limita de durata e plasa de siguranta pe cost: un apel uitat deschis
        // consuma credit Twilio si tokeni la fiecare tura.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(agent.MaxCallDurationSeconds));

        try
        {
            await _handler.HandleAsync(socket, call, agent, timeout.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Apelul {CallSid} a atins limita de {Seconds}s",
                callSid,
                agent.MaxCallDurationSeconds);

            // CancellationToken-ul cererii e deja anulat aici: fara unul nou,
            // notificarea ar fi abandonata exact cand e nevoie de ea.
            await _notifications.NotifyAsync(
                call.TenantId,
                NotificationTypes.CallLimitReached,
                "Apel închis la limita de durată",
                $"Apelul de la {call.CallerPhone} a depășit " +
                $"{agent.MaxCallDurationSeconds} secunde și a fost închis automat.",
                "/dashboard/voice-agents",
                "warning",
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Stream-ul {CallSid} a eșuat", callSid);
        }
    }
}
