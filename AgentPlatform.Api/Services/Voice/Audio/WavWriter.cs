using System.Buffers.Binary;

namespace AgentPlatform.Api.Services.Voice.Audio;

/// <summary>
/// Impacheteaza PCM brut intr-un fisier WAV.
/// </summary>
/// <remarks>
/// Necesar pentru Whisper: API-ul de transcriere respinge PCM fara container,
/// fiindca nu poate ghici rata de eșantionare sau numarul de canale.
/// </remarks>
public static class WavWriter
{
    private const int HeaderSize = 44;

    public static byte[] Wrap(
        ReadOnlySpan<byte> pcm,
        int sampleRate = 8000,
        short channels = 1,
        short bitsPerSample = 16)
    {
        var output = new byte[HeaderSize + pcm.Length];
        var span = output.AsSpan();

        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = (short)(channels * bitsPerSample / 8);

        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], 36 + pcm.Length);
        "WAVE"u8.CopyTo(span[8..]);

        "fmt "u8.CopyTo(span[12..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1); // PCM necomprimat
        BinaryPrimitives.WriteInt16LittleEndian(span[22..], channels);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], byteRate);
        BinaryPrimitives.WriteInt16LittleEndian(span[32..], blockAlign);
        BinaryPrimitives.WriteInt16LittleEndian(span[34..], bitsPerSample);

        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], pcm.Length);
        pcm.CopyTo(span[HeaderSize..]);

        return output;
    }

    /// <summary>Durata audio, pentru statistici si pentru audio_duration_ms.</summary>
    public static int DurationMs(int pcmBytes, int sampleRate = 8000) =>
        pcmBytes * 1000 / (sampleRate * 2);
}
