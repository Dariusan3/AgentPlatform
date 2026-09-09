using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AgentPlatform.Api.Http;

/// <summary>
/// Traduce erorile de validare produse de ASP.NET. Implicit ele sunt in engleza
/// („The Name field is required.”) si ajung direct in interfata, unde utilizatorul
/// citeste romana. Traducem aici, o singura data, in loc sa punem ErrorMessage
/// pe fiecare atribut din DTO-uri — un DTO nou e acoperit din start.
/// </summary>
public static partial class ValidationMessages
{
    /// <summary>Eticheta plus genul ei, ca acordul sa iasa corect in romana.</summary>
    private readonly record struct Label(string Text, bool Feminine);

    /// <summary>Numele campurilor asa cum le stie utilizatorul, nu cum se cheama in cod.</summary>
    private static readonly Dictionary<string, Label> Labels =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AiAgentId"] = new("Agentul AI", Feminine: false),
            ["Auth"] = new("Cheia de autentificare push", Feminine: true),
            ["City"] = new("Orașul", Feminine: false),
            ["CompanyName"] = new("Numele companiei", Feminine: false),
            ["ContactName"] = new("Numele contactului", Feminine: false),
            ["ContactPhone"] = new("Telefonul contactului", Feminine: false),
            ["Endpoint"] = new("Adresa endpointului", Feminine: true),
            ["FullName"] = new("Numele complet", Feminine: false),
            ["Language"] = new("Limba", Feminine: true),
            ["ListingUrl"] = new("Linkul anunțului", Feminine: false),
            ["MaxCallDurationSeconds"] = new("Durata maximă a apelului", Feminine: true),
            ["Message"] = new("Mesajul", Feminine: false),
            ["Name"] = new("Numele", Feminine: false),
            ["Neighborhood"] = new("Cartierul", Feminine: false),
            ["NewPassword"] = new("Parola nouă", Feminine: true),
            ["Notes"] = new("Notițele", Feminine: true),
            ["P256dh"] = new("Cheia publică push", Feminine: true),
            ["Phone"] = new("Telefonul", Feminine: false),
            ["PriceEur"] = new("Prețul", Feminine: false),
            ["PropertyType"] = new("Tipul proprietății", Feminine: false),
            ["Rooms"] = new("Numărul de camere", Feminine: false),
            ["Status"] = new("Statusul", Feminine: false),
            ["SurfaceSqm"] = new("Suprafața", Feminine: true),
            ["Title"] = new("Titlul", Feminine: false),
            ["Tone"] = new("Tonul", Feminine: false),
            ["TwilioPhoneNumber"] = new("Numărul Twilio", Feminine: false),
            ["Type"] = new("Tipul", Feminine: false),
            ["VoiceName"] = new("Vocea", Feminine: true),
            ["WhatsAppNumber"] = new("Numărul de WhatsApp", Feminine: false),
        };

    /// <summary>
    /// Mesajul general plus detaliile per camp, gata de pus in raspuns.
    /// Cheile raman cele din JSON-ul trimis, ca frontendul sa poata marca inputul.
    /// </summary>
    /// <param name="parameterNames">
    /// Numele parametrilor actiunii (ex. „dto”). Cand corpul nu se poate lega,
    /// ASP.NET raporteaza eroarea pe ele, iar utilizatorul n-are ce intelege
    /// dintr-un camp care exista doar in semnatura metodei.
    /// </param>
    public static (string Message, IReadOnlyDictionary<string, string[]> Errors) From(
        ModelStateDictionary modelState,
        IEnumerable<string>? parameterNames = null)
    {
        var internalKeys = new HashSet<string>(
            parameterNames ?? [], StringComparer.OrdinalIgnoreCase);

        var errors = new Dictionary<string, string[]>();
        var bodyProblem = false;

        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0) continue;

            // Cheia goala sau numele parametrului inseamna „corpul cererii”, nu un camp
            if (key.Length == 0 || internalKeys.Contains(key))
            {
                bodyProblem = true;
                continue;
            }

            var field = key.Split('.').Last();
            var messages = entry.Errors
                .Select(error => Translate(error.ErrorMessage, field))
                .Distinct()
                .ToArray();

            errors[JsonKey(key)] = messages;
        }

        if (errors.Count == 0)
        {
            var message = bodyProblem
                ? "Cererea a ajuns fără date sau cu un JSON pe care serverul nu îl poate citi."
                : "Datele trimise nu sunt valide.";

            return (message, errors);
        }

        // Un singur camp gresit: mesajul lui e mai util decat „datele sunt invalide”
        var summary = errors.Count == 1
            ? errors.Values.First().First()
            : "Datele trimise nu sunt valide. Verifică ce ai completat.";

        return (summary, errors);
    }

    private static Label LabelFor(string field) =>
        Labels.TryGetValue(field, out var label)
            ? label
            : new Label($"Câmpul „{JsonKey(field)}”", Feminine: false);

    /// <summary>Numele campului asa cum apare in JSON: prima litera mica, fara prefixul „$.”.</summary>
    private static string JsonKey(string key)
    {
        var name = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;

        if (name.Length == 0 || char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <remarks>
    /// Traducem dupa textul englezesc pentru ca in acest punct atributul care a
    /// generat eroarea nu mai e disponibil. Orice mesaj neacoperit cade pe unul
    /// generic in romana — nu lasam engleza sa ajunga la utilizator.
    /// </remarks>
    private static string Translate(string english, string field)
    {
        var label = LabelFor(field);
        var text = label.Text;
        var required = label.Feminine ? "obligatorie" : "obligatoriu";
        var valid = label.Feminine ? "validă" : "valid";

        if (string.IsNullOrWhiteSpace(english))
        {
            return $"{text} nu este {valid}.";
        }

        if (RequiredPattern().IsMatch(english))
        {
            return $"{text} este {required}.";
        }

        var maxLength = MaxLengthPattern().Match(english);
        if (maxLength.Success)
        {
            return $"{text} poate avea cel mult {maxLength.Groups[1].Value} caractere.";
        }

        var minLength = MinLengthPattern().Match(english);
        if (minLength.Success)
        {
            return $"{text} trebuie să aibă cel puțin {minLength.Groups[1].Value} caractere.";
        }

        var range = RangePattern().Match(english);
        if (range.Success)
        {
            return $"{text} trebuie să fie între {range.Groups[1].Value} " +
                   $"și {range.Groups[2].Value}.";
        }

        if (english.Contains("e-mail", StringComparison.OrdinalIgnoreCase))
        {
            return $"{text} nu este o adresă de email validă.";
        }

        if (english.Contains("URL", StringComparison.OrdinalIgnoreCase))
        {
            return $"{text} nu este un link valid.";
        }

        if (english.Contains("request body", StringComparison.OrdinalIgnoreCase))
        {
            return "Cererea a ajuns fără date. Trimite un corp JSON.";
        }

        if (english.Contains("JSON", StringComparison.OrdinalIgnoreCase)
            || english.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
            || english.Contains("is not valid", StringComparison.OrdinalIgnoreCase))
        {
            return $"{text} are un format pe care serverul nu îl poate citi.";
        }

        return $"{text} nu este {valid}.";
    }

    [GeneratedRegex(@"field is required", RegexOptions.IgnoreCase)]
    private static partial Regex RequiredPattern();

    [GeneratedRegex(@"maximum length of '(\d+)'", RegexOptions.IgnoreCase)]
    private static partial Regex MaxLengthPattern();

    [GeneratedRegex(@"minimum length of '(\d+)'", RegexOptions.IgnoreCase)]
    private static partial Regex MinLengthPattern();

    [GeneratedRegex(@"must be between (\S+) and (\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex RangePattern();
}
