using System.Text.Json;
using AgentPlatform.Api.Exceptions;

namespace AgentPlatform.Api.Middleware;

/// <summary>
/// Traduce excepțiile in raspunsuri JSON uniforme. Mesajele necunoscute nu ies
/// niciodata catre client: un stack trace intr-un 500 e o scurgere de informatie.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteAsync(context, exception);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        // Daca raspunsul a inceput deja, nu mai putem schimba statusul
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Excepție după ce răspunsul a început");
            return;
        }

        var (status, message, errors) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message, null),
            UnauthorizedException ex => (StatusCodes.Status401Unauthorized, ex.Message, null),
            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                ex.Message,
                ex.Errors.Count > 0 ? ex.Errors : null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "A apărut o eroare internă. Am înregistrat-o și o investigăm.",
                (IReadOnlyDictionary<string, string[]>?)null),
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Eroare nehandled pe {Path}", context.Request.Path);
        }
        else
        {
            _logger.LogInformation(
                "{Status} pe {Path}: {Message}",
                status,
                context.Request.Path,
                exception.Message);
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new Dictionary<string, object?>
        {
            ["error"] = message,
            ["statusCode"] = status,
            ["timestamp"] = DateTime.UtcNow.ToString("o"),
        };

        if (errors is not null)
        {
            payload["errors"] = errors;
        }

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, JsonSerializerOptions.Web));
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
