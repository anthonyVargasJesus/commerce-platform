using Inventory.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, extensions) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception occurred");
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = httpContext.Request.Path,
        };

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problemDetails.Extensions[key] = value;
            }
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, IDictionary<string, object?>? Extensions) MapException(Exception exception) =>
        exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, null),
            ConflictException => (StatusCodes.Status409Conflict, exception.Message, null),
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                new Dictionary<string, object?> { ["errors"] = validationException.Errors }),
            Domain.Exceptions.DomainException => (StatusCodes.Status422UnprocessableEntity, exception.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null),
        };
}
