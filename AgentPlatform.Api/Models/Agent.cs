using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

[Table("agents")]
public class Agent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("full_name")]
    [MaxLength(200)]
    public string? FullName { get; set; }

    [Column("company_name")]
    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("plan")]
    [MaxLength(20)]
    public string Plan { get; set; } = "starter";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
