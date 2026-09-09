using AgentPlatform.Api.Data;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<TestController> _logger;

    public TestController(AppDbContext db, ILogger<TestController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Health check. Ramane public, ca sa poata fi apelat de monitorizare.</summary>
    [AllowAnonymous]
    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        try
        {
            // IgnoreQueryFilters: ruta e publica, deci nu exista tenant, iar
            // filtrul global ar transforma numaratoarea in zero mereu.
            var agentsCount = await _db.Agents.IgnoreQueryFilters().CountAsync();

            return Ok(new
            {
                status = "ok",
                message = "Conexiune la Supabase reusita",
                agentsCount
            });
        }
        catch (Exception exception)
        {
            // Ruta e publica, deci mesajul excepției nu poate iesi: ar arata
            // hostul, userul si baza din connection string.
            _logger.LogError(exception, "Health check: conexiunea la baza a eșuat");

            return StatusCode(503, new
            {
                status = "error",
                message = "Nu am putut contacta baza de date. Detaliile sunt în loguri.",
                agentsCount = (int?)null
            });
        }
    }

    /// <summary>Verifica un token Supabase si arata tenantul extras din el.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var tenantId = User.GetTenantId();

        return Ok(new
        {
            tenantId,
            email = User.GetEmail(),
            // Acelasi tenant, citit din HttpContext.Items via TenantMiddleware
            tenantIdFromMiddleware = HttpContext.GetTenantId(),
            message = "Token valid! TenantId extras cu succes."
        });
    }

    /// <summary>
    /// Dovedeste ca tenantul din token corespunde unui rand real din agents.
    /// </summary>
    [HttpGet("me/agent")]
    [Authorize]
    public async Task<IActionResult> MyAgent()
    {
        var tenantId = User.GetTenantId();

        var agent = await _db.Agents
            .Where(a => a.Id == tenantId)
            .Select(a => new { a.Id, a.Email, a.FullName, a.Plan, a.CreatedAt })
            .FirstOrDefaultAsync();

        if (agent is null)
        {
            return NotFound(new
            {
                message = "Nu exista rand in agents pentru acest tenant. " +
                          "Verifica trigger-ul on_auth_user_created."
            });
        }

        return Ok(agent);
    }
}
