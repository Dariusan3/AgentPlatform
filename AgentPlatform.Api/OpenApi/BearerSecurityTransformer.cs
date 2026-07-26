using Microsoft.AspNetCore.OpenApi;
// In Microsoft.OpenApi 2.0 tipurile au ieșit din subnamespace-ul .Models
using Microsoft.OpenApi;

namespace AgentPlatform.Api.OpenApi;

/// <summary>
/// Declara autentificarea Bearer in documentul OpenAPI, ca Scalar sa afiseze
/// un camp de token si sa trimita singur headerul Authorization.
/// </summary>
public class BearerSecurityTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description =
                "Access token-ul din Supabase. In Scalar lipesti doar tokenul, " +
                "fara prefixul \"Bearer\"."
        };

        return Task.CompletedTask;
    }
}
