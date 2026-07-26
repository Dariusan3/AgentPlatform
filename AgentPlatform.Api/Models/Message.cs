using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("messages")]
public class Message
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("conversation_id")]
    public Guid ConversationId { get; set; }

    /// <summary>user | assistant | system</summary>
    [Column("role")]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("tokens_used")]
    public int? TokensUsed { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Necesara pentru filtrul global: `messages` nu are tenant_id, deci
    /// izolarea se face prin conversatia parinte.
    /// </summary>
    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
}
