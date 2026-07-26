using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversations;

    public ConversationsController(IConversationService conversations)
    {
        _conversations = conversations;
    }

    private Guid TenantId => User.GetTenantId();

    /// <summary>
    /// Lista pentru panoul din stanga. `messages` contine doar ultimul mesaj,
    /// ca previzualizare — istoricul complet vine din endpointul de detaliu.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<ConversationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConversationResponseDto>>> GetAll(CancellationToken ct)
        => Ok(await _conversations.GetAllAsync(TenantId, ct));

    /// <summary>Conversatia cu toate mesajele si leadul asociat, daca exista.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ConversationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationResponseDto>> GetById(
        Guid id,
        CancellationToken ct)
        => Ok(await _conversations.GetByIdAsync(id, TenantId, ct));

    [HttpPatch("{id:guid}/close")]
    [ProducesResponseType<ConversationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationResponseDto>> Close(
        Guid id,
        CancellationToken ct)
        => Ok(await _conversations.CloseAsync(id, TenantId, ct));

    /// <summary>
    /// Simuleaza un mesaj primit pe WhatsApp si genereaza raspunsul agentului.
    /// Exista ca sa poti testa agentii local, fara Twilio si fara telefon.
    /// </summary>
    [HttpPost("simulate")]
    [ProducesResponseType<SimulateResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SimulateResultDto>> Simulate(
        SimulateMessageDto dto,
        CancellationToken ct)
        => Ok(await _conversations.SimulateInboundAsync(dto, TenantId, ct));

    /// <summary>Creeaza leadul din conversatie. Idempotent.</summary>
    [HttpPatch("{id:guid}/convert")]
    [ProducesResponseType<LeadResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeadResponseDto>> Convert(Guid id, CancellationToken ct)
        => Ok(await _conversations.ConvertToLeadAsync(id, TenantId, ct));
}
