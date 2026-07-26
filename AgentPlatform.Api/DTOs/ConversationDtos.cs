using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.DTOs;

public class MessageResponseDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; }

    public static MessageResponseDto From(Message message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        TokensUsed = message.TokensUsed,
        CreatedAt = message.CreatedAt,
    };
}

public class ConversationResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? AiAgentId { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int LeadScore { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public List<MessageResponseDto> Messages { get; set; } = [];
    public LeadResponseDto? Lead { get; set; }

    /// <param name="includeMessages">
    /// Listele nu trimit mesajele: o conversatie lunga ar umfla raspunsul degeaba.
    /// </param>
    public static ConversationResponseDto From(
        Conversation conversation,
        bool includeMessages = true,
        Lead? lead = null) => new()
    {
        Id = conversation.Id,
        TenantId = conversation.TenantId,
        AiAgentId = conversation.AiAgentId,
        ContactPhone = conversation.ContactPhone ?? string.Empty,
        ContactName = conversation.ContactName,
        Channel = conversation.Channel,
        Status = conversation.Status,
        LeadScore = conversation.LeadScore ?? 0,
        StartedAt = conversation.StartedAt,
        LastMessageAt = conversation.LastMessageAt,
        Messages = includeMessages
            ? conversation.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(MessageResponseDto.From)
                .ToList()
            : [],
        Lead = lead is null ? null : LeadResponseDto.From(lead),
    };
}
