using System.Security.Claims;
using AgentPlatform.Api.Exceptions;

namespace AgentPlatform.Api.Extensions;

public static class ClaimsExtensions
{
    /// <summary>
    /// `sub` din tokenul Supabase este `auth.users.id`, care este si
    /// `agents.id`, deci este tenant_id-ul. Vezi RLS in supabase/migrations.
    /// </summary>
    /// <remarks>
    /// JwtBearer are MapInboundClaims activ implicit, deci `sub` ajunge
    /// remapat pe ClaimTypes.NameIdentifier. Cautam ambele nume, ca metoda sa
    /// nu se rupa daca maparea e schimbata ulterior.
    /// </remarks>
    public static Guid GetTenantId(this ClaimsPrincipal user)
    {
        if (!user.TryGetTenantId(out var tenantId))
        {
            throw new UnauthorizedException(
                "Tokenul nu contine un claim 'sub' valid, deci nu putem stabili tenantul.");
        }

        return tenantId;
    }

    public static bool TryGetTenantId(this ClaimsPrincipal user, out Guid tenantId)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(raw, out tenantId);
    }

    /// <summary>Emailul din token, sau null daca lipseste (ex. login pe telefon).</summary>
    public static string? GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");
}
