using System.ComponentModel.DataAnnotations;

namespace AgentPlatform.Api.DTOs;

/// <summary>
/// Un mesaj primit, ca si cum ar fi venit de pe WhatsApp. Cand Twilio va fi
/// conectat, webhookul va construi exact acelasi obiect din datele lui.
/// </summary>
public class SimulateMessageDto
{
    [Required]
    public Guid AiAgentId { get; set; }

    [Required]
    [MaxLength(20)]
    public string ContactPhone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ContactName { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;
}

public class SimulateResultDto
{
    /// <summary>Conversatia cu tot firul, inclusiv mesajul nou si raspunsul.</summary>
    public ConversationResponseDto Conversation { get; set; } = new();

    /// <summary>Raspunsul agentului, sau null daca generarea a eșuat.</summary>
    public string? AiReply { get; set; }

    /// <summary>
    /// De ce nu exista raspuns. Mesajul primit e salvat oricum: a ajuns, deci
    /// trebuie inregistrat chiar daca agentul nu a putut raspunde.
    /// </summary>
    public string? ReplyError { get; set; }
}
