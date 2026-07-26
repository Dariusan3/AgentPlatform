using System.Security.Cryptography;
using System.Text;

namespace AgentPlatform.Api.Services;

public interface ITwilioRequestValidator
{
    bool IsConfigured { get; }

    /// <summary>URL-ul public pe care Twilio l-a apelat, pentru o cale data.</summary>
    string? PublicUrl(string path);

    bool IsValid(string url, IEnumerable<KeyValuePair<string, string>> form, string? signature);
}

/// <summary>
/// Verifica semnatura X-Twilio-Signature. Fara ea, oricine care afla URL-ul
/// public poate injecta mesaje in conversatiile clientilor.
/// </summary>
/// <remarks>
/// Algoritmul Twilio: concateneaza URL-ul cu parametrii sortati alfabetic
/// (cheie + valoare, fara separatori), apoi HMAC-SHA1 cu auth token, in base64.
/// </remarks>
public class TwilioRequestValidator : ITwilioRequestValidator
{
    private readonly IConfiguration _config;

    public TwilioRequestValidator(IConfiguration config)
    {
        _config = config;
    }

    private string? AuthToken => _config["Twilio:AuthToken"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(AuthToken);

    /// <summary>
    /// Trebuie configurat explicit: in spatele unui tunel, ASP.NET vede
    /// http://localhost:5274, nu URL-ul https public pe care a semnat Twilio.
    /// </summary>
    public string? PublicUrl(string path)
    {
        var baseUrl = _config["Twilio:PublicBaseUrl"];
        return string.IsNullOrWhiteSpace(baseUrl)
            ? null
            : $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    public bool IsValid(
        string url,
        IEnumerable<KeyValuePair<string, string>> form,
        string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || AuthToken is null) return false;

        var payload = new StringBuilder(url);
        foreach (var pair in form.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            payload.Append(pair.Key).Append(pair.Value);
        }

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(AuthToken));
        var expected = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString())));

        // Comparatie in timp constant: altfel semnatura se poate ghici byte cu byte
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }
}
