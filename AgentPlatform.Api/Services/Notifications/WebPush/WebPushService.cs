using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;

namespace AgentPlatform.Api.Services.Notifications.WebPush;

public interface IWebPushService
{
    /// <summary>Fara chei VAPID, Web Push nu poate functiona deloc.</summary>
    bool IsConfigured { get; }

    /// <summary>Cheia publica pe care browserul o cere ca sa se aboneze.</summary>
    string? PublicKey { get; }

    Task SendAsync(
        PushSubscription subscription,
        WebPushPayload payload,
        CancellationToken ct = default);
}

/// <summary>Ce ajunge in notificarea de sistem afisata de browser.</summary>
public record WebPushPayload(string Title, string Body, string? Url, string Severity);

public class WebPushService : IWebPushService
{
    /// <summary>Cat pastreaza serviciul de push mesajul daca browserul e inchis.</summary>
    private const int TtlSeconds = 86400;

    /// <summary>
    /// camelCase, ca restul API-ului: service workerul citeste acelasi stil de
    /// nume ca oriunde altundeva in frontend.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IPushSubscriptionRepository _subscriptions;
    private readonly IConfiguration _config;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(
        HttpClient http,
        IPushSubscriptionRepository subscriptions,
        IConfiguration config,
        ILogger<WebPushService> logger)
    {
        _http = http;
        _subscriptions = subscriptions;
        _config = config;
        _logger = logger;
    }

    public string? PublicKey => _config["WebPush:PublicKey"];

    private string? PrivateKey => _config["WebPush:PrivateKey"];

    private string Subject => _config["WebPush:Subject"] ?? "mailto:contact@portar.ro";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);

    public async Task SendAsync(
        PushSubscription subscription,
        WebPushPayload payload,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("Web Push nu e configurat; notificarea ramane doar in aplicatie");
            return;
        }

        var body = WebPushCrypto.Encrypt(
            JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions),
            WebPushCrypto.FromBase64Url(subscription.P256dh),
            WebPushCrypto.FromBase64Url(subscription.Auth));

        using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Endpoint)
        {
            Content = new ByteArrayContent(body),
        };

        request.Content.Headers.ContentType =
            new MediaTypeHeaderValue("application/octet-stream");
        request.Content.Headers.ContentEncoding.Add("aes128gcm");
        request.Headers.TryAddWithoutValidation("TTL", TtlSeconds.ToString());
        request.Headers.TryAddWithoutValidation("Urgency", "normal");
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            VapidAuth.BuildAuthorizationHeader(
                subscription.Endpoint, Subject, PublicKey!, PrivateKey!));

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException exception)
        {
            // Rețeaua cazuta nu trebuie sa opreasca restul notificarilor
            _logger.LogWarning(
                exception, "Web Push nu a putut fi trimis catre {Host}",
                new Uri(subscription.Endpoint).Host);
            return;
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                await _subscriptions.TouchAsync(subscription.Id, ct);
                return;
            }

            // 404/410 = abonament expirat sau revocat din browser. Pastrat, ar
            // genera erori la fiecare notificare pana la sfarsitul timpului.
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                await _subscriptions.DeleteAsync(subscription.Id, ct);
                _logger.LogInformation(
                    "Abonament Web Push expirat, sters: {Endpoint}",
                    Truncate(subscription.Endpoint));
                return;
            }

            var detail = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Serviciul de push a raspuns {Status} pentru {Endpoint}: {Detail}",
                (int)response.StatusCode,
                Truncate(subscription.Endpoint),
                Truncate(detail, 200));
        }
    }

    /// <summary>Adresele de push sunt foarte lungi; in loguri incurca mai mult decat ajuta.</summary>
    private static string Truncate(string value, int max = 60) =>
        value.Length <= max ? value : $"{value[..max]}…";
}

/// <summary>
/// Varianta care nu trimite nimic si nu se plange.
/// </summary>
/// <remarks>
/// Nu e folosita in aplicatie: <see cref="WebPushService"/> tace singur cand nu
/// e configurat. Exista pentru teste, ca sa nu ceara rețea.
/// </remarks>
public class NullWebPushService : IWebPushService
{
    public bool IsConfigured => false;
    public string? PublicKey => null;

    public Task SendAsync(
        PushSubscription subscription,
        WebPushPayload payload,
        CancellationToken ct = default) => Task.CompletedTask;
}
