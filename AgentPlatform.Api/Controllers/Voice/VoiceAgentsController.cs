using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Services.Voice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers.Voice;

[ApiController]
[Authorize]
[Route("api/voice-agents")]
public class VoiceAgentsController : ControllerBase
{
    private readonly IVoiceAgentService _agents;

    public VoiceAgentsController(IVoiceAgentService agents)
    {
        _agents = agents;
    }

    private Guid TenantId => User.GetTenantId();

    [HttpGet]
    [ProducesResponseType<List<VoiceAgentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VoiceAgentResponseDto>>> GetAll(CancellationToken ct)
        => Ok(await _agents.GetAllAsync(TenantId, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<VoiceAgentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceAgentResponseDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _agents.GetByIdAsync(id, TenantId, ct));

    [HttpPost]
    [ProducesResponseType<VoiceAgentResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VoiceAgentResponseDto>> Create(
        VoiceAgentCreateDto dto,
        CancellationToken ct)
    {
        var created = await _agents.CreateAsync(dto, TenantId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<VoiceAgentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceAgentResponseDto>> Update(
        Guid id,
        VoiceAgentUpdateDto dto,
        CancellationToken ct)
        => Ok(await _agents.UpdateAsync(id, dto, TenantId, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _agents.DeleteAsync(id, TenantId, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/toggle")]
    [ProducesResponseType<VoiceAgentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceAgentResponseDto>> Toggle(Guid id, CancellationToken ct)
        => Ok(await _agents.ToggleActiveAsync(id, TenantId, ct));

    /// <summary>
    /// Deschide un apel de test pe care il preia browserul: vorbesti in microfon,
    /// agentul raspunde in boxe. Fara Twilio, fara numar de telefon.
    /// </summary>
    [HttpPost("{id:guid}/test-call")]
    [ProducesResponseType<VoiceTestCallResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceTestCallResponseDto>> TestCall(
        Guid id,
        CancellationToken ct)
    {
        var call = await _agents.StartTestCallAsync(id, TenantId, ct);

        // Construita din cererea curenta, ca sa fie corecta si prin ngrok
        var scheme = Request.IsHttps ? "wss" : "ws";
        call.StreamUrl = $"{scheme}://{Request.Host}/api/voice/stream/{call.CallSid}";

        return Ok(call);
    }

    /// <summary>Istoricul apelurilor, fara transcriere completa.</summary>
    [HttpGet("{id:guid}/calls")]
    [ProducesResponseType<List<VoiceCallResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<VoiceCallResponseDto>>> Calls(
        Guid id,
        CancellationToken ct)
        => Ok(await _agents.GetCallHistoryAsync(id, TenantId, ct));
}
