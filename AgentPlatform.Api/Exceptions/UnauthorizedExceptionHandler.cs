using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AgentPlatform.Api.Exceptions;

/// <summary>
/// Fara asta, un token valid dar fara `sub` ar da 500, adica am da vina pe
/// server pentru o problema a cererii.
/// </summary>
public class UnauthorizedExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not UnauthorizedException)
        {
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Token incomplet",
                Detail = exception.Message
            },
            cancellationToken);

        return true;
    }
}
