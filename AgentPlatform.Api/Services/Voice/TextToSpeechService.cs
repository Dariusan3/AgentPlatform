using AgentPlatform.Api.Exceptions;
using Microsoft.CognitiveServices.Speech;

namespace AgentPlatform.Api.Services.Voice;

public interface ITextToSpeechService
{
    bool IsConfigured { get; }

    /// <summary>Text → PCM 16-bit 8 kHz mono, formatul cerut de Twilio.</summary>
    Task<byte[]> SynthesizeAsync(
        string text,
        string voiceName,
        CancellationToken ct = default);
}

/// <summary>
/// Sinteza vocala prin Azure Cognitive Services Speech.
/// </summary>
/// <remarks>
/// Vocile romanesti Azure sunt <c>ro-RO-AlinaNeural</c> (feminin) si
/// <c>ro-RO-EmilNeural</c> (masculin). „Ioana" e voce macOS, nu Azure — daca o
/// ceri, Azure o ignora si foloseste vocea implicita.
/// </remarks>
public class TextToSpeechService : ITextToSpeechService
{
    private readonly IConfiguration _config;
    private readonly ILogger<TextToSpeechService> _logger;

    public TextToSpeechService(IConfiguration config, ILogger<TextToSpeechService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private string? Key => _config["Azure:SpeechKey"];

    private string Region => _config["Azure:SpeechRegion"] ?? "westeurope";

    private string DefaultVoice => _config["Azure:VoiceName"] ?? "ro-RO-AlinaNeural";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Key);

    public async Task<byte[]> SynthesizeAsync(
        string text,
        string voiceName,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new ValidationException(
                "Lipsește Azure:SpeechKey. Sinteza vocală nu poate funcționa fără ea. " +
                "Ia o cheie din portal.azure.com → Speech service.");
        }

        var config = SpeechConfig.FromSubscription(Key, Region);
        config.SpeechSynthesisVoiceName =
            string.IsNullOrWhiteSpace(voiceName) ? DefaultVoice : voiceName;

        // Exact formatul pe care il asteapta Twilio dupa conversia in μ-law
        config.SetSpeechSynthesisOutputFormat(
            SpeechSynthesisOutputFormat.Raw8Khz16BitMonoPcm);

        // null pentru output: vrem octeții in memorie, nu redare pe difuzor
        using var synthesizer = new SpeechSynthesizer(config, null);
        using var result = await synthesizer.SpeakTextAsync(text).WaitAsync(ct);

        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            return result.AudioData;
        }

        var details = SpeechSynthesisCancellationDetails.FromResult(result);
        _logger.LogError(
            "Sinteza a eșuat: {Reason} — {Error} {Details}",
            result.Reason,
            details.ErrorCode,
            details.ErrorDetails);

        throw new ValidationException(
            details.ErrorCode == CancellationErrorCode.AuthenticationFailure
                ? "Cheia Azure Speech e invalidă sau regiunea e greșită."
                : $"Sinteza vocală a eșuat: {details.ErrorDetails}");
    }
}
