using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Middleware;
using AgentPlatform.Api.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddSupabaseAuth(builder.Configuration);
builder.Services.AddFrontendCors(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityTransformer>();
});

builder.Services.AddRepositories();
builder.Services.AddDomainServices();

var app = builder.Build();

// Primul in lant: prinde tot ce se arunca mai jos, inclusiv din middleware
app.UseExceptionMiddleware();

app.UseCors(ServiceExtensions.CorsPolicy);

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
