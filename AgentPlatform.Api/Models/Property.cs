using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace AgentPlatform.Api.Models;

/// <summary>
/// Listare imobiliara. Coloanele de date sunt nullable in DB, deci si aici -
/// altfel EF arunca la materializare pe orice rand incomplet. Contractul
/// non-null e impus in DTO-uri, nu in model.
/// </summary>
[Table("properties")]
public class Property
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("tenant_id")]
    public Guid TenantId { get; set; }

    [Column("ai_agent_id")]
    public Guid? AiAgentId { get; set; }

    [Column("title")]
    [MaxLength(300)]
    public string? Title { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("price_eur")]
    public decimal? PriceEur { get; set; }

    [Column("price_ron")]
    public decimal? PriceRon { get; set; }

    [Column("surface_sqm")]
    public decimal? SurfaceSqm { get; set; }

    [Column("rooms")]
    public int? Rooms { get; set; }

    [Column("city")]
    [MaxLength(100)]
    public string? City { get; set; }

    [Column("neighborhood")]
    [MaxLength(100)]
    public string? Neighborhood { get; set; }

    /// <summary>apartment | house | land | commercial</summary>
    [Column("property_type")]
    [MaxLength(50)]
    public string? PropertyType { get; set; }

    [Column("listing_url")]
    public string? ListingUrl { get; set; }

    /// <summary>Serializat in jsonb printr-un value converter din AppDbContext.</summary>
    [Column("images")]
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// vector(1536) pentru cautare semantica. Tipul e Pgvector.Vector, nu
    /// float[]: Npgsql nu poate mapa un array de float pe tipul `vector`.
    /// </summary>
    [Column("embedding")]
    public Vector? Embedding { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Agent? Tenant { get; set; }

    [ForeignKey(nameof(AiAgentId))]
    public AiAgent? AiAgent { get; set; }
}
