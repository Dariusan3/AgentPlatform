using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/properties")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _properties;

    public PropertiesController(IPropertyService properties)
    {
        _properties = properties;
    }

    private Guid TenantId => User.GetTenantId();

    [HttpGet]
    [ProducesResponseType<List<PropertyResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PropertyResponseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? city,
        CancellationToken ct)
        => Ok(await _properties.GetAllAsync(TenantId, search, type, city, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PropertyResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponseDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _properties.GetByIdAsync(id, TenantId, ct));

    [HttpPost]
    [ProducesResponseType<PropertyResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PropertyResponseDto>> Create(
        PropertyCreateDto dto,
        CancellationToken ct)
    {
        var created = await _properties.CreateAsync(dto, TenantId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<PropertyResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyResponseDto>> Update(
        Guid id,
        PropertyUpdateDto dto,
        CancellationToken ct)
        => Ok(await _properties.UpdateAsync(id, dto, TenantId, ct));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _properties.DeleteAsync(id, TenantId, ct);
        return NoContent();
    }
}
