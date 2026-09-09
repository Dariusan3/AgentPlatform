using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Http;
using AgentPlatform.Api.Middleware;
using AgentPlatform.Api.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSupabaseAuth(builder.Configuration);
builder.Services.AddFrontendCors(builder.Configuration);

// Filtru global de autorizare: un controller nou fara [Authorize] ramane inchis,
// nu deschis. [AllowAnonymous] il ocoleste, deci webhookurile Twilio merg mai
// departe. Nu folosim FallbackPolicy fiindca aceea prinde si cererile care nu
// nimeresc nicio ruta, si atunci un 404 sau un 405 ar iesi drept 401.
builder.Services.AddControllers(options =>
    options.Filters.Add(new AuthorizeFilter(
        new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())));

// Raspunsul implicit pentru un model invalid e un ProblemDetails in engleza.
// Il inlocuim cu aceeasi forma ca restul erorilor, cu mesaje in romana.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Fara asta, statusurile pe care le intoarce singur MVC (415, de exemplu)
    // pleaca drept ProblemDetails in engleza, pe langa formatul nostru.
    options.SuppressMapClientErrors = true;

    options.InvalidModelStateResponseFactory = context =>
    {
        var (message, errors) = ValidationMessages.From(
            context.ModelState,
            context.ActionDescriptor.Parameters.Select(parameter => parameter.Name));

        return new BadRequestObjectResult(new Dictionary<string, object?>
        {
            ["error"] = message,
            ["statusCode"] = StatusCodes.Status400BadRequest,
            ["timestamp"] = DateTime.UtcNow.ToString("o"),
            ["errors"] = errors,
        });
    };
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityTransformer>();
});

builder.Services.AddRepositories();
builder.Services.AddDomainServices();

var app = builder.Build();

// Primul in lant: prinde tot ce se arunca mai jos, inclusiv din middleware
app.UseExceptionMiddleware();

// Statusurile produse de pipeline (404 pe ruta, 405, 415) pleaca implicit cu
// corpul gol, iar clientul nu are ce afisa. Le dam acelasi JSON ca restul.
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;

    await ApiError.WriteAsync(
        context.HttpContext,
        response.StatusCode,
        ApiError.ForStatus(response.StatusCode, context.HttpContext.Request.Method));
});

app.UseCors(ServiceExtensions.CorsPolicy);

// Twilio Media Streams: audio bidirectional prin WebSocket.
// KeepAlive sub 30s: unele proxy-uri taie conexiunile inactive.
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(20),
});

// Ordinea conteaza: autentificarea populeaza User, tenantul se citeste din el,
// iar autorizarea decide la final daca cererea trece.
app.UseAuthentication();
app.UseTenantContext();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "AgentPlatform API";
        options.Theme = ScalarTheme.Purple;
    });

    // Radacina duce direct in Scalar, ca sa nu conteze cum pornesti aplicatia
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.MapControllers();

app.Run();
