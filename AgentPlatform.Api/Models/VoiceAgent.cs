using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("voice_agents")]
public class VoiceAgent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Numarul Twilio Voice care raspunde pentru acest agent.</summary>
    [Column("twilio_phone_number")]
    [MaxLength(20)]
    public string? TwilioPhoneNumber { get; set; }

    /// <summary>Voce Azure Speech, ex. ro-RO-AlinaNeural.</summary>
    [Column("voice_name")]
    [MaxLength(50)]
    public string VoiceName { get; set; } = "ro-RO-AlinaNeural";

    [Column("system_prompt")]
    public string? SystemPrompt { get; set; }

    [Column("greeting_message")]
    public string? GreetingMessage { get; set; }

    /// <summary>Plasa de siguranta pe cost: un apel uitat deschis arde credit.</summary>
    [Column("max_call_duration_seconds")]
    public int MaxCallDurationSeconds { get; set; } = 300;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Agent? Tenant { get; set; }
}
