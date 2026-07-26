using System.Text;
using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.Services;

public interface IAiReplyService
{
    bool IsConfigured { get; }

    Task<string> GenerateAsync(
        AiAgent agent,
        IReadOnlyList<Property> properties,
        IReadOnlyList<Message> history,
        CancellationToken ct = default);
}

/// <summary>
/// Construieste promptul si cheama modelul. Aici sta singura logica de
/// „cum vorbeste agentul", deci e locul unde se ajusteaza calitatea raspunsurilor.
/// </summary>
public class AiReplyService : IAiReplyService
{
    /// <summary>
    /// Cate listari intra in context. Toate ar umfla promptul inutil; cand vor fi
    /// sute, aici intra cautarea vectoriala pe embedding.
    /// </summary>
    private const int MaxProperties = 15;

    /// <summary>Cate mesaje din istoric trimitem, ca sa nu crestem la infinit.</summary>
    private const int MaxHistory = 20;

    private readonly IGroqClient _groq;

    public AiReplyService(IGroqClient groq)
    {
        _groq = groq;
    }

    public bool IsConfigured => _groq.IsConfigured;

    public Task<string> GenerateAsync(
        AiAgent agent,
        IReadOnlyList<Property> properties,
        IReadOnlyList<Message> history,
        CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new("system", BuildSystemPrompt(agent, properties)),
        };

        foreach (var message in history.TakeLast(MaxHistory))
        {
            // Rolul „system" din istoric nu se retrimite: promptul e reconstruit
            var role = message.Role == "assistant" ? "assistant" : "user";
            messages.Add(new ChatMessage(role, message.Content));
        }

        return _groq.CompleteAsync(messages, ct: ct);
    }

    private static string BuildSystemPrompt(
        AiAgent agent,
        IReadOnlyList<Property> properties)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine($"Ești {agent.Name}, agent imobiliar.");

        if (!string.IsNullOrWhiteSpace(agent.Persona))
        {
            prompt.AppendLine();
            prompt.AppendLine(agent.Persona.Trim());
        }

        prompt.AppendLine();
        prompt.AppendLine("REGULI STRICTE:");
        prompt.AppendLine($"- Răspunde exclusiv în {LanguageName(agent.Language)}.");
        prompt.AppendLine($"- Ton: {ToneName(agent.Tone)}.");
        prompt.AppendLine(
            "- Folosește DOAR listările din lista de mai jos. Nu inventa niciodată " +
            "proprietăți, prețuri, adrese sau disponibilitate.");
        prompt.AppendLine(
            "- Dacă nimic nu se potrivește cererii, spune-o direct și întreabă ce " +
            "ar accepta clientul să schimbe (buget, zonă, număr de camere).");
        prompt.AppendLine("- Nu promite prețuri mai mici decât cele listate.");
        prompt.AppendLine(
            "- Scrii pe WhatsApp: maxim 3-4 propoziții, fără liste cu buline, " +
            "fără formatare markdown.");
        prompt.AppendLine(
            "- Întreabă bugetul și zona dacă nu le știi deja din conversație.");

        prompt.AppendLine();

        if (properties.Count == 0)
        {
            // Fara asta modelul ar inventa listari ca sa fie de ajutor
            prompt.AppendLine(
                "LISTĂRI DISPONIBILE: niciuna. Nu ai nicio proprietate în portofoliu, " +
                "deci nu poți propune nimic concret. Spune-i clientului că revii cu " +
                "oferte și întreabă-i criteriile.");
        }
        else
        {
            prompt.AppendLine("LISTĂRI DISPONIBILE:");
            foreach (var property in properties.Take(MaxProperties))
            {
                prompt.AppendLine(Describe(property));
            }
        }

        return prompt.ToString();
    }

    private static string Describe(Property property)
    {
        var parts = new List<string>();

        if (property.Rooms is > 0) parts.Add($"{property.Rooms} camere");
        if (property.SurfaceSqm is > 0) parts.Add($"{property.SurfaceSqm:0.#} m²");
        if (!string.IsNullOrWhiteSpace(property.Neighborhood)) parts.Add(property.Neighborhood);
        if (!string.IsNullOrWhiteSpace(property.City)) parts.Add(property.City);

        var price = property.PriceEur is { } eur
            ? $"{eur:N0} € ({property.PriceRon ?? 0:N0} RON)"
            : "preț nespecificat";

        var line = $"- {property.Title}: {string.Join(", ", parts)} — {price}";

        if (!string.IsNullOrWhiteSpace(property.Description))
        {
            // Descrierea e tăiată: detaliile lungi consuma context fara sa ajute
            var description = property.Description.Length > 200
                ? property.Description[..200] + "…"
                : property.Description;
            line += $" | {description}";
        }

        return line;
    }

    private static string LanguageName(string code) => code switch
    {
        "en" => "engleză",
        "hu" => "maghiară",
        _ => "română, cu diacritice",
    };

    private static string ToneName(string tone) => tone switch
    {
        "friendly" => "prietenos și direct, ca între cunoscuți",
        "formal" => "formal, cu formule de politețe",
        _ => "profesional și concis",
    };
}
