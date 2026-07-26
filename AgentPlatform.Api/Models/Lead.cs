using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("leads")]
public class Lead
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("conversation_id")]
    public Guid? ConversationId { get; set; }

    [Column("name")]
    [MaxLength(200)]
    public string? Name { get; set; }

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("email")]
    [MaxLength(255)]
    public string? Email { get; set; }

    [Column("budget_min")]
    public decimal? BudgetMin { get; set; }

    [Column("budget_max")]
    public decimal? BudgetMax { get; set; }

    [Column("preferred_city")]
    [MaxLength(100)]
    public string? PreferredCity { get; set; }

    [Column("preferred_type")]
    [MaxLength(50)]
    public string? PreferredType { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>new | contacted | qualified | lost</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "new";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Agent? Tenant { get; set; }

    [ForeignKey(nameof(ConversationId))]
    public Conversation? Conversation { get; set; }
}
