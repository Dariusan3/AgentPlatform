using System.ComponentModel.DataAnnotations;
using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.DTOs;

public class VoiceAgentCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? TwilioPhoneNumber { get; set; }

    /// <summary>Voce Azure. Pentru română: ro-RO-AlinaNeural sau ro-RO-EmilNeural.</summary>
    [MaxLength(50)]
    public string VoiceName { get; set; } = "ro-RO-AlinaNeural";

    public string? SystemPrompt { get; set; }

    public string? GreetingMessage { get; set; }

    [Range(30, 1800)]
    public int MaxCallDurationSeconds { get; set; } = 300;
}

public class VoiceAgentUpdateDto : VoiceAgentCreateDto
{
    public bool IsActive { get; set; } = true;
}

public class VoiceAgentResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TwilioPhoneNumber { get; set; }
    public string VoiceName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string GreetingMessage { get; set; } = string.Empty;
    public int MaxCallDurationSeconds { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public int CallsCount { get; set; }
    public int QualifiedLeadsCount { get; set; }

    public static VoiceAgentResponseDto From(
        VoiceAgent agent,
        int callsCount = 0,
        int qualifiedCount = 0) => new()
    {
        Id = agent.Id,
        TenantId = agent.TenantId,
        Name = agent.Name,
        TwilioPhoneNumber = agent.TwilioPhoneNumber,
        VoiceName = agent.VoiceName,
        SystemPrompt = agent.SystemPrompt ?? string.Empty,
        GreetingMessage = agent.GreetingMessage ?? string.Empty,
        MaxCallDurationSeconds = agent.MaxCallDurationSeconds,
        IsActive = agent.IsActive,
        CreatedAt = agent.CreatedAt,
        CallsCount = callsCount,
        QualifiedLeadsCount = qualifiedCount,
    };
}

/// <summary>Raspunsul la deschiderea unui apel de test din platforma.</summary>
public class VoiceTestCallResponseDto
{
    public Guid CallId { get; set; }
    public string CallSid { get; set; } = string.Empty;

    /// <summary>Adresa WebSocket la care se conecteaza browserul.</summary>
    public string StreamUrl { get; set; } = string.Empty;
}

public class VoiceMessageResponseDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? AudioDurationMs { get; set; }
    public DateTime CreatedAt { get; set; }

    public static VoiceMessageResponseDto From(VoiceMessage message) => new()
    {
        Id = message.Id,
        Role = message.Role,
        Content = message.Content,
        AudioDurationMs = message.AudioDurationMs,
        CreatedAt = message.CreatedAt,
    };
}

public class VoiceCallResponseDto
{
    public Guid Id { get; set; }
    public Guid? VoiceAgentId { get; set; }
    public string CallSid { get; set; } = string.Empty;
    public string CallerPhone { get; set; } = string.Empty;
    public string? CallerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? DurationSeconds { get; set; }
    public string? Transcript { get; set; }
    public bool LeadQualified { get; set; }
    public bool ViewingScheduled { get; set; }
    public DateTime? ViewingDateTime { get; set; }
    public bool SmsSent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public List<VoiceMessageResponseDto> Messages { get; set; } = [];

    public static VoiceCallResponseDto From(VoiceCall call, bool includeMessages = true) => new()
    {
        Id = call.Id,
        VoiceAgentId = call.VoiceAgentId,
        CallSid = call.CallSid,
        CallerPhone = call.CallerPhone,
        CallerName = call.CallerName,
        Status = call.Status,
        DurationSeconds = call.DurationSeconds,
        Transcript = call.Transcript,
        LeadQualified = call.LeadQualified,
        ViewingScheduled = call.ViewingScheduled,
        ViewingDateTime = call.ViewingDateTime,
        SmsSent = call.SmsSent,
        CreatedAt = call.CreatedAt,
        EndedAt = call.EndedAt,
        Messages = includeMessages
            ? call.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(VoiceMessageResponseDto.From)
                .ToList()
            : [],
    };
}
