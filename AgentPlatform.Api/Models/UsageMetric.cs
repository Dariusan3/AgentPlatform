using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("usage_metrics")]
public class UsageMetric
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    /// <summary>Format "2026-07". Unic per tenant, vezi UNIQUE(tenant_id, month).</summary>
    [Column("month")]
    [MaxLength(7)]
    public string Month { get; set; } = string.Empty;

    [Column("messages_count")]
    public int MessagesCount { get; set; }

    [Column("tokens_used")]
    public long TokensUsed { get; set; }

    [Column("leads_generated")]
    public int LeadsGenerated { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Agent? Tenant { get; set; }
}
