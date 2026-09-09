using System.Text.Json;

namespace AgentPlatform.Api.Http;

/// <summary>
/// Forma unica a erorilor API. Toate caile care raspund cu eroare trec pe aici —
/// middleware-ul de exceptii, evenimentele JWT, validarea modelului si
/// statusurile pe care le produce pipeline-ul singur (404 pe ruta, 405, 415).
/// Frontendul citeste mereu aceleasi campuri, deci nu trebuie sa ghiceasca.
/// </summary>
public static class ApiError
{
    /// <summary>Scrie raspunsul, daca nu a plecat deja ceva pe fir.</summary>
    public static async Task WriteAsync(
        HttpContext context,
        int status,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new Dictionary<string, object?>
        {
            ["error"] = message,
            ["statusCode"] = status,
            ["timestamp"] = DateTime.UtcNow.ToString("o"),
        };

        if (errors is { Count: > 0 })
        {
            payload["errors"] = errors;
        }

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, JsonSerializerOptions.Web));
    }

    /// <summary>
    /// Mesajul pentru un status pe care nu l-a produs codul nostru, ci pipeline-ul.
    /// Fara ele, clientul primeste un corp gol si arata „Serverul a raspuns 405”.
    /// </summary>
    public static string ForStatus(int status, string method) => status switch
    {
        400 => "Cererea nu a putut fi citită. Verifică datele trimise.",
        401 => "Trebuie să fii conectat pentru asta.",
        403 => "Nu ai acces la resursa asta.",
        404 => "Ruta cerută nu există pe acest server.",
        405 => $"Ruta există, dar nu acceptă metoda {method}.",
        408 => "Cererea a durat prea mult și a fost oprită.",
        413 => "Ce ai trimis e prea mare pentru server.",
        415 => "Formatul trimis nu e acceptat. Trimite JSON.",
        429 => "Prea multe cereri într-un timp scurt. Așteaptă puțin.",
        >= 500 => "A apărut o eroare internă. Am înregistrat-o și o investigăm.",
        _ => "Cererea nu a putut fi procesată.",
    };
}
