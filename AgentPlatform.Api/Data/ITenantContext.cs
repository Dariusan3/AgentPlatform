using AgentPlatform.Api.Extensions;

namespace AgentPlatform.Api.Data;

public interface ITenantContext
{
    /// <summary>Tenantul cererii curente, sau null pe rutele publice.</summary>
    Guid? TenantId { get; }
}

/// <summary>
/// Permite fixarea tenantului pe cereri care nu au JWT — concret, webhookul
/// Twilio, care afla tenantul din numarul de WhatsApp, nu din claim-uri.
/// </summary>
public interface ITenantContextSetter
{
    void Override(Guid tenantId);
}

/// <summary>
/// Citeste tenantul din claim-urile cererii. Sursa unica pentru filtrele
/// globale din AppDbContext.
/// </summary>
public class HttpTenantContext : ITenantContext, ITenantContextSetter
{
    private readonly IHttpContextAccessor _accessor;
    private Guid? _overridden;

    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <remarks>
    /// Suprascrierea are prioritate. E scoped, deci nu se scurge intre cereri.
    /// </remarks>
    public Guid? TenantId =>
        _overridden
        ?? (_accessor.HttpContext?.User is { } user && user.TryGetTenantId(out var id)
            ? id
            : null);

    public void Override(Guid tenantId) => _overridden = tenantId;
}
