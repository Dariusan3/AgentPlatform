using AgentPlatform.Api.Data;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services;
using AgentPlatform.Api.Services.Voice;
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
        // Aceeasi instanta pe ambele interfete: webhookul fixeaza tenantul,
        // iar filtrele globale din AppDbContext il citesc imediat.
        services.AddScoped<HttpTenantContext>();
        services.AddScoped<ITenantContext>(p => p.GetRequiredService<HttpTenantContext>());
        services.AddScoped<ITenantContextSetter>(p => p.GetRequiredService<HttpTenantContext>());

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
        services.AddScoped<IVoiceAgentRepository, VoiceAgentRepository>();
        services.AddScoped<IVoiceCallRepository, VoiceCallRepository>();

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
        services.AddScoped<ITwilioRequestValidator, TwilioRequestValidator>();

        // Agent vocal
        services.AddScoped<IVoiceAgentService, VoiceAgentService>();
        services.AddScoped<IVoiceAiService, VoiceAiService>();
        services.AddScoped<ISmsService, SmsService>();
        // Providerul de sinteza se alege din config, ca sa poti testa local fara
        // cont Azure. Implicit e „azure”: pe un server Linux `say` nu exista, deci
        // o valoare implicita locala ar picat abia in productie.
        services.AddScoped<ITextToSpeechService>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var choice = config["Voice:TtsProvider"]?.Trim().ToLowerInvariant();

            return choice switch
            {
                "macos" => ActivatorUtilities
                    .CreateInstance<MacSayTextToSpeechService>(provider),
                null or "" or "azure" => ActivatorUtilities
                    .CreateInstance<TextToSpeechService>(provider),
                _ => throw new InvalidOperationException(
                    $"Voice:TtsProvider „{choice}” nu există. Valori acceptate: azure, macos."),
            };
        });
        services.AddScoped<VoiceStreamHandler>();

        // Transcrierea trimite fisiere audio, deci are nevoie de timeout mai lung
        services.AddHttpClient<ISpeechService, SpeechService>(http =>
            http.Timeout = TimeSpan.FromSeconds(60));

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
