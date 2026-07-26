using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgentPlatform.Api.Models;

/// <summary>
/// Tenantul. `Id` este `auth.users.id` din Supabase, deci este si TenantId-ul
/// extras din JWT. Randul e creat automat de trigger-ul on_auth_user_created.
/// </summary>
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

    [Column("stripe_customer_id")]
    [MaxLength(100)]
    public string? StripeCustomerId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
