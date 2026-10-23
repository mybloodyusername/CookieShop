using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CookieShop.App.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace CookieShop.Api.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred.");

        // Npgsql can wrap serializable-transaction failures in InvalidOperationException.
        var databaseConflict = exception.GetBaseException() is PostgresException
            { SqlState: "23505" or "23503" or "23514" or "40001" or "40P01" };
        var statusCode = exception switch
        {
            InvalidCredentialException => StatusCodes.Status401Unauthorized,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            _ when databaseConflict => StatusCodes.Status409Conflict,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            PostgresException { SqlState: "40001" or "40P01" } => StatusCodes.Status409Conflict,
            BadHttpRequestException e => e.StatusCode,
            ConflictException or DuplicateException => StatusCodes.Status409Conflict,
            NotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        await Results.Problem(
                detail: statusCode == 500 ? "An unexpected error occurred." :
                    (databaseConflict || exception is DbUpdateConcurrencyException) ? "A concurrent update occurred. Reload and retry." : exception.Message,
                statusCode: statusCode)
            .ExecuteAsync(httpContext);

        return true;
    }
}