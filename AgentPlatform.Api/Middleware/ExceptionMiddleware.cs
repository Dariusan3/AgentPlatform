using System.Data.Common;
using System.Net.Sockets;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Http;
using Npgsql;

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

        // Clientul a inchis pagina: nu e o eroare a noastra si nu mai are cine citi
        if (context.RequestAborted.IsCancellationRequested
            && exception is OperationCanceledException)
        {
            _logger.LogDebug("Cerere abandonată de client pe {Path}", context.Request.Path);
            return;
        }

        var (status, message, errors) = Describe(exception);

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

        await ApiError.WriteAsync(context, status, message, errors);
    }

    /// <remarks>
    /// Problemele de infrastructura primesc mesaje proprii, nu „eroare interna”:
    /// cand baza sau un serviciu extern e picat, utilizatorul trebuie sa afle ca
    /// nu e vina datelor lui si ca merita reincercat.
    /// </remarks>
    private static (int Status, string Message, IReadOnlyDictionary<string, string[]>? Errors)
        Describe(Exception exception) => exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message, null),

            UnauthorizedException ex => (StatusCodes.Status401Unauthorized, ex.Message, null),

            ValidationException ex => (
                StatusCodes.Status400BadRequest,
                ex.Message,
                ex.Errors.Count > 0 ? ex.Errors : null),

            // Baza de date: pooler picat, retea cazuta, prea multe conexiuni
            NpgsqlException or DbException or SocketException => (
                StatusCodes.Status503ServiceUnavailable,
                "Baza de date nu răspunde acum. Încearcă din nou în câteva momente.",
                null),

            // Groq, Twilio, Azure Speech, serviciile de push
            HttpRequestException => (
                StatusCodes.Status502BadGateway,
                "Un serviciu extern nu a răspuns. Încearcă din nou în câteva momente.",
                null),

            TimeoutException or TaskCanceledException or OperationCanceledException => (
                StatusCodes.Status504GatewayTimeout,
                "Operațiunea a durat prea mult și a fost oprită. Încearcă din nou.",
                null),

            // EF impacheteaza eroarea reala: DbUpdateException peste PostgresException,
            // HttpRequestException peste SocketException. Cautam si in interior.
            { InnerException: not null } ex => Describe(ex.InnerException),

            _ => (
                StatusCodes.Status500InternalServerError,
                "A apărut o eroare internă. Am înregistrat-o și o investigăm.",
                null),
        };
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}
