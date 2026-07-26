using System.ComponentModel.DataAnnotations;
using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.DTOs;

public class ProfileResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Phone { get; set; }
    public string Plan { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public static ProfileResponseDto From(Agent agent) => new()
    {
        Id = agent.Id,
        Email = agent.Email,
        FullName = agent.FullName,
        CompanyName = agent.CompanyName,
        Phone = agent.Phone,
        Plan = agent.Plan,
        CreatedAt = agent.CreatedAt,
    };
}

public class ProfileUpdateDto
{
    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }
}

public class UsageResponseDto
{
    public string Month { get; set; } = string.Empty;
    public int MessagesCount { get; set; }
    public long TokensUsed { get; set; }
    public int LeadsGenerated { get; set; }

    /// <summary>
    /// Limitele planului. `null` inseamna nelimitat - un numar uriaș ar face
    /// frontendul sa deseneze o bara de progres fara sens.
    /// </summary>
    public int? MessagesLimit { get; set; }
    public int? LeadsLimit { get; set; }
    public string Plan { get; set; } = string.Empty;
}

public class PasswordUpdateDto
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
