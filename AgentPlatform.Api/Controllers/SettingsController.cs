using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;

    public SettingsController(ISettingsService settings)
    {
        _settings = settings;
    }

    private Guid TenantId => User.GetTenantId();

    [HttpGet("profile")]
    [ProducesResponseType<ProfileResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponseDto>> GetProfile(CancellationToken ct)
        => Ok(await _settings.GetProfileAsync(TenantId, ct));

    /// <summary>Emailul lipseste intentionat: e adresa de login, gestionata de Supabase Auth.</summary>
    [HttpPut("profile")]
    [ProducesResponseType<ProfileResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfileResponseDto>> UpdateProfile(
        ProfileUpdateDto dto,
        CancellationToken ct)
        => Ok(await _settings.UpdateProfileAsync(dto, TenantId, ct));

    [HttpGet("usage")]
    [ProducesResponseType<UsageResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UsageResponseDto>> GetUsage(CancellationToken ct)
        => Ok(await _settings.GetUsageAsync(TenantId, ct));

    /// <summary>Deleaga catre Supabase Admin API — parolele nu stau in baza noastra.</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePassword(
        PasswordUpdateDto dto,
        CancellationToken ct)
    {
        await _settings.UpdatePasswordAsync(TenantId, dto.NewPassword, ct);
        return NoContent();
    }
}
