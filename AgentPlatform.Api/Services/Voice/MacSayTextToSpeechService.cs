using System.Diagnostics;
using AgentPlatform.Api.Exceptions;

namespace AgentPlatform.Api.Services.Voice;

/// <summary>
/// Sinteza vocala prin comanda `say` din macOS, pentru dezvoltare locala.
/// </summary>
/// <remarks>
/// macOS are voce romaneasca nativa („Ioana") si poate scrie direct PCM 16-bit
/// 8 kHz mono — exact formatul cerut de Twilio, deci nu e nevoie de nicio
/// conversie in plus fata de calea Azure.
///
/// Exista ca sa poti testa agentul vocal complet fara cont Azure. In productie
/// nu are ce cauta: depinde de binarul `say`, deci merge doar pe macOS, si
/// calitatea vocii e sub cea a vocilor neurale.
/// </remarks>
public class MacSayTextToSpeechService : ITextToSpeechService
{
    /// <summary>Sinteza locala e rapida; peste atat ceva e blocat.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly IConfiguration _config;
    private readonly ILogger<MacSayTextToSpeechService> _logger;

    public MacSayTextToSpeechService(
        IConfiguration config,
        ILogger<MacSayTextToSpeechService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private string Voice => _config["Voice:MacVoice"] ?? "Ioana";

    public bool IsConfigured => OperatingSystem.IsMacOS();

    public async Task<byte[]> SynthesizeAsync(
        string text,
        string voiceName,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            throw new ValidationException(
                "Providerul TTS „macos” merge doar pe macOS. " +
                "Schimbă Voice:TtsProvider în „azure” și configurează Azure:SpeechKey.");
        }

        // Vocile Azure (ro-RO-AlinaNeural) nu exista in macOS: ignoram numele
        // primit si folosim vocea locala, ca sa nu eșueze apelul degeaba.
        _ = voiceName;

        var wavPath = Path.Combine(Path.GetTempPath(), $"tts-{Guid.NewGuid():N}.wav");

        try
        {
            await RunSayAsync(text, wavPath, ct);

            var wav = await File.ReadAllBytesAsync(wavPath, ct);
            var pcm = StripWavHeader(wav);

            _logger.LogDebug(
                "Sinteza locala: {Chars} caractere → {Bytes} octeți PCM ({Ms} ms)",
                text.Length,
                pcm.Length,
                pcm.Length / 16);

            return pcm;
        }
        finally
        {
            // Fisierele temporare se acumuleaza altfel la fiecare replica
            try { File.Delete(wavPath); } catch (IOException) { /* nu conteaza */ }
        }
    }

    private async Task RunSayAsync(string text, string wavPath, CancellationToken ct)
    {
        var info = new ProcessStartInfo("/usr/bin/say")
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        // Argumente ca lista, nu ca linie de comanda: textul vine de la AI si
        // poate contine ghilimele sau caractere pe care shellul le-ar interpreta.
        info.ArgumentList.Add("-v");
        info.ArgumentList.Add(Voice);
        info.ArgumentList.Add("-o");
        info.ArgumentList.Add(wavPath);
        info.ArgumentList.Add("--data-format=LEI16@8000");
        info.ArgumentList.Add(text);

        using var process = Process.Start(info)
            ?? throw new ValidationException("Comanda `say` nu a putut fi pornita.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);

        var stderr = await process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            throw new ValidationException($"Sinteza locala a depasit {Timeout.TotalSeconds}s.");
        }

        if (process.ExitCode != 0)
        {
            throw new ValidationException(
                $"Comanda `say` a eșuat (cod {process.ExitCode}). " +
                $"Verifică dacă vocea „{Voice}” e instalată: say -v '?' | grep ro_RO. " +
                $"Detalii: {stderr.Trim()}");
        }
    }

    /// <summary>
    /// Scoate containerul WAV si intoarce eșantioanele brute.
    /// </summary>
    /// <remarks>
    /// Nu presupunem antet de 44 de octeți: `say` insereaza si un chunk LIST,
    /// deci parcurgem chunk-urile pana la „data”. Un antet presupus greșit ar
    /// trece zgomot prin codec fara sa dea eroare.
    /// </remarks>
    private static byte[] StripWavHeader(byte[] wav)
    {
        const int RiffHeaderBytes = 12;

        if (wav.Length < RiffHeaderBytes ||
            System.Text.Encoding.ASCII.GetString(wav, 0, 4) != "RIFF")
        {
            throw new ValidationException("Sinteza locala nu a produs un fișier WAV valid.");
        }

        var offset = RiffHeaderBytes;
        while (offset + 8 <= wav.Length)
        {
            var chunkId = System.Text.Encoding.ASCII.GetString(wav, offset, 4);
            var chunkSize = BitConverter.ToInt32(wav, offset + 4);

            if (chunkId == "data")
            {
                var start = offset + 8;
                var length = Math.Min(chunkSize, wav.Length - start);
                return wav[start..(start + length)];
            }

            // Chunk-urile impare sunt aliniate cu un octet de umplutura
            offset += 8 + chunkSize + (chunkSize % 2);
        }

        throw new ValidationException("Fișierul WAV nu are chunk „data”.");
    }
}
