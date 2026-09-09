using System.Text;
using AgentPlatform.Api.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AgentPlatform.Api.Extensions;

public static class AuthExtensions
{
    /// <summary>
    /// Validare pentru tokenurile emise de Supabase Auth.
    /// </summary>
    /// <remarks>
    /// Proiectele Supabase noi semneaza asimetric (ES256) si publica cheile in
    /// JWKS. Acolo `Authority` face discovery si roteste cheile singur, deci nu
    /// tinem nicio cheie in configurare.
    ///
    /// Proiectele vechi semneaza HS256 cu un secret partajat. Il folosim doar
    /// daca `Supabase:JwtSecret` e completat explicit.
    /// </remarks>
    public static IServiceCollection AddSupabaseAuth(
        this IServiceCollection services,
        IConfiguration config)
    {
        var supabaseUrl = config["Supabase:Url"]
            ?? throw new InvalidOperationException(
                "Supabase:Url lipseste din configuratie.");

        var issuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
        var legacySecret = config["Supabase:JwtSecret"];

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Supabase pune iss si aud in token, deci le verificam:
                    // altfel am accepta tokenuri de la orice alt proiect.
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                if (string.IsNullOrWhiteSpace(legacySecret))
                {
                    // Calea normala: cheile ES256 sunt luate din
                    // /auth/v1/.well-known/openid-configuration
                    options.Authority = issuer;
                }
                else
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(legacySecret));
                }

                options.Events = BearerEvents();
            });

        services.AddAuthorization();

        return services;
    }

    /// <remarks>
    /// Fara asta, un 401 pleaca cu corpul gol si frontendul nu are ce afisa.
    /// Distingem si motivul: token expirat, token invalid sau lipsa completa —
    /// altfel utilizatorul nu stie daca trebuie sa se reconecteze sau sa astepte.
    /// </remarks>
    private static JwtBearerEvents BearerEvents() => new()
    {
        OnAuthenticationFailed = context =>
        {
            context.HttpContext.Items[FailureReasonKey] = context.Exception switch
            {
                SecurityTokenExpiredException =>
                    "Sesiunea a expirat. Conectează-te din nou.",
                SecurityTokenInvalidIssuerException
                    or SecurityTokenInvalidAudienceException =>
                    "Sesiunea nu e valabilă pentru această aplicație. " +
                    "Conectează-te din nou.",
                SecurityTokenSignatureKeyNotFoundException =>
                    "Nu am putut verifica sesiunea: cheile Supabase nu au putut fi " +
                    "citite. Verifică legătura la internet și încearcă din nou.",
                _ => "Sesiunea nu mai e validă. Conectează-te din nou.",
            };

            return Task.CompletedTask;
        },

        OnChallenge = async context =>
        {
            // Preluam noi raspunsul: implicit ar fi 401 gol, cu un header WWW-Authenticate
            context.HandleResponse();

            var message = context.HttpContext.Items[FailureReasonKey] as string
                ?? "Trebuie să fii conectat pentru asta.";

            await ApiError.WriteAsync(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                message);
        },

        OnForbidden = context => ApiError.WriteAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Contul tău nu are acces la resursa asta."),
    };

    /// <summary>Motivul esecului, pasat din OnAuthenticationFailed in OnChallenge.</summary>
    private const string FailureReasonKey = "AuthFailureReason";
}
