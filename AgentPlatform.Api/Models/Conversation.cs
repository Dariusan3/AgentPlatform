using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("conversations")]
public class Conversation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("ai_agent_id")]
    public Guid? AiAgentId { get; set; }

    [Column("contact_phone")]
    [MaxLength(20)]
    public string? ContactPhone { get; set; }

    [Column("contact_name")]
    [MaxLength(200)]
    public string? ContactName { get; set; }

    [Column("channel")]
    [MaxLength(20)]
    public string Channel { get; set; } = "whatsapp";

    /// <summary>active | closed | converted</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "active";

    [Column("lead_score")]
    public int? LeadScore { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("last_message_at")]
    public DateTime? LastMessageAt { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Agent? Tenant { get; set; }

    [ForeignKey(nameof(AiAgentId))]
    public AiAgent? AiAgent { get; set; }

    public List<Message> Messages { get; set; } = [];
}
