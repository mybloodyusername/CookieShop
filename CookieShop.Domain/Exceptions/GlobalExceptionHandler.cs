using System.Security.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CookieShop.Domain.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred.");

        var statusCode = exception switch
        {
            UnauthorizedAccessException or InvalidCredentialException => StatusCodes.Status401Unauthorized,
            // NotFoundException => StatusCodes.Status404NotFound,
            // ValidationException or ConflictException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        await Results.Problem(
                statusCode: statusCode,
                title: statusCode == 500
                    ? "An unexpected error occurred."
                    : exception.Message)
            .ExecuteAsync(httpContext);

        return true;
    }
}