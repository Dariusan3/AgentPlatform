using System.Text;
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
            });

        services.AddAuthorization();

        return services;
    }
}
