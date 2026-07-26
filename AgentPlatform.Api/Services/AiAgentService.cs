using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;

namespace AgentPlatform.Api.Services;

public interface IAiAgentService
{
    Task<List<AiAgentResponseDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<AiAgentResponseDto> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<AiAgentResponseDto> CreateAsync(
        AiAgentCreateDto dto,
        Guid tenantId,
        CancellationToken ct = default);
    Task<AiAgentResponseDto> UpdateAsync(
        Guid id,
        AiAgentUpdateDto dto,
        Guid tenantId,
        CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<AiAgentResponseDto> ToggleActiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default);
}

public class AiAgentService : IAiAgentService
{
    private static readonly string[] AllowedTones = ["professional", "friendly", "formal"];
    private static readonly string[] AllowedLanguages = ["ro", "en", "hu"];

    private readonly IAiAgentRepository _agents;

    public AiAgentService(IAiAgentRepository agents)
    {
        _agents = agents;
    }

    public async Task<List<AiAgentResponseDto>> GetAllAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agents = await _agents.GetAllAsync(tenantId, ct);

        // Doua query-uri agregate, nu doua per agent: altfel ar fi N+1
        var conversationCounts = await _agents.GetConversationCountsAsync(tenantId, ct);
        var leadCounts = await _agents.GetLeadCountsAsync(tenantId, ct);

        return agents
            .Select(agent => AiAgentResponseDto.From(
                agent,
                conversationCounts.GetValueOrDefault(agent.Id),
                leadCounts.GetValueOrDefault(agent.Id)))
            .ToList();
    }

    public async Task<AiAgentResponseDto> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.AiAgent();

        var conversationCounts = await _agents.GetConversationCountsAsync(tenantId, ct);
        var leadCounts = await _agents.GetLeadCountsAsync(tenantId, ct);

        return AiAgentResponseDto.From(
            agent,
            conversationCounts.GetValueOrDefault(agent.Id),
            leadCounts.GetValueOrDefault(agent.Id));
    }

    public async Task<AiAgentResponseDto> CreateAsync(
        AiAgentCreateDto dto,
        Guid tenantId,
        CancellationToken ct = default)
    {
        Validate(dto);

        var agent = new AiAgent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = dto.Name.Trim(),
            Persona = dto.Persona?.Trim(),
            Tone = dto.Tone,
            Language = dto.Language,
            WhatsAppNumber = dto.WhatsAppNumber?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var created = await _agents.CreateAsync(agent, ct);
        return AiAgentResponseDto.From(created);
    }

    public async Task<AiAgentResponseDto> UpdateAsync(
        Guid id,
        AiAgentUpdateDto dto,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.AiAgent();

        Validate(dto);

        agent.Name = dto.Name.Trim();
        agent.Persona = dto.Persona?.Trim();
        agent.Tone = dto.Tone;
        agent.Language = dto.Language;
        agent.WhatsAppNumber = dto.WhatsAppNumber?.Trim();
        agent.IsActive = dto.IsActive;

        var updated = await _agents.UpdateAsync(agent, ct);
        return AiAgentResponseDto.From(updated);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var agent = await _agents.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.AiAgent();

        await _agents.DeleteAsync(agent, ct);
    }

    public async Task<AiAgentResponseDto> ToggleActiveAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var agent = await _agents.ToggleActiveAsync(id, tenantId, ct)
            ?? throw NotFoundException.AiAgent();

        return AiAgentResponseDto.From(agent);
    }

    private static void Validate(AiAgentCreateDto dto)
    {
        if (!AllowedTones.Contains(dto.Tone))
        {
            throw ValidationException.ForField(
                nameof(dto.Tone),
                $"Ton invalid. Acceptate: {string.Join(", ", AllowedTones)}.");
        }

        if (!AllowedLanguages.Contains(dto.Language))
        {
            throw ValidationException.ForField(
                nameof(dto.Language),
                $"Limbă invalidă. Acceptate: {string.Join(", ", AllowedLanguages)}.");
        }
    }
}
