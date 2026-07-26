using System.Text.Json.Serialization;

namespace AgentPlatform.Api.Services.Voice;

/// <summary>
/// Mesajele pe care Twilio Media Streams le trimite prin WebSocket.
/// </summary>
/// <remarks>
/// Evenimente: <c>connected</c>, <c>start</c> (aduce streamSid), <c>media</c>
/// (audio μ-law base64, la fiecare 20 ms), <c>stop</c>.
/// </remarks>
public class TwilioMediaMessage
{
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("streamSid")]
    public string? StreamSid { get; set; }

    [JsonPropertyName("start")]
    public TwilioStreamStart? Start { get; set; }

    [JsonPropertyName("media")]
    public TwilioMediaPayload? Media { get; set; }
}

public class TwilioStreamStart
{
    [JsonPropertyName("streamSid")]
    public string? StreamSid { get; set; }

    [JsonPropertyName("callSid")]
    public string? CallSid { get; set; }
}

public class TwilioMediaPayload
{
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }
}

/// <summary>Audio trimis catre Twilio. `streamSid` e obligatoriu, altfel se ignora.</summary>
public class TwilioMediaOutbound
{
    [JsonPropertyName("event")]
    public string Event { get; init; } = "media";

    [JsonPropertyName("streamSid")]
    public string StreamSid { get; init; } = string.Empty;

    [JsonPropertyName("media")]
    public TwilioMediaPayload Media { get; init; } = new();
}

/// <summary>
/// Golește ce a rămas in coada de redare la Twilio. Trimis cand vrem sa
/// intrerupem agentul, ca sa nu vorbeasca peste client.
/// </summary>
public class TwilioClearOutbound
{
    [JsonPropertyName("event")]
    public string Event { get; init; } = "clear";

    [JsonPropertyName("streamSid")]
    public string StreamSid { get; init; } = string.Empty;
}
