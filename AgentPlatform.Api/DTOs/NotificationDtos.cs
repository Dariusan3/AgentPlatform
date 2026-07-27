using System.ComponentModel.DataAnnotations;
using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.DTOs;

public class NotificationResponseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool Read { get; set; }
    public DateTime CreatedAt { get; set; }

    public static NotificationResponseDto From(Notification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        Title = notification.Title,
        Body = notification.Body,
        Severity = notification.Severity,
        Link = notification.Link,
        Read = notification.ReadAt is not null,
        CreatedAt = notification.CreatedAt,
    };
}

public class NotificationListDto
{
    /// <summary>Numarul din clopotel. Nu depinde de cate elemente s-au cerut.</summary>
    public int UnreadCount { get; set; }

    public List<NotificationResponseDto> Items { get; set; } = [];
}

public class NotificationPreferenceDto
{
    public string Type { get; set; } = string.Empty;

    /// <summary>activitate | apeluri | operational</summary>
    public string Group { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool InApp { get; set; }
    public bool Push { get; set; }
}

public class NotificationPreferenceUpdateDto
{
    [Required]
    [MaxLength(40)]
    public string Type { get; set; } = string.Empty;

    public bool InApp { get; set; } = true;
    public bool Push { get; set; }
}

/// <summary>Abonamentul primit de la browser, dupa `pushManager.subscribe()`.</summary>
public class PushSubscriptionCreateDto
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Cheia publica a browserului, base64url.</summary>
    [Required]
    public string P256dh { get; set; } = string.Empty;

    /// <summary>Secretul de autentificare al browserului, base64url.</summary>
    [Required]
    public string Auth { get; set; } = string.Empty;
}

/// <summary>Ce ii trebuie browserului ca sa se poata abona.</summary>
public class PushConfigDto
{
    public bool Enabled { get; set; }

    /// <summary>Cheia publica VAPID. Null cand Web Push nu e configurat.</summary>
    public string? PublicKey { get; set; }
}
