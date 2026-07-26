using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("voice_messages")]
public class VoiceMessage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("voice_call_id")]
    public Guid VoiceCallId { get; set; }

    /// <summary>caller | agent</summary>
    [Column("role")]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("audio_duration_ms")]
    public int? AudioDurationMs { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>Necesara pentru filtrul global: voice_messages nu are tenant_id.</summary>
    [ForeignKey(nameof(VoiceCallId))]
    public VoiceCall? VoiceCall { get; set; }
}
