using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _leads;

    public LeadsController(ILeadService leads)
    {
        _leads = leads;
    }

    private Guid TenantId => User.GetTenantId();

    /// <param name="status">new | contacted | qualified | lost. Omis = toate.</param>
    [HttpGet]
    [ProducesResponseType<List<LeadResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<LeadResponseDto>>> GetAll(
        [FromQuery] string? status,
        CancellationToken ct)
        => Ok(await _leads.GetAllAsync(TenantId, status, ct));

    /// <summary>Detaliu cu conversatia si mesajele ei, pentru drawerul din frontend.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LeadResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeadResponseDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _leads.GetByIdAsync(id, TenantId, ct));

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<LeadResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeadResponseDto>> UpdateStatus(
        Guid id,
        LeadStatusDto dto,
        CancellationToken ct)
        => Ok(await _leads.UpdateStatusAsync(id, dto.Status, TenantId, ct));

    [HttpPatch("{id:guid}/notes")]
    [ProducesResponseType<LeadResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeadResponseDto>> UpdateNotes(
        Guid id,
        LeadNotesDto dto,
        CancellationToken ct)
        => Ok(await _leads.UpdateNotesAsync(id, dto.Notes, TenantId, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _leads.DeleteAsync(id, TenantId, ct);
        return NoContent();
    }
}
