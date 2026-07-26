using System.Globalization;
using AgentPlatform.Api.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AgentPlatform.Api.Services.Voice;

public interface ISmsService
{
    bool IsConfigured { get; }

    Task<bool> SendViewingConfirmationAsync(
        string phone,
        string callerName,
        DateTime viewingDate,
        string agentName,
        CancellationToken ct = default);

    Task<bool> SendLeadSummaryToAgentAsync(
        string agentPhone,
        VoiceCall call,
        CancellationToken ct = default);
}

/// <summary>
/// Trimite SMS-uri prin Twilio.
/// </summary>
/// <remarks>
/// Nu arunca excepții: un SMS nelivrat nu trebuie sa dea peste cap apelul, care
/// s-a terminat deja bine. Intoarce false si scrie in log.
/// </remarks>
public class SmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IConfiguration config, ILogger<SmsService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private string? AccountSid => _config["Twilio:AccountSid"];
    private string? AuthToken => _config["Twilio:AuthToken"];
    private string? SmsNumber => _config["Twilio:SmsNumber"] ?? _config["Twilio:VoiceNumber"];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(SmsNumber);

    public Task<bool> SendViewingConfirmationAsync(
        string phone,
        string callerName,
        DateTime viewingDate,
        string agentName,
        CancellationToken ct = default)
    {
        var ro = new CultureInfo("ro-RO");
        var when = viewingDate.ToString("dddd, d MMMM, 'ora' HH:mm", ro);

        var body =
            $"Bună ziua, {callerName}! Vizionarea dumneavoastră este programată " +
            $"{when}. Vă așteptăm! Pentru reprogramare, răspundeți la acest mesaj. " +
            $"— {agentName}";

        return SendAsync(phone, body, "confirmare vizionare", ct);
    }

    public Task<bool> SendLeadSummaryToAgentAsync(
        string agentPhone,
        VoiceCall call,
        CancellationToken ct = default)
    {
        var minutes = call.DurationSeconds is { } seconds
            ? $"{Math.Max(1, seconds / 60)} min"
            : "durată necunoscută";

        var body = new List<string>
        {
            $"Lead nou din apel telefonic ({minutes}).",
            $"Telefon: {call.CallerPhone}",
        };

        if (!string.IsNullOrWhiteSpace(call.CallerName))
        {
            body.Add($"Nume: {call.CallerName}");
        }

        if (call.ViewingScheduled && call.ViewingDateTime is { } viewing)
        {
            var ro = new CultureInfo("ro-RO");
            body.Add($"Vizionare: {viewing.ToString("d MMM, HH:mm", ro)}");
        }

        return SendAsync(agentPhone, string.Join(" ", body), "rezumat lead", ct);
    }

    private async Task<bool> SendAsync(
        string to,
        string body,
        string what,
        CancellationToken ct)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(
                "SMS ({What}) nu a fost trimis: lipsesc Twilio:AccountSid, " +
                "Twilio:AuthToken sau Twilio:SmsNumber.",
                what);
            return false;
        }

        var destination = PhoneNumber.Normalize(to);
        if (destination.Length < 6)
        {
            _logger.LogWarning("SMS ({What}) nu a fost trimis: numar invalid {To}", what, to);
            return false;
        }

        try
        {
            TwilioClient.Init(AccountSid, AuthToken);

            var message = await MessageResource.CreateAsync(
                to: new Twilio.Types.PhoneNumber(destination),
                from: new Twilio.Types.PhoneNumber(SmsNumber),
                body: body);

            _logger.LogInformation(
                "SMS ({What}) trimis catre {To}: {Sid}", what, destination, message.Sid);
            return true;
        }
        catch (Exception exception)
        {
            // Apelul s-a terminat bine; un SMS eșuat nu trebuie sa il invalideze
            _logger.LogError(exception, "SMS ({What}) catre {To} a eșuat", what, destination);
            return false;
        }
    }
}
