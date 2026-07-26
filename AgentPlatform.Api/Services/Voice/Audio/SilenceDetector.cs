namespace AgentPlatform.Api.Services.Voice.Audio;

/// <summary>
/// Decide cand vorbitorul a terminat propozitia, ca sa nu trimitem la
/// transcriere fiecare fragment de 20 ms.
/// </summary>
/// <remarks>
/// Nu declanseaza pe tacere pura: cere mai intai sa fi existat vorbire. Altfel
/// linistea de la inceputul apelului ar fi interpretata drept „a terminat" si am
/// trimite la Whisper doar zgomot de fond.
/// </remarks>
public class SilenceDetector
{
    /// <summary>RMS sub care considerăm liniște. Vocea la telefon trece de 1000.</summary>
    private const double RmsThreshold = 500;

    private const int SampleRate = 8000;
    private const int BytesPerSample = 2;

    private readonly int _silenceThresholdMs;
    private int _silentMs;
    private bool _heardSpeech;

    public SilenceDetector(int thresholdMs = 800)
    {
        _silenceThresholdMs = thresholdMs;
    }

    /// <summary>true daca s-a vorbit si de atunci a trecut pragul de liniște.</summary>
    public bool DetectSilence(ReadOnlySpan<byte> pcmChunk)
    {
        if (pcmChunk.Length < BytesPerSample) return false;

        var durationMs = pcmChunk.Length * 1000 / (SampleRate * BytesPerSample);

        if (Rms(pcmChunk) >= RmsThreshold)
        {
            _heardSpeech = true;
            _silentMs = 0;
            return false;
        }

        if (!_heardSpeech) return false;

        _silentMs += durationMs;
        return _silentMs >= _silenceThresholdMs;
    }

    /// <summary>Se apeleaza dupa ce fragmentul a fost trimis la procesare.</summary>
    public void Reset()
    {
        _silentMs = 0;
        _heardSpeech = false;
    }

    private static double Rms(ReadOnlySpan<byte> pcm)
    {
        double sum = 0;
        var count = pcm.Length / BytesPerSample;

        for (var i = 0; i < count; i++)
        {
            var sample = (short)(pcm[i * 2] | (pcm[i * 2 + 1] << 8));
            sum += (double)sample * sample;
        }

        return count == 0 ? 0 : Math.Sqrt(sum / count);
    }
}
