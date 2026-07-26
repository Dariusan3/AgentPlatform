using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AgentPlatform.Api.Exceptions;

namespace AgentPlatform.Api.Services;

public record ChatMessage(string Role, string Content);

public interface IGroqClient
{
    /// <summary>true daca exista cheie configurata; false inseamna „nu incerca".</summary>
    bool IsConfigured { get; }

    Task<string> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default);
}

/// <summary>
/// Groq expune un API compatibil OpenAI, deci nu e nevoie de SDK: doar
/// /openai/v1/chat/completions cu cheia in Authorization.
/// </summary>
public class GroqClient : IGroqClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GroqClient> _logger;

    public GroqClient(HttpClient http, IConfiguration config, ILogger<GroqClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    private string? ApiKey => _config["Groq:ApiKey"];

    private string Model => _config["Groq:Model"] ?? "llama-3.3-70b-versatile";

    /// <summary>
    /// Configurabil: API-ul e compatibil OpenAI, deci acelasi client merge si cu
    /// OpenAI sau cu un model local. Si face codul testabil fara cheie reala.
    /// </summary>
    private string Endpoint =>
        _config["Groq:Endpoint"] ?? "https://api.groq.com/openai/v1/chat/completions";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public async Task<string> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new ValidationException(
                "Lipsește Groq:ApiKey din appsettings.Development.json. " +
                "Ia o cheie de pe console.groq.com și adaug-o acolo.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new GroqRequest(
                Model,
                messages.Select(m => new GroqMessage(m.Role, m.Content)).ToArray(),
                // Temperatura joasa: un agent imobiliar nu trebuie sa fie creativ cu preturile
                Temperature: 0.4,
                MaxTokens: 400)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Groq a raspuns {Status}: {Body}",
                (int)response.StatusCode,
                body);

            throw new ValidationException(Explain(response.StatusCode, body));
        }

        var payload = await response.Content.ReadFromJsonAsync<GroqResponse>(ct);
        var reply = payload?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new ValidationException("Groq a răspuns fără conținut.");
        }

        return reply;
    }

    /// <summary>Mesajele generice („Groq a răspuns 400") nu ajuta la depanare.</summary>
    private string Explain(HttpStatusCode status, string body) => status switch
    {
        HttpStatusCode.Unauthorized =>
            "Cheia Groq e invalidă sau expirată. Verifică Groq:ApiKey.",
        HttpStatusCode.TooManyRequests =>
            "Ai depășit limita de cereri Groq. Așteaptă un minut.",
        HttpStatusCode.NotFound or HttpStatusCode.BadRequest
            when body.Contains("model", StringComparison.OrdinalIgnoreCase) =>
            $"Modelul „{Model}” nu e disponibil pe contul tău. " +
            "Schimbă Groq:Model cu unul din lista de pe console.groq.com.",
        _ => $"Groq a răspuns {(int)status}.",
    };

    private record GroqRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] GroqMessage[] Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private record GroqMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record GroqResponse(
        [property: JsonPropertyName("choices")] GroqChoice[]? Choices);

    private record GroqChoice(
        [property: JsonPropertyName("message")] GroqMessage? Message);
}
