using System.Text;
using System.Xml.Linq;
using AgentPlatform.Api.Data;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services;
using AgentPlatform.Api.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

/// <summary>
/// Primeste mesajele de pe WhatsApp prin Twilio.
/// </summary>
/// <remarks>
/// AllowAnonymous e obligatoriu: Twilio nu trimite JWT. Protectia vine din
/// semnatura X-Twilio-Signature, verificata la fiecare cerere.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IConversationService _conversations;
    private readonly IAiAgentRepository _agents;
    private readonly ITwilioRequestValidator _validator;
    private readonly ITenantContextSetter _tenant;
    private readonly INotificationService _notifications;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IConversationService conversations,
        IAiAgentRepository agents,
        ITwilioRequestValidator validator,
        ITenantContextSetter tenant,
        INotificationService notifications,
        IConfiguration config,
        ILogger<WebhooksController> logger)
    {
        _conversations = conversations;
        _agents = agents;
        _validator = validator;
        _tenant = tenant;
        _notifications = notifications;
        _config = config;
        _logger = logger;
    }

    [HttpPost("twilio")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/xml")]
    public async Task<IActionResult> Twilio(CancellationToken ct)
    {
        var form = Request.Form.ToDictionary(f => f.Key, f => f.Value.ToString());
        var from = form.GetValueOrDefault("From", string.Empty);
        var to = form.GetValueOrDefault("To", string.Empty);
        var body = form.GetValueOrDefault("Body", string.Empty);
        var profileName = form.GetValueOrDefault("ProfileName");

        if (!_validator.IsConfigured)
        {
            _logger.LogError(
                "Twilio:AuthToken lipseste. Webhookul refuza cererile pana e configurat.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var url = _validator.PublicUrl("/api/webhooks/twilio");
        if (url is null)
        {
            _logger.LogError(
                "Twilio:PublicBaseUrl lipseste. Semnatura nu poate fi verificata fara " +
                "URL-ul public exact pe care l-a apelat Twilio.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var signature = Request.Headers["X-Twilio-Signature"].ToString();
        if (!_validator.IsValid(url, form, signature))
        {
            _logger.LogWarning(
                "Semnatura Twilio invalida pentru {Url}. Cerere respinsa.", url);
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            // Poza sau audio, fara text. Confirmam ca am primit, fara sa raspundem.
            return Empty();
        }

        var agent = await ResolveAgentAsync(to, ct);
        if (agent is null)
        {
            _logger.LogWarning(
                "Niciun agent activ pentru numarul {To}. Mesajul de la {From} e ignorat.",
                to,
                from);
            return Empty();
        }

        // Tenantul vine din agent, nu din claim-uri: de aici incolo filtrele globale functioneaza
        _tenant.Override(agent.TenantId);

        var reply = await _conversations.HandleInboundWhatsAppAsync(
            agent,
            PhoneNumber.Normalize(from),
            profileName,
            body,
            ct);

        // TwiML: Twilio trimite raspunsul, deci nu ne trebuie apel separat spre API-ul lui
        return reply is null ? Empty() : Twiml(reply);
    }

    /// <summary>
    /// In productie fiecare agent are numarul lui, deci `To` il identifica.
    /// In sandbox numarul e comun tuturor, deci cadem pe un tenant din configurare.
    /// </summary>
    private async Task<Models.AiAgent?> ResolveAgentAsync(string to, CancellationToken ct)
    {
        var byNumber = await _agents.FindByWhatsAppNumberAsync(to, ct);
        if (byNumber is { IsActive: true }) return byNumber;

        if (byNumber is { IsActive: false })
        {
            _logger.LogInformation(
                "Agentul {Name} deserveste {To}, dar e oprit.", byNumber.Name, to);

            await _notifications.NotifyAsync(
                byNumber.TenantId,
                NotificationTypes.InactiveAgentMessage,
                "Mesaj către un agent oprit",
                $"Cineva i-a scris lui {byNumber.Name}, dar agentul e oprit " +
                "și mesajul a rămas fără răspuns.",
                "/dashboard/agents",
                "warning",
                ct);

            return null;
        }

        var devTenant = _config["Twilio:SandboxTenantId"];
        if (Guid.TryParse(devTenant, out var tenantId))
        {
            _logger.LogInformation(
                "Numarul {To} nu e alocat niciunui agent; folosesc tenantul de sandbox.",
                to);
            return await _agents.GetFirstActiveAsync(tenantId, ct);
        }

        return null;
    }

    private ContentResult Twiml(string message)
    {
        var xml = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("Response", new XElement("Message", message)));

        return new ContentResult
        {
            Content = xml.Declaration + Environment.NewLine + xml,
            ContentType = "application/xml",
            StatusCode = StatusCodes.Status200OK,
        };
    }

    /// <summary>TwiML gol: confirmam primirea fara sa trimitem nimic.</summary>
    private ContentResult Empty() => new()
    {
        Content = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>")
            .AppendLine()
            .Append("<Response />")
            .ToString(),
        ContentType = "application/xml",
        StatusCode = StatusCodes.Status200OK,
    };
}
