using AgentPlatform.Api.Data;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Extensions;
using AgentPlatform.Api.Middleware;
using AgentPlatform.Api.OpenApi;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddSupabaseAuth(builder.Configuration);

builder.Services.AddExceptionHandler<UnauthorizedExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityTransformer>();
});

var app = builder.Build();

app.UseExceptionHandler();

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

// Ordinea conteaza: autentificarea populeaza User, tenantul se citeste din el,
// iar autorizarea decide la final daca cererea trece.
app.UseAuthentication();
app.UseTenantContext();
app.UseAuthorization();

app.MapControllers();

app.Run();
