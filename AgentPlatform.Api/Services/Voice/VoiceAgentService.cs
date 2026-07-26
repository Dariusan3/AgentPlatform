using System.Security.Cryptography;
using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;

namespace AgentPlatform.Api.Services.Voice;

public interface IVoiceAgentService
{
    Task<List<VoiceAgentResponseDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<VoiceAgentResponseDto> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);
    Task<VoiceAgentResponseDto> CreateAsync(
        VoiceAgentCreateDto dto,
        Guid tenantId,
        CancellationToken ct = default);
    Task<VoiceAgentResponseDto> UpdateAsync(
        Guid id,
        VoiceAgentUpdateDto dto,
        Guid tenantId,
        CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<VoiceAgentResponseDto> ToggleActiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);
    Task<List<VoiceCallResponseDto>> GetCallHistoryAsync(
        Guid agentId,
        Guid tenantId,
        CancellationToken ct = default);
    Task<VoiceCallResponseDto> GetCallAsync(
        Guid callId,
        Guid tenantId,
        CancellationToken ct = default);
    Task<VoiceTestCallResponseDto> StartTestCallAsync(
        Guid agentId,
        Guid tenantId,
        CancellationToken ct = default);
}

public class VoiceAgentService : IVoiceAgentService
{
    /// <summary>Vocile romanesti pe care Azure le are efectiv.</summary>
    private static readonly string[] RomanianVoices =
        ["ro-RO-AlinaNeural", "ro-RO-EmilNeural"];

    private readonly IVoiceAgentRepository _agents;
    private readonly IVoiceCallRepository _calls;

    public VoiceAgentService(IVoiceAgentRepository agents, IVoiceCallRepository calls)
    {
        _agents = agents;
        _calls = calls;
    }

    public async Task<List<VoiceAgentResponseDto>> GetAllAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agents = await _agents.GetAllAsync(tenantId, ct);
        var stats = await _agents.GetStatsAsync(tenantId, ct);

        return agents
            .Select(agent =>
            {
                var (calls, qualified) = stats.GetValueOrDefault(agent.Id);
                return VoiceAgentResponseDto.From(agent, calls, qualified);
            })
            .ToList();
    }

    public async Task<VoiceAgentResponseDto> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.VoiceAgent();

        var stats = await _agents.GetStatsAsync(tenantId, ct);
        var (calls, qualified) = stats.GetValueOrDefault(agent.Id);

        return VoiceAgentResponseDto.From(agent, calls, qualified);
    }

    public async Task<VoiceAgentResponseDto> CreateAsync(
        VoiceAgentCreateDto dto,
        Guid tenantId,
        CancellationToken ct = default)
    {
        Validate(dto);
        await EnsureNumberIsFreeAsync(dto.TwilioPhoneNumber, null, ct);

        var agent = new VoiceAgent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = dto.Name.Trim(),
            TwilioPhoneNumber = Normalize(dto.TwilioPhoneNumber),
            VoiceName = dto.VoiceName,
            SystemPrompt = dto.SystemPrompt?.Trim(),
            GreetingMessage = dto.GreetingMessage?.Trim(),
            MaxCallDurationSeconds = dto.MaxCallDurationSeconds,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        return VoiceAgentResponseDto.From(await _agents.CreateAsync(agent, ct));
    }

    public async Task<VoiceAgentResponseDto> UpdateAsync(
        Guid id,
        VoiceAgentUpdateDto dto,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.VoiceAgent();

        Validate(dto);
        await EnsureNumberIsFreeAsync(dto.TwilioPhoneNumber, id, ct);

        agent.Name = dto.Name.Trim();
        agent.TwilioPhoneNumber = Normalize(dto.TwilioPhoneNumber);
        agent.VoiceName = dto.VoiceName;
        agent.SystemPrompt = dto.SystemPrompt?.Trim();
        agent.GreetingMessage = dto.GreetingMessage?.Trim();
        agent.MaxCallDurationSeconds = dto.MaxCallDurationSeconds;
        agent.IsActive = dto.IsActive;

        return VoiceAgentResponseDto.From(await _agents.UpdateAsync(agent, ct));
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.VoiceAgent();

        await _agents.DeleteAsync(agent, ct);
    }

    public async Task<VoiceAgentResponseDto> ToggleActiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.ToggleActiveAsync(id, tenantId, ct)
            ?? throw NotFoundException.VoiceAgent();

        return VoiceAgentResponseDto.From(agent);
    }

    public async Task<List<VoiceCallResponseDto>> GetCallHistoryAsync(
        Guid agentId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        _ = await _agents.GetByIdAsync(agentId, tenantId, ct)
            ?? throw NotFoundException.VoiceAgent();

        var calls = await _calls.GetByAgentAsync(agentId, tenantId, ct);

        // Fara mesaje: istoricul e o lista, transcrierea completa vine la detaliu
        return calls.Select(c => VoiceCallResponseDto.From(c, includeMessages: false)).ToList();
    }

    /// <summary>Un apel cu transcrierea completa, replica cu replica.</summary>
    public async Task<VoiceCallResponseDto> GetCallAsync(
        Guid callId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var call = await _calls.GetByIdAsync(callId, tenantId, ct)
            ?? throw NotFoundException.VoiceCall();

        return VoiceCallResponseDto.From(call);
    }

    /// <summary>
    /// Deschide un apel pe care browserul il poate prelua, ca sa poti vorbi cu
    /// agentul fara Twilio si fara numar de telefon.
    /// </summary>
    /// <remarks>
    /// Streamul WebSocket nu primeste JWT — nici de la Twilio, nici de la browser —
    /// deci <c>CallSid</c> e singura dovada ca cererea vine de la cine a deschis
    /// apelul. De aceea e aleator criptografic, nu un GUID previzibil.
    /// </remarks>
    public async Task<VoiceTestCallResponseDto> StartTestCallAsync(
        Guid agentId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(agentId, tenantId, ct)
            ?? throw NotFoundException.VoiceAgent();

        if (!agent.IsActive)
        {
            throw ValidationException.ForField(
                "isActive",
                "Agentul e oprit. Pornește-l ca să poți vorbi cu el.");
        }

        // Prefixul TEST separa apelurile din platforma de cele reale in istoric
        var callSid = "TEST" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        var call = await _calls.CreateAsync(
            new VoiceCall
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VoiceAgentId = agent.Id,
                CallSid = callSid,
                CallerPhone = "test-browser",
                CallerName = "Test din platformă",
                Status = "ringing",
                CreatedAt = DateTime.UtcNow,
            },
            ct);

        return new VoiceTestCallResponseDto { CallId = call.Id, CallSid = callSid };
    }

    private static string? Normalize(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : PhoneNumber.Normalize(phone);

    private static void Validate(VoiceAgentCreateDto dto)
    {
        // Strict, nu permisiv: o voce inexistenta nu da eroare la Azure, ci
        // foloseste in silenta vocea implicita — agentul ar vorbi altfel decat
        // ai configurat, fara sa afli de ce. Cand Azure adauga voci, se adauga aici.
        if (!RomanianVoices.Contains(dto.VoiceName))
        {
            throw ValidationException.ForField(
                nameof(dto.VoiceName),
                $"Vocea „{dto.VoiceName}” nu există în Azure Speech. " +
                $"Voci românești disponibile: {string.Join(", ", RomanianVoices)}.");
        }
    }

    /// <summary>
    /// Un numar Twilio poate deservi un singur agent: altfel apelul primit nu ar
    /// avea cui sa fie atribuit.
    /// </summary>
    private async Task EnsureNumberIsFreeAsync(
        string? phone,
        Guid? currentAgentId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(phone)) return;

        var existing = await _agents.FindByPhoneNumberAsync(phone, ct);
        if (existing is not null && existing.Id != currentAgentId)
        {
            throw ValidationException.ForField(
                "twilioPhoneNumber",
                "Numărul e deja folosit de alt agent vocal.");
        }
    }
}
