using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Models;
using AgentPlatform.Api.Repositories;

namespace AgentPlatform.Api.Services;

public interface IPropertyService
{
    Task<List<PropertyResponseDto>> GetAllAsync(
        Guid tenantId,
        string? search,
        string? type,
        string? city,
        CancellationToken ct = default);
    Task<PropertyResponseDto> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<PropertyResponseDto> CreateAsync(
        PropertyCreateDto dto,
        Guid tenantId,
        CancellationToken ct = default);
    Task<PropertyResponseDto> UpdateAsync(
        Guid id,
        PropertyUpdateDto dto,
        Guid tenantId,
        CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}

public class PropertyService : IPropertyService
{
    /// <summary>
    /// Cursul e fix deocamdata. Cand va conta, se ia din BNR si se cacheaza zilnic;
    /// pana atunci o constanta e mai onesta decat un serviciu care nu face nimic.
    /// </summary>
    public const decimal EurToRon = 4.97m;

    private static readonly string[] AllowedTypes =
        ["apartment", "house", "land", "commercial"];

    private readonly IPropertyRepository _properties;
    private readonly IAiAgentRepository _aiAgents;

    public PropertyService(IPropertyRepository properties, IAiAgentRepository aiAgents)
    {
        _properties = properties;
        _aiAgents = aiAgents;
    }

    public async Task<List<PropertyResponseDto>> GetAllAsync(
        Guid tenantId,
        string? search,
        string? type,
        string? city,
        CancellationToken ct = default)
    {
        var hasFilters = !string.IsNullOrWhiteSpace(search)
                         || !string.IsNullOrWhiteSpace(type)
                         || !string.IsNullOrWhiteSpace(city);

        var items = hasFilters
            ? await _properties.SearchAsync(search, type, city, tenantId, ct)
            : await _properties.GetAllAsync(tenantId, ct);

        return items.Select(PropertyResponseDto.From).ToList();
    }

    public async Task<PropertyResponseDto> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var property = await _properties.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Property();

        return PropertyResponseDto.From(property);
    }

    public async Task<PropertyResponseDto> CreateAsync(
        PropertyCreateDto dto,
        Guid tenantId,
        CancellationToken ct = default)
    {
        await ValidateAsync(dto, tenantId, ct);

        var property = new Property
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AiAgentId = dto.AiAgentId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            PriceEur = dto.PriceEur,
            PriceRon = Math.Round(dto.PriceEur * EurToRon, 2),
            SurfaceSqm = dto.SurfaceSqm,
            Rooms = dto.Rooms,
            City = dto.City.Trim(),
            Neighborhood = dto.Neighborhood?.Trim(),
            PropertyType = dto.PropertyType,
            ListingUrl = dto.ListingUrl?.Trim(),
            CreatedAt = DateTime.UtcNow,
            // TODO: genereaza embedding din Title + Description si scrie-l aici,
            // ca sa intre in cautarea vectoriala. Vezi IEmbeddingService.
            Embedding = null,
        };

        var created = await _properties.CreateAsync(property, ct);
        return PropertyResponseDto.From(created);
    }

    public async Task<PropertyResponseDto> UpdateAsync(
        Guid id,
        PropertyUpdateDto dto,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var property = await _properties.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Property();

        await ValidateAsync(dto, tenantId, ct);

        property.AiAgentId = dto.AiAgentId;
        property.Title = dto.Title.Trim();
        property.Description = dto.Description?.Trim();
        property.PriceEur = dto.PriceEur;
        property.PriceRon = Math.Round(dto.PriceEur * EurToRon, 2);
        property.SurfaceSqm = dto.SurfaceSqm;
        property.Rooms = dto.Rooms;
        property.City = dto.City.Trim();
        property.Neighborhood = dto.Neighborhood?.Trim();
        property.PropertyType = dto.PropertyType;
        property.ListingUrl = dto.ListingUrl?.Trim();
        // TODO: regenereaza embedding-ul daca s-au schimbat titlul sau descrierea

        var updated = await _properties.UpdateAsync(property, ct);
        return PropertyResponseDto.From(updated);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var property = await _properties.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Property();

        await _properties.DeleteAsync(property, ct);
    }

    private async Task ValidateAsync(
        PropertyCreateDto dto,
        Guid tenantId,
        CancellationToken ct)
    {
        if (!AllowedTypes.Contains(dto.PropertyType))
        {
            throw ValidationException.ForField(
                nameof(dto.PropertyType),
                $"Tip invalid. Acceptate: {string.Join(", ", AllowedTypes)}.");
        }

        // Fara asta, un tenant ar putea lega proprietatea de agentul altcuiva
        if (dto.AiAgentId is { } agentId
            && await _aiAgents.GetByIdAsync(agentId, tenantId, ct) is null)
        {
            throw ValidationException.ForField(
                nameof(dto.AiAgentId),
                "Agentul AI indicat nu există în contul tău.");
        }
    }
}
