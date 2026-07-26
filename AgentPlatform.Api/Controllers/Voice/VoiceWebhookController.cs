using System.Xml.Linq;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services;
using AgentPlatform.Api.Services.Voice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers.Voice;

/// <summary>
/// Webhookurile de apel de la Twilio Voice.
/// </summary>
/// <remarks>
/// AllowAnonymous fiindca Twilio nu trimite JWT. Protectia e semnatura
/// X-Twilio-Signature, verificata la fiecare cerere.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/voice/webhook")]
public class VoiceWebhookController : ControllerBase
{
    private readonly IVoiceAgentRepository _agents;
    private readonly IVoiceCallRepository _calls;
    private readonly ITwilioRequestValidator _validator;
    private readonly IConfiguration _config;
    private readonly ILogger<VoiceWebhookController> _logger;

    public VoiceWebhookController(
        IVoiceAgentRepository agents,
        IVoiceCallRepository calls,
        ITwilioRequestValidator validator,
        IConfiguration config,
        ILogger<VoiceWebhookController> logger)
    {
        _agents = agents;
        _calls = calls;
        _validator = validator;
        _config = config;
        _logger = logger;
    }

    /// <summary>Twilio anunta un apel primit. Raspundem cu TwiML care porneste stream-ul.</summary>
    [HttpPost("incoming")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/xml")]
    public async Task<IActionResult> Incoming(CancellationToken ct)
    {
        var form = Request.Form.ToDictionary(f => f.Key, f => f.Value.ToString());

        if (!Validate("incoming", form, out var error)) return error!;

        var callSid = form.GetValueOrDefault("CallSid", string.Empty);
        var from = form.GetValueOrDefault("From", string.Empty);
        var to = form.GetValueOrDefault("To", string.Empty);

        if (string.IsNullOrWhiteSpace(callSid))
        {
            return Hangup("Apel fără identificator.");
        }

        var agent = await ResolveAgentAsync(to, ct);
        if (agent is null)
        {
            _logger.LogWarning(
                "Apel de la {From} pe {To}: niciun agent vocal activ.", from, to);
            return Say("Ne pare rău, momentan nu putem preluă apelul. Vă rugăm reveniți.");
        }

        // Idempotent: Twilio poate reincerca webhookul, iar call_sid e unic
        var call = await _calls.GetByCallSidAsync(callSid, ct);
        if (call is null)
        {
            call = await _calls.CreateAsync(
                new VoiceCall
                {
                    Id = Guid.NewGuid(),
                    TenantId = agent.TenantId,
                    VoiceAgentId = agent.Id,
                    CallSid = callSid,
                    CallerPhone = PhoneNumber.Normalize(from),
                    Status = "ringing",
                    CreatedAt = DateTime.UtcNow,
                },
                ct);
        }

        var wsBase = WebSocketBaseUrl();
        if (wsBase is null)
        {
            _logger.LogError("Twilio:PublicBaseUrl lipseste; nu pot construi URL-ul wss.");
            return Say("Configurare incompletă. Vă rugăm reveniți mai târziu.");
        }

        _logger.LogInformation(
            "Apel {CallSid} de la {From} preluat de {Agent}", callSid, from, agent.Name);

        // <Connect><Stream> deschide WebSocket bidirectional catre noi
        var twiml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("Response",
                new XElement("Connect",
                    new XElement("Stream",
                        new XAttribute("url", $"{wsBase}/api/voice/stream/{callSid}")))));

        return Xml(twiml);
    }

    /// <summary>Twilio raporteaza sfarsitul apelului si durata reala.</summary>
    [HttpPost("status")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var form = Request.Form.ToDictionary(f => f.Key, f => f.Value.ToString());

        if (!Validate("status", form, out var error)) return error!;

        var callSid = form.GetValueOrDefault("CallSid", string.Empty);
        var status = form.GetValueOrDefault("CallStatus", string.Empty);

        var call = await _calls.GetByCallSidAsync(callSid, ct);
        if (call is null)
        {
            _logger.LogInformation("Status pentru apel necunoscut {CallSid}", callSid);
            return NoContent();
        }

        call.Status = status switch
        {
            "completed" => "completed",
            "busy" or "failed" or "no-answer" or "canceled" => "failed",
            "in-progress" => "active",
            _ => call.Status,
        };

        // Durata raportata de Twilio e cea facturata; o preferam calculului nostru
        if (int.TryParse(form.GetValueOrDefault("CallDuration"), out var seconds)
            && seconds > 0)
        {
            call.DurationSeconds = seconds;
        }

        if (call.Status is "completed" or "failed")
        {
            call.EndedAt ??= DateTime.UtcNow;
        }

        await _calls.UpdateAsync(call, ct);

        _logger.LogInformation(
            "Apel {CallSid}: {Status}, {Seconds}s", callSid, call.Status, call.DurationSeconds);

        return NoContent();
    }

    /// <summary>
    /// In productie fiecare agent are numarul lui. Cand numarul nu e alocat,
    /// cadem pe tenantul de test din configurare.
    /// </summary>
    private async Task<VoiceAgent?> ResolveAgentAsync(string to, CancellationToken ct)
    {
        var byNumber = await _agents.FindByPhoneNumberAsync(to, ct);
        if (byNumber is { IsActive: true }) return byNumber;

        if (byNumber is { IsActive: false })
        {
            _logger.LogInformation(
                "Agentul vocal {Name} deserveste {To}, dar e oprit.", byNumber.Name, to);
            return null;
        }

        return Guid.TryParse(_config["Twilio:SandboxTenantId"], out var tenantId)
            ? await _agents.GetFirstActiveAsync(tenantId, ct)
            : null;
    }

    /// <summary>https://... → wss://..., fiindca Twilio cere schema WebSocket.</summary>
    private string? WebSocketBaseUrl()
    {
        var baseUrl = _config["Twilio:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;

        return baseUrl.TrimEnd('/')
            .Replace("https://", "wss://", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "ws://", StringComparison.OrdinalIgnoreCase);
    }

    private bool Validate(
        string what,
        IReadOnlyDictionary<string, string> form,
        out IActionResult? error)
    {
        if (!_validator.IsConfigured)
        {
            _logger.LogError("Twilio:AuthToken lipseste; webhookul {What} refuza cererea.", what);
            error = StatusCode(StatusCodes.Status503ServiceUnavailable);
            return false;
        }

        var url = _validator.PublicUrl($"/api/voice/webhook/{what}");
        if (url is null)
        {
            _logger.LogError("Twilio:PublicBaseUrl lipseste; nu pot verifica semnatura.");
            error = StatusCode(StatusCodes.Status503ServiceUnavailable);
            return false;
        }

        var signature = Request.Headers["X-Twilio-Signature"].ToString();
        if (!_validator.IsValid(url, form, signature))
        {
            _logger.LogWarning("Semnatura Twilio invalida pe {Url}", url);
            error = StatusCode(StatusCodes.Status403Forbidden);
            return false;
        }

        error = null;
        return true;
    }

    private ContentResult Xml(XDocument document) => new()
    {
        Content = document.Declaration + Environment.NewLine + document,
        ContentType = "application/xml",
        StatusCode = StatusCodes.Status200OK,
    };

    private ContentResult Say(string message) => Xml(new XDocument(
        new XDeclaration("1.0", "UTF-8", null),
        new XElement("Response",
            new XElement("Say",
                new XAttribute("language", "ro-RO"),
                message),
            new XElement("Hangup"))));

    private ContentResult Hangup(string reason)
    {
        _logger.LogWarning("Apel inchis: {Reason}", reason);
        return Xml(new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("Response", new XElement("Hangup"))));
    }
}
