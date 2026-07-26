using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

/// <summary>Agentii AI ai tenantului. Nu a se confunda cu tabela `agents`, care e tenantul.</summary>
[ApiController]
[Authorize]
[Route("api/agents")]
public class AgentsController : ControllerBase
{
    private readonly IAiAgentService _agents;

    public AgentsController(IAiAgentService agents)
    {
        _agents = agents;
    }

    private Guid TenantId => User.GetTenantId();

    [HttpGet]
    [ProducesResponseType<List<AiAgentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AiAgentResponseDto>>> GetAll(CancellationToken ct)
        => Ok(await _agents.GetAllAsync(TenantId, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AiAgentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiAgentResponseDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _agents.GetByIdAsync(id, TenantId, ct));

    [HttpPost]
    [ProducesResponseType<AiAgentResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AiAgentResponseDto>> Create(
        AiAgentCreateDto dto,
        CancellationToken ct)
    {
        var created = await _agents.CreateAsync(dto, TenantId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AiAgentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiAgentResponseDto>> Update(
        Guid id,
        AiAgentUpdateDto dto,
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

    /// <summary>Comuta intre activ si inactiv, fara sa trimita tot obiectul.</summary>
    [HttpPatch("{id:guid}/toggle")]
    [ProducesResponseType<AiAgentResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiAgentResponseDto>> Toggle(Guid id, CancellationToken ct)
        => Ok(await _agents.ToggleActiveAsync(id, TenantId, ct));
}
