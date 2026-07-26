using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Services.Voice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers.Voice;

[ApiController]
[Authorize]
[Route("api/voice-calls")]
public class VoiceCallsController : ControllerBase
{
    private readonly IVoiceAgentService _voice;

    public VoiceCallsController(IVoiceAgentService voice)
    {
        _voice = voice;
    }

    /// <summary>
    /// Un apel cu transcrierea completa. Interogat ciclic in timpul apelului de
    /// test, ca sa vezi transcrierea aparand replica cu replica.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<VoiceCallResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VoiceCallResponseDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _voice.GetCallAsync(id, User.GetTenantId(), ct));
}
