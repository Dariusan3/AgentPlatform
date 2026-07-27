using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("notifications")]
public class Notification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    /// <summary>Vezi <see cref="NotificationTypes"/>.</summary>
    [Column("type")]
    [MaxLength(40)]
    public string Type { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>info | success | warning | error</summary>
    [Column("severity")]
    [MaxLength(10)]
    public string Severity { get; set; } = "info";

    /// <summary>Ruta din aplicatie catre entitatea care a generat notificarea.</summary>
    [Column("link")]
    [MaxLength(200)]
    public string? Link { get; set; }

    /// <summary>Null cat timp e necitita. Data, nu boolean: arata si cand ai citit.</summary>
    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("notification_preferences")]
public class NotificationPreference
{
    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("type")]
    [MaxLength(40)]
    public string Type { get; set; } = string.Empty;

    [Column("in_app")]
    public bool InApp { get; set; } = true;

    [Column("push")]
    public bool Push { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Un browser abonat la Web Push. Un cont poate avea mai multe: laptop, telefon,
/// alt browser.
/// </summary>
[Table("push_subscriptions")]
public class PushSubscription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    /// <summary>Adresa serviciului de push al browserului (Google, Mozilla, Apple).</summary>
    [Column("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Cheia publica a browserului, pentru schimbul de chei ECDH.</summary>
    [Column("p256dh")]
    public string P256dh { get; set; } = string.Empty;

    /// <summary>Secretul de autentificare, folosit la derivarea cheii de criptare.</summary>
    [Column("auth")]
    public string Auth { get; set; } = string.Empty;

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("last_used_at")]
    public DateTime? LastUsedAt { get; set; }
}
