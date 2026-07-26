namespace AgentPlatform.Api.Services.Voice.Audio;

/// <summary>
/// Codec G.711 μ-law, formatul in care Twilio Media Streams trimite si primeste
/// audio (8 kHz, mono, 8 biți per eșantion).
/// </summary>
/// <remarks>
/// μ-law comprima 16 biți liniari in 8 biți logaritmic: pastreaza rezolutia la
/// amplitudini mici, unde e vocea, si o pierde la cele mari, unde nu se aude
/// diferenta. De aceea telefonia il foloseste de 50 de ani.
/// </remarks>
public static class G711
{
    private const int Bias = 0x84;
    private const int Clip = 32635;

    /// <summary>Numarul de biți de exponent pentru fiecare interval de amplitudine.</summary>
    private static readonly byte[] ExponentTable =
    [
        0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
    ];

    /// <summary>Un eșantion PCM 16-bit semnat, in octetul μ-law corespunzator.</summary>
    public static byte EncodeSample(short sample)
    {
        var sign = (sample >> 8) & 0x80;
        if (sign != 0) sample = (short)-sample;
        if (sample > Clip) sample = Clip;

        sample = (short)(sample + Bias);
        var exponent = ExponentTable[(sample >> 7) & 0xFF];
        var mantissa = (sample >> (exponent + 3)) & 0x0F;

        // Complementul: μ-law transmite valorile inversate pe fir
        return (byte)~(sign | (exponent << 4) | mantissa);
    }

    public static short DecodeSample(byte value)
    {
        value = (byte)~value;

        var sign = value & 0x80;
        var exponent = (value >> 4) & 0x07;
        var mantissa = value & 0x0F;

        var sample = ((mantissa << 3) + Bias) << exponent;
        sample -= Bias;

        return (short)(sign != 0 ? -sample : sample);
    }

    /// <summary>PCM 16-bit little-endian → μ-law. Un octet de ieșire la doi de intrare.</summary>
    public static byte[] PcmToMulaw(ReadOnlySpan<byte> pcm)
    {
        var output = new byte[pcm.Length / 2];
        for (var i = 0; i < output.Length; i++)
        {
            var sample = (short)(pcm[i * 2] | (pcm[i * 2 + 1] << 8));
            output[i] = EncodeSample(sample);
        }
        return output;
    }

    /// <summary>μ-law → PCM 16-bit little-endian. Doi octeți de ieșire la unul de intrare.</summary>
    public static byte[] MulawToPcm(ReadOnlySpan<byte> mulaw)
    {
        var output = new byte[mulaw.Length * 2];
        for (var i = 0; i < mulaw.Length; i++)
        {
            var sample = DecodeSample(mulaw[i]);
            output[i * 2] = (byte)(sample & 0xFF);
            output[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }
        return output;
    }
}
