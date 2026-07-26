using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("ai_agents")]
public class AiAgent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("persona")]
    public string? Persona { get; set; }

    [Column("tone")]
    [MaxLength(50)]
    public string Tone { get; set; } = "professional";

    [Column("language")]
    [MaxLength(10)]
    public string Language { get; set; } = "ro";

    [Column("whatsapp_number")]
    [MaxLength(20)]
    public string? WhatsAppNumber { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Agent? Tenant { get; set; }
}
