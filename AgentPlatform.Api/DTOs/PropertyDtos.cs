using System.ComponentModel.DataAnnotations;
using AgentPlatform.Api.Models;

namespace AgentPlatform.Api.DTOs;

public class PropertyCreateDto
{
    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, 100_000_000)]
    public decimal PriceEur { get; set; }

    [Range(0, 100_000)]
    public decimal SurfaceSqm { get; set; }

    [Range(0, 100)]
    public int Rooms { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Neighborhood { get; set; }

    /// <summary>apartment | house | land | commercial</summary>
    [Required]
    [MaxLength(50)]
    public string PropertyType { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ListingUrl { get; set; }

    public Guid? AiAgentId { get; set; }
}

public class PropertyUpdateDto : PropertyCreateDto;

/// <summary>
/// Contract non-null catre frontend, chiar daca coloanele din DB sunt nullable.
/// </summary>
public class PropertyResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? AiAgentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PriceEur { get; set; }
    public decimal PriceRon { get; set; }
    public decimal SurfaceSqm { get; set; }
    public int Rooms { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public string PropertyType { get; set; } = string.Empty;
    public string? ListingUrl { get; set; }
    public List<string> Images { get; set; } = [];
    public DateTime CreatedAt { get; set; }

    public static PropertyResponseDto From(Property property) => new()
    {
        Id = property.Id,
        TenantId = property.TenantId,
        AiAgentId = property.AiAgentId,
        Title = property.Title ?? string.Empty,
        Description = property.Description ?? string.Empty,
        PriceEur = property.PriceEur ?? 0,
        PriceRon = property.PriceRon ?? 0,
        SurfaceSqm = property.SurfaceSqm ?? 0,
        Rooms = property.Rooms ?? 0,
        City = property.City ?? string.Empty,
        Neighborhood = property.Neighborhood,
        PropertyType = property.PropertyType ?? string.Empty,
        ListingUrl = property.ListingUrl,
        Images = property.Images,
        CreatedAt = property.CreatedAt,
    };
}
