using System.Text;
using System.Text.Json;
using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.Services.Voice;

/// <summary>Ce spune agentul si ce a inteles din replica clientului.</summary>
public class VoiceAiResponse
{
    public string Text { get; set; } = string.Empty;

    /// <summary>SCHEDULE_VIEWING | LEAD_QUALIFIED | END_CALL | null</summary>
    public string? Intent { get; set; }

    /// <summary>nume, telefon, buget, data_vizionare — ce a reușit sa extraga.</summary>
    public Dictionary<string, string> ExtractedData { get; set; } = [];
}

public interface IVoiceAiService
{
    Task<VoiceAiResponse> ProcessTurnAsync(
        string userText,
        IReadOnlyList<VoiceMessage> history,
        VoiceAgent agent,
        VoiceCall call,
        IReadOnlyList<Property> properties,
        CancellationToken ct = default);
}

/// <summary>
/// Genereaza replica agentului si extrage intentii din aceeași cerere.
/// </summary>
/// <remarks>
/// Un singur apel, cu raspuns JSON, nu doua: la telefon fiecare apel in plus se
/// aude ca pauza. De aceea cerem modelului sa intoarca simultan textul rostit si
/// ce a inteles.
/// </remarks>
public class VoiceAiService : IVoiceAiService
{
    private const int MaxHistory = 20;
    private const int MaxProperties = 10;

    private readonly IGroqClient _groq;
    private readonly IConfiguration _config;
    private readonly ILogger<VoiceAiService> _logger;

    public VoiceAiService(
        IGroqClient groq,
        IConfiguration config,
        ILogger<VoiceAiService> logger)
    {
        _groq = groq;
        _config = config;
        _logger = logger;
    }

    public async Task<VoiceAiResponse> ProcessTurnAsync(
        string userText,
        IReadOnlyList<VoiceMessage> history,
        VoiceAgent agent,
        VoiceCall call,
        IReadOnlyList<Property> properties,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new("system", BuildSystemPrompt(agent, properties)),
        };

        foreach (var message in history.TakeLast(MaxHistory))
        {
            messages.Add(new ChatMessage(
                message.Role == "agent" ? "assistant" : "user",
                message.Content));
        }

        // Ultima replica nu e inca in istoric: a fost transcrisa acum
        if (history.LastOrDefault()?.Content != userText)
        {
            messages.Add(new ChatMessage("user", userText));
        }

        var raw = await _groq.CompleteAsync(
            messages,
            _config["Groq:ChatModel"] ?? "llama-3.3-70b-versatile",
            ct);

        return Parse(raw);
    }

    private static string BuildSystemPrompt(
        VoiceAgent agent,
        IReadOnlyList<Property> properties)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine($"Ești {agent.Name}, agent imobiliar virtual la telefon.");

        if (!string.IsNullOrWhiteSpace(agent.SystemPrompt))
        {
            prompt.AppendLine();
            prompt.AppendLine(agent.SystemPrompt.Trim());
        }

        prompt.AppendLine();
        prompt.AppendLine("REGULI DE VORBIRE:");
        prompt.AppendLine("- Vorbești DOAR în română, natural, cu diacritice.");
        prompt.AppendLine(
            "- Maxim 2 propoziții per replică. Ești la telefon, nu scrii un email.");
        prompt.AppendLine(
            "- Fără markdown, fără liste, fără enumerări cu cifre. Se citește cu voce.");
        prompt.AppendLine(
            "- Nu spune prețuri cu zecimale. „nouăzeci și două de mii de euro\", " +
            "nu „92000.00 EUR\".");
        prompt.AppendLine("- O singură întrebare pe replică.");

        prompt.AppendLine();
        prompt.AppendLine("SCOPUL TĂU, în ordine:");
        prompt.AppendLine("1. Află ce caută: tip proprietate, zonă, număr de camere.");
        prompt.AppendLine("2. Află bugetul.");
        prompt.AppendLine("3. Propune o proprietate potrivită din lista de mai jos.");
        prompt.AppendLine("4. Propune o vizionare cu zi și oră concrete.");
        prompt.AppendLine("5. Confirmă numele clientului.");

        prompt.AppendLine();
        prompt.AppendLine("REGULI STRICTE:");
        prompt.AppendLine(
            "- Folosește DOAR proprietățile din listă. Nu inventa niciodată " +
            "proprietăți, prețuri sau disponibilitate.");
        prompt.AppendLine("- Nu promite prețuri mai mici decât cele listate.");

        prompt.AppendLine();
        if (properties.Count == 0)
        {
            prompt.AppendLine(
                "PROPRIETĂȚI: niciuna în portofoliu. Nu poți propune nimic concret — " +
                "notează cerințele clientului și spune că revii cu oferte.");
        }
        else
        {
            prompt.AppendLine("PROPRIETĂȚI DISPONIBILE:");
            foreach (var property in properties.Take(MaxProperties))
            {
                prompt.AppendLine(Describe(property));
            }
        }

        // Formatul de raspuns: text + intentie + date extrase, intr-un singur apel
        prompt.AppendLine();
        prompt.AppendLine("FORMAT DE RĂSPUNS — răspunde EXCLUSIV cu JSON valid:");
        prompt.AppendLine("""
            {
              "text": "ce spui cu voce tare, în română",
              "intent": null | "LEAD_QUALIFIED" | "SCHEDULE_VIEWING" | "END_CALL",
              "data": { "nume": "...", "buget": "...", "data_vizionare": "2026-07-28T17:00" }
            }
            """);
        prompt.AppendLine("Când folosești fiecare intenție:");
        prompt.AppendLine(
            "- LEAD_QUALIFIED: ai aflat ce caută ȘI bugetul ȘI numele.");
        prompt.AppendLine(
            "- SCHEDULE_VIEWING: clientul a acceptat o zi și o oră precise. " +
            "Pune data în `data.data_vizionare`, format ISO.");
        prompt.AppendLine(
            "- END_CALL: clientul își ia la revedere sau spune că nu mai e interesat.");
        prompt.AppendLine("- null: în orice alt caz.");

        return prompt.ToString();
    }

    private static string Describe(Property property)
    {
        var parts = new List<string>();
        if (property.Rooms is > 0) parts.Add($"{property.Rooms} camere");
        if (property.SurfaceSqm is > 0) parts.Add($"{property.SurfaceSqm:0.#} metri pătrați");
        if (!string.IsNullOrWhiteSpace(property.Neighborhood)) parts.Add(property.Neighborhood);
        if (!string.IsNullOrWhiteSpace(property.City)) parts.Add(property.City);

        var price = property.PriceEur is { } eur
            ? $"{eur:N0} euro"
            : "preț nespecificat";

        return $"- {property.Title}: {string.Join(", ", parts)} — {price}";
    }

    /// <summary>
    /// Modelele adauga uneori text in jurul JSON-ului. Extragem obiectul si, daca
    /// tot nu se poate parsa, folosim raspunsul brut ca replica — mai bine ceva
    /// rostit decat tăcere pe linie.
    /// </summary>
    private VoiceAiResponse Parse(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');

        if (start >= 0 && end > start)
        {
            try
            {
                using var document = JsonDocument.Parse(raw[start..(end + 1)]);
                var root = document.RootElement;

                var response = new VoiceAiResponse
                {
                    Text = root.TryGetProperty("text", out var text)
                        ? text.GetString()?.Trim() ?? string.Empty
                        : string.Empty,
                    Intent = root.TryGetProperty("intent", out var intent)
                             && intent.ValueKind == JsonValueKind.String
                        ? intent.GetString()
                        : null,
                };

                if (root.TryGetProperty("data", out var data)
                    && data.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in data.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String
                            && property.Value.GetString() is { Length: > 0 } value)
                        {
                            response.ExtractedData[property.Name] = value;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(response.Text)) return response;
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(
                    exception, "Raspunsul AI nu e JSON valid, folosesc textul brut");
            }
        }

        return new VoiceAiResponse { Text = raw.Trim() };
    }
}
