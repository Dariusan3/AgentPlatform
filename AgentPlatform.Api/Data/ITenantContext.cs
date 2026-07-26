using AgentPlatform.Api.Extensions;

namespace AgentPlatform.Api.Data;

public interface ITenantContext
{
    /// <summary>Tenantul cererii curente, sau null pe rutele publice.</summary>
    Guid? TenantId { get; }
}

/// <summary>
/// Citeste tenantul din claim-urile cererii. Sursa unica pentru filtrele
/// globale din AppDbContext.
/// </summary>
public class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? TenantId =>
        _accessor.HttpContext?.User is { } user && user.TryGetTenantId(out var id)
            ? id
            : null;
}
