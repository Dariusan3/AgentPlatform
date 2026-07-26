using AgentPlatform.Api.Data;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Extensions;

public static class ServiceExtensions
{
    public const string CorsPolicy = "frontend";

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection lipseste din configuratie.");

        // ITenantContext e citit de filtrele globale din AppDbContext
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IAiAgentRepository, AiAgentRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        return services;
    }

    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IAiAgentService, AiAgentService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAiReplyService, AiReplyService>();

        services.AddHttpClient<IGroqClient, GroqClient>(http =>
            // Modelele mari pot depasi 30s la prompturi lungi
            http.Timeout = TimeSpan.FromSeconds(60));

        // Fara ConfigureHttpClient: clientul isi citeste configurarea la apel,
        // ca un secret lipsa sa nu doboare tot SettingsController.
        services.AddHttpClient<ISupabaseAdminClient, SupabaseAdminClient>();

        return services;
    }

    /// <summary>
    /// Originile vin din configurare, nu hardcodate: in producție frontendul nu
    /// va mai fi pe localhost.
    /// </summary>
    public static IServiceCollection AddFrontendCors(
        this IServiceCollection services,
        IConfiguration config)
    {
        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? ["http://localhost:5173"];

        services.AddCors(options =>
            options.AddPolicy(CorsPolicy, policy => policy
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()));

        return services;
    }
}
