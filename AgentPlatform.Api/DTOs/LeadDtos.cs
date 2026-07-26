using System.ComponentModel.DataAnnotations;
using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.DTOs;

public class LeadUpdateDto
{
    /// <summary>new | contacted | qualified | lost</summary>
    [MaxLength(20)]
    public string? Status { get; set; }

    public string? Notes { get; set; }
}

/// <summary>Corp pentru PATCH /api/leads/{id}/status</summary>
public class LeadStatusDto
{
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Corp pentru PATCH /api/leads/{id}/notes</summary>
public class LeadNotesDto
{
    [Required]
    public string Notes { get; set; } = string.Empty;
}

public class LeadResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ConversationId { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? PreferredCity { get; set; }
    public string? PreferredType { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ConversationResponseDto? Conversation { get; set; }

    public static LeadResponseDto From(Lead lead, bool includeConversation = false) => new()
    {
        Id = lead.Id,
        TenantId = lead.TenantId,
        ConversationId = lead.ConversationId,
        Name = lead.Name,
        Phone = lead.Phone,
        Email = lead.Email,
        BudgetMin = lead.BudgetMin,
        BudgetMax = lead.BudgetMax,
        PreferredCity = lead.PreferredCity,
        PreferredType = lead.PreferredType,
        Notes = lead.Notes,
        Status = lead.Status,
        CreatedAt = lead.CreatedAt,
        // Fara flag am recursie: conversatia ar include leadul care include conversatia
        Conversation = includeConversation && lead.Conversation is not null
            ? ConversationResponseDto.From(lead.Conversation)
            : null,
    };
}
