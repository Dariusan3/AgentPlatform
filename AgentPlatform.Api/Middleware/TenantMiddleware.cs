using AgentPlatform.Api.Extensions;

namespace AgentPlatform.Api.Middleware;

/// <summary>
/// Pune tenantul in HttpContext.Items, ca serviciile care primesc doar
/// IHttpContextAccessor sa nu repete extragerea din claim-uri.
/// </summary>
/// <remarks>
/// Trebuie inregistrat DUPA UseAuthentication: inainte de ea, User este
/// neautenticat si nu are niciun claim.
/// </remarks>
public class TenantMiddleware
{
    public const string ItemKey = "TenantId";

    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Rutele publice (ex. /api/test/ping) trec fara tenant, nu e o eroare
        if (context.User.TryGetTenantId(out var tenantId))
        {
            context.Items[ItemKey] = tenantId;
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
        => app.UseMiddleware<TenantMiddleware>();

    /// <summary>Tenantul pus de TenantMiddleware, sau null pe cereri neautenticate.</summary>
    public static Guid? GetTenantId(this HttpContext context)
        => context.Items[TenantMiddleware.ItemKey] as Guid?;
}
