namespace AgentPlatform.Api.Services;

/// <summary>
/// Aduce numerele la o forma unica.
/// </summary>
/// <remarks>
/// Fara asta, „+40 745 213 897" scris de mana in panou si „+40745213897" primit
/// de la WhatsApp sunt doua persoane diferite, iar acelasi client capata doua
/// conversatii paralele.
/// </remarks>
public static class PhoneNumber
{
    private const string RoPrefix = "40";

    /// <summary>Forma canonica: „+40745213897". Ce nu se poate normaliza rămâne curatat de spatii.</summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var value = raw.Trim();

        // Twilio trimite „whatsapp:+40745213897"
        if (value.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
        {
            value = value["whatsapp:".Length..];
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return string.Empty;

        // 0745213897 -> 40745213897, ca sa se potriveasca cu forma internationala
        if (digits.StartsWith('0') && !digits.StartsWith("00"))
        {
            digits = RoPrefix + digits[1..];
        }
        else if (digits.StartsWith("00"))
        {
            digits = digits[2..];
        }

        return $"+{digits}";
    }
}
