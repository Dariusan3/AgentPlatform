using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface IPropertyRepository
{
    Task<List<Property>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<Property?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Property> CreateAsync(Property property, CancellationToken ct = default);
    Task<Property> UpdateAsync(Property property, CancellationToken ct = default);
    Task DeleteAsync(Property property, CancellationToken ct = default);
    Task<List<Property>> SearchAsync(
        string? search,
        string? type,
        string? city,
        Guid tenantId,
        CancellationToken ct = default);
    Task<int> CountAsync(Guid tenantId, CancellationToken ct = default);
}

public class PropertyRepository : IPropertyRepository
{
    private readonly AppDbContext _db;

    public PropertyRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<Property>> GetAllAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Properties
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public Task<Property?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.Properties
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

    public async Task<Property> CreateAsync(Property property, CancellationToken ct = default)
    {
        _db.Properties.Add(property);
        await _db.SaveChangesAsync(ct);
        return property;
    }

    public async Task<Property> UpdateAsync(Property property, CancellationToken ct = default)
    {
        _db.Properties.Update(property);
        await _db.SaveChangesAsync(ct);
        return property;
    }

    public async Task DeleteAsync(Property property, CancellationToken ct = default)
    {
        _db.Properties.Remove(property);
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<Property>> SearchAsync(
        string? search,
        string? type,
        string? city,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var query = _db.Properties.Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = $"%{search.Trim()}%";
            // ILike face potrivirea case-insensitive in Postgres, fara ToLower()
            query = query.Where(p =>
                EF.Functions.ILike(p.Title ?? string.Empty, needle) ||
                EF.Functions.ILike(p.Neighborhood ?? string.Empty, needle) ||
                EF.Functions.ILike(p.City ?? string.Empty, needle));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(p => p.PropertyType == type);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(p => p.City == city);
        }

        return query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid tenantId, CancellationToken ct = default) =>
        _db.Properties.CountAsync(p => p.TenantId == tenantId, ct);
}
