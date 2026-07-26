using System.Net.Http.Headers;
using System.Text.Json;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Services.Voice.Audio;

namespace AgentPlatform.Api.Services.Voice;

public interface ISpeechService
{
    bool IsConfigured { get; }

    /// <summary>PCM 16-bit 8 kHz mono → text. Șir gol daca nu s-a auzit nimic.</summary>
    Task<string> TranscribeAudioAsync(byte[] pcmAudio, CancellationToken ct = default);
}

/// <summary>
/// Transcriere prin Groq Whisper. Gratuit pe nivelul de bază și rapid, ceea ce
/// conteaza la telefon: fiecare sutime de secunda se aude ca ezitare.
/// </summary>
public class SpeechService : ISpeechService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<SpeechService> _logger;

    public SpeechService(HttpClient http, IConfiguration config, ILogger<SpeechService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    private string? ApiKey => _config["Groq:ApiKey"];

    private string Model => _config["Groq:WhisperModel"] ?? "whisper-large-v3";

    private string Endpoint =>
        _config["Groq:TranscriptionEndpoint"]
        ?? "https://api.groq.com/openai/v1/audio/transcriptions";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public async Task<string> TranscribeAudioAsync(
        byte[] pcmAudio,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new ValidationException(
                "Lipsește Groq:ApiKey. Transcrierea are nevoie de ea.");
        }

        // Whisper respinge PCM brut: fara container nu poate deduce rata de eșantionare
        var wav = WavWriter.Wrap(pcmAudio);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(wav);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(file, "file", "audio.wav");
        content.Add(new StringContent(Model), "model");
        // Fixam limba: fara ea, „bună ziua" e uneori detectat drept italiană
        content.Add(new StringContent("ro"), "language");
        content.Add(new StringContent("json"), "response_format");

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Whisper a raspuns {Status}: {Body}", (int)response.StatusCode, body);
            throw new ValidationException(
                $"Transcrierea a eșuat ({(int)response.StatusCode}).");
        }

        using var document = JsonDocument.Parse(body);
        var text = document.RootElement.TryGetProperty("text", out var node)
            ? node.GetString()?.Trim() ?? string.Empty
            : string.Empty;

        _logger.LogInformation(
            "Transcris {Ms} ms de audio: {Text}",
            WavWriter.DurationMs(pcmAudio.Length),
            text);

        return text;
    }
}
