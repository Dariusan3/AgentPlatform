using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("voice_calls")]
public class VoiceCall
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("voice_agent_id")]
    public Guid? VoiceAgentId { get; set; }

    /// <summary>
    /// Identificatorul Twilio al apelului. Leaga webhookul de conexiunea
    /// WebSocket, care se deschide ca o cerere separata.
    /// </summary>
    [Column("call_sid")]
    [MaxLength(64)]
    public string CallSid { get; set; } = string.Empty;

    /// <summary>Vine abia in evenimentul `start` al stream-ului, nu din webhook.</summary>
    [Column("stream_sid")]
    [MaxLength(64)]
    public string? StreamSid { get; set; }

    [Column("caller_phone")]
    [MaxLength(20)]
    public string CallerPhone { get; set; } = string.Empty;

    [Column("caller_name")]
    [MaxLength(200)]
    public string? CallerName { get; set; }

    /// <summary>ringing | active | completed | failed</summary>
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ringing";

    [Column("duration_seconds")]
    public int? DurationSeconds { get; set; }

    [Column("transcript")]
    public string? Transcript { get; set; }

    [Column("lead_qualified")]
    public bool LeadQualified { get; set; }

    [Column("viewing_scheduled")]
    public bool ViewingScheduled { get; set; }

    [Column("viewing_date_time")]
    public DateTime? ViewingDateTime { get; set; }

    [Column("sms_sent")]
    public bool SmsSent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }

    [ForeignKey(nameof(VoiceAgentId))]
    public VoiceAgent? VoiceAgent { get; set; }

    public List<VoiceMessage> Messages { get; set; } = [];
}
