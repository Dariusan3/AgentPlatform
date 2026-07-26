using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services.Voice.Audio;

namespace AgentPlatform.Api.Services.Voice;

/// <summary>
/// Inima agentului vocal: primeste audio de la Twilio, transcrie, cheama AI-ul,
/// sintetizeaza raspunsul si il trimite inapoi.
/// </summary>
public class VoiceStreamHandler
{
    /// <summary>Twilio trimite cadre de 20 ms = 160 octeți μ-law.</summary>
    private const int FrameBytes = 160;

    /// <summary>Sub 0,4 s de vorbire e aproape sigur zgomot; nu merita transcris.</summary>
    private const int MinPcmBytes = 8000 * 2 * 400 / 1000;

    /// <summary>Peste 30 s intr-o singura replica, tăiem: probabil e zgomot continuu.</summary>
    private const int MaxPcmBytes = 8000 * 2 * 30;

    private readonly ISpeechService _speech;
    private readonly ITextToSpeechService _tts;
    private readonly IVoiceAiService _ai;
    private readonly ISmsService _sms;
    private readonly IVoiceCallRepository _calls;
    private readonly IPropertyRepository _properties;
    private readonly IAgentRepository _tenants;
    private readonly ILogger<VoiceStreamHandler> _logger;

    public VoiceStreamHandler(
        ISpeechService speech,
        ITextToSpeechService tts,
        IVoiceAiService ai,
        ISmsService sms,
        IVoiceCallRepository calls,
        IPropertyRepository properties,
        IAgentRepository tenants,
        ILogger<VoiceStreamHandler> logger)
    {
        _speech = speech;
        _tts = tts;
        _ai = ai;
        _sms = sms;
        _calls = calls;
        _properties = properties;
        _tenants = tenants;
        _logger = logger;
    }

    public async Task HandleAsync(
        WebSocket socket,
        VoiceCall call,
        VoiceAgent agent,
        CancellationToken ct)
    {
        var pcmBuffer = new List<byte>();
        var silence = new SilenceDetector(thresholdMs: 800);
        var buffer = new byte[8192];
        var greeted = false;

        _logger.LogInformation(
            "Stream deschis pentru apelul {CallSid}, agent {Agent}",
            call.CallSid,
            agent.Name);

        while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            var raw = await ReceiveAsync(socket, buffer, ct);
            if (raw is null) break;

            TwilioMediaMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<TwilioMediaMessage>(raw);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Mesaj WebSocket ilizibil, ignorat");
                continue;
            }

            switch (message?.Event)
            {
                case "start":
                    // streamSid vine abia acum si e obligatoriu ca sa putem trimite audio
                    call.StreamSid = message.Start?.StreamSid ?? message.StreamSid;
                    call.Status = "active";
                    await _calls.UpdateAsync(call, ct);

                    if (!greeted)
                    {
                        greeted = true;
                        await GreetAsync(socket, call, agent, ct);
                    }
                    break;

                case "media":
                    if (message.Media?.Payload is not { Length: > 0 } payload) break;

                    var mulaw = Convert.FromBase64String(payload);
                    var pcm = G711.MulawToPcm(mulaw);
                    pcmBuffer.AddRange(pcm);

                    var tooLong = pcmBuffer.Count >= MaxPcmBytes;
                    if (silence.DetectSilence(pcm) || tooLong)
                    {
                        silence.Reset();

                        if (pcmBuffer.Count >= MinPcmBytes)
                        {
                            var audio = pcmBuffer.ToArray();
                            pcmBuffer.Clear();
                            await ProcessTurnAsync(socket, audio, call, agent, ct);
                        }
                        else
                        {
                            // Prea scurt: zgomot, nu vorbire
                            pcmBuffer.Clear();
                        }
                    }
                    break;

                case "stop":
                    _logger.LogInformation("Twilio a inchis stream-ul {CallSid}", call.CallSid);
                    await FinalizeAsync(call, agent, ct);
                    return;
            }
        }

        // Ieșire prin timeout sau socket inchis: apelul trebuie totusi finalizat
        await FinalizeAsync(call, agent, ct);
    }

    private async Task GreetAsync(
        WebSocket socket,
        VoiceCall call,
        VoiceAgent agent,
        CancellationToken ct)
    {
        var greeting = string.IsNullOrWhiteSpace(agent.GreetingMessage)
            ? "Bună ziua! Cu ce vă pot ajuta?"
            : agent.GreetingMessage;

        try
        {
            await SpeakAsync(socket, greeting, call, agent, ct);
            await SaveMessageAsync(call.Id, "agent", greeting, ct);
        }
        catch (Exception exception)
        {
            // Fara salut apelul continua: clientul va vorbi primul
            _logger.LogError(exception, "Salutul nu a putut fi rostit");
        }
    }

    private async Task ProcessTurnAsync(
        WebSocket socket,
        byte[] pcmAudio,
        VoiceCall call,
        VoiceAgent agent,
        CancellationToken ct)
    {
        try
        {
            var userText = await _speech.TranscribeAudioAsync(pcmAudio, ct);
            if (string.IsNullOrWhiteSpace(userText)) return;

            await SaveMessageAsync(
                call.Id, "caller", userText, ct, WavWriter.DurationMs(pcmAudio.Length));

            var history = await _calls.GetMessagesAsync(call.Id, ct);
            var properties = await _properties.GetAllAsync(call.TenantId, ct);

            var response = await _ai.ProcessTurnAsync(
                userText, history, agent, call, properties, ct);

            if (string.IsNullOrWhiteSpace(response.Text)) return;

            // Salvam INAINTE de a rosti: daca sinteza eșueaza, transcriptul arata
            // totusi ce a intentionat agentul sa spuna. Altfel replica se pierde
            // complet si apelul pare ca s-a intrerupt fara motiv.
            await SaveMessageAsync(call.Id, "agent", response.Text, ct);
            await HandleIntentAsync(response, call, agent, ct);

            try
            {
                await SpeakAsync(socket, response.Text, call, agent, ct);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Replica a fost generata dar nu a putut fi rostita pe {CallSid}. " +
                    "Clientul nu aude nimic.",
                    call.CallSid);
            }
        }
        catch (Exception exception)
        {
            // O tura eșuata nu inchide apelul: clientul poate reformula
            _logger.LogError(
                exception, "Tura de conversatie a eșuat pentru {CallSid}", call.CallSid);
        }
    }

    /// <summary>Sintetizeaza, converteste in μ-law si trimite in cadre de 20 ms.</summary>
    private async Task SpeakAsync(
        WebSocket socket,
        string text,
        VoiceCall call,
        VoiceAgent agent,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(call.StreamSid))
        {
            _logger.LogWarning("Nu pot trimite audio fara streamSid pe {CallSid}", call.CallSid);
            return;
        }

        var pcm = await _tts.SynthesizeAsync(text, agent.VoiceName, ct);
        var mulaw = G711.PcmToMulaw(pcm);

        for (var offset = 0; offset < mulaw.Length; offset += FrameBytes)
        {
            if (ct.IsCancellationRequested || socket.State != WebSocketState.Open) return;

            var length = Math.Min(FrameBytes, mulaw.Length - offset);
            var frame = new TwilioMediaOutbound
            {
                StreamSid = call.StreamSid,
                Media = new TwilioMediaPayload
                {
                    Payload = Convert.ToBase64String(mulaw, offset, length),
                },
            };

            await socket.SendAsync(
                JsonSerializer.SerializeToUtf8Bytes(frame),
                WebSocketMessageType.Text,
                endOfMessage: true,
                ct);
        }

        _logger.LogInformation(
            "Rostit {Ms} ms pe {CallSid}: {Text}",
            WavWriter.DurationMs(pcm.Length),
            call.CallSid,
            text);
    }

    private async Task HandleIntentAsync(
        VoiceAiResponse response,
        VoiceCall call,
        VoiceAgent agent,
        CancellationToken ct)
    {
        if (response.ExtractedData.TryGetValue("nume", out var name)
            && string.IsNullOrWhiteSpace(call.CallerName))
        {
            call.CallerName = name;
            await _calls.UpdateAsync(call, ct);
        }

        switch (response.Intent)
        {
            case "LEAD_QUALIFIED":
                if (call.LeadQualified) break;
                call.LeadQualified = true;
                await _calls.UpdateAsync(call, ct);

                var tenant = await _tenants.GetByIdAsync(call.TenantId, ct);
                if (!string.IsNullOrWhiteSpace(tenant?.Phone))
                {
                    await _sms.SendLeadSummaryToAgentAsync(tenant.Phone, call, ct);
                }
                break;

            case "SCHEDULE_VIEWING":
                var viewing = ParseViewingDate(response.ExtractedData);
                if (viewing is null)
                {
                    _logger.LogInformation(
                        "SCHEDULE_VIEWING fara data valida pe {CallSid}", call.CallSid);
                    break;
                }

                call.ViewingScheduled = true;
                call.ViewingDateTime = viewing;
                call.LeadQualified = true;
                await _calls.UpdateAsync(call, ct);

                call.SmsSent = await _sms.SendViewingConfirmationAsync(
                    call.CallerPhone,
                    call.CallerName ?? "Client",
                    viewing.Value,
                    agent.Name,
                    ct);
                await _calls.UpdateAsync(call, ct);
                break;

            case "END_CALL":
                _logger.LogInformation("Agentul a incheiat apelul {CallSid}", call.CallSid);
                break;
        }
    }

    /// <summary>
    /// Modelul intoarce de obicei ISO, dar nu garantat. Datele din trecut sunt
    /// respinse: o vizionare programata ieri e sigur o greșeala de interpretare.
    /// </summary>
    private DateTime? ParseViewingDate(IReadOnlyDictionary<string, string> data)
    {
        if (!data.TryGetValue("data_vizionare", out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!DateTime.TryParse(
                raw,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal
                | System.Globalization.DateTimeStyles.AssumeLocal,
                out var parsed))
        {
            _logger.LogWarning("Data de vizionare neinterpretabila: {Raw}", raw);
            return null;
        }

        return parsed < DateTime.UtcNow.AddMinutes(-5) ? null : parsed;
    }

    private async Task FinalizeAsync(VoiceCall call, VoiceAgent agent, CancellationToken ct)
    {
        if (call.Status == "completed") return;

        var messages = await _calls.GetMessagesAsync(call.Id, CancellationToken.None);

        call.Status = "completed";
        call.EndedAt = DateTime.UtcNow;
        call.DurationSeconds = (int)(call.EndedAt.Value - call.CreatedAt).TotalSeconds;
        call.Transcript = string.Join(
            Environment.NewLine,
            messages.Select(m =>
                $"[{(m.Role == "caller" ? "Client" : agent.Name)}] {m.Content}"));

        await _calls.UpdateAsync(call, CancellationToken.None);

        _logger.LogInformation(
            "Apel {CallSid} finalizat: {Seconds}s, {Turns} replici, lead={Qualified}",
            call.CallSid,
            call.DurationSeconds,
            messages.Count,
            call.LeadQualified);
    }

    private Task SaveMessageAsync(
        Guid callId,
        string role,
        string content,
        CancellationToken ct,
        int? durationMs = null) =>
        _calls.AddMessageAsync(
            new VoiceMessage
            {
                Id = Guid.NewGuid(),
                VoiceCallId = callId,
                Role = role,
                Content = content,
                AudioDurationMs = durationMs,
                CreatedAt = DateTime.UtcNow,
            },
            ct);

    /// <summary>Reasambleaza mesajul, care poate veni fragmentat pe mai multe cadre.</summary>
    private static async Task<string?> ReceiveAsync(
        WebSocket socket,
        byte[] buffer,
        CancellationToken ct)
    {
        var builder = new StringBuilder();

        while (true)
        {
            WebSocketReceiveResult result;
            try
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            }
            catch (WebSocketException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            if (result.MessageType == WebSocketMessageType.Close) return null;

            builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (result.EndOfMessage) return builder.ToString();
        }
    }
}
