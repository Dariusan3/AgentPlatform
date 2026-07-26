using System.Text.Json;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AgentPlatform.Api.Data;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AiAgent> AiAgents => Set<AiAgent>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<UsageMetric> UsageMetrics => Set<UsageMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Lista de imagini merge in jsonb printr-un converter explicit, ca sa nu
        // depindem de EnableDynamicJson pe data source.
        var imagesConverter = new ValueConverter<List<string>, string>(
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Default),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonSerializerOptions.Default)
                    ?? new List<string>());

        modelBuilder.Entity<Property>(entity =>
        {
            entity
                .Property(p => p.Images)
                .HasColumnType("jsonb")
                .HasConversion(imagesConverter);

            entity.Property(p => p.Embedding).HasColumnType("vector(1536)");
        });

        modelBuilder.Entity<Conversation>()
            .HasMany(c => c.Messages)
            .WithOne(m => m.Conversation!)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UsageMetric>()
            .HasIndex(u => new { u.TenantId, u.Month })
            .IsUnique();

        ApplyTenantFilters(modelBuilder);
    }

    /// <summary>
    /// API-ul se conecteaza cu service_role, care OCOLESTE RLS. Fara filtrele
    /// astea, un singur query fara WHERE tenant_id ar returna datele tuturor
    /// clientilor. Repository-urile filtreaza si ele explicit; asta e plasa de
    /// siguranta pentru cazul in care cineva uita.
    /// </summary>
    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>().HasQueryFilter(a => a.Id == _tenant.TenantId);
        modelBuilder.Entity<AiAgent>().HasQueryFilter(a => a.TenantId == _tenant.TenantId);
        modelBuilder.Entity<Property>().HasQueryFilter(p => p.TenantId == _tenant.TenantId);
        modelBuilder.Entity<Conversation>().HasQueryFilter(c => c.TenantId == _tenant.TenantId);
        modelBuilder.Entity<Lead>().HasQueryFilter(l => l.TenantId == _tenant.TenantId);
        modelBuilder.Entity<UsageMetric>().HasQueryFilter(u => u.TenantId == _tenant.TenantId);

        // messages nu are tenant_id: se filtreaza prin conversatia parinte
        modelBuilder.Entity<Message>()
            .HasQueryFilter(m => m.Conversation!.TenantId == _tenant.TenantId);
    }
}
