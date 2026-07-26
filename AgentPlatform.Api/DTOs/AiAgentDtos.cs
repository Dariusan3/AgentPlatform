using System.ComponentModel.DataAnnotations;
using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.DTOs;

public class AiAgentCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Persona { get; set; }

    /// <summary>professional | friendly | formal</summary>
    [MaxLength(50)]
    public string Tone { get; set; } = "professional";

    [MaxLength(10)]
    public string Language { get; set; } = "ro";

    [MaxLength(20)]
    public string? WhatsAppNumber { get; set; }
}

public class AiAgentUpdateDto : AiAgentCreateDto
{
    public bool IsActive { get; set; } = true;
}

public class AiAgentResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Persona { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public int ConversationsCount { get; set; }
    public int LeadsCount { get; set; }

    public static AiAgentResponseDto From(
        AiAgent agent,
        int conversationsCount = 0,
        int leadsCount = 0) => new()
    {
        Id = agent.Id,
        TenantId = agent.TenantId,
        Name = agent.Name,
        Persona = agent.Persona ?? string.Empty,
        Tone = agent.Tone,
        Language = agent.Language,
        WhatsAppNumber = agent.WhatsAppNumber,
        IsActive = agent.IsActive,
        CreatedAt = agent.CreatedAt,
        ConversationsCount = conversationsCount,
        LeadsCount = leadsCount,
    };
}
