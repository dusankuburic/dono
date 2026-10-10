using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManager.Api.Middleware;

/// <summary>
/// Translates domain/grain exceptions into HTTP responses:
/// KeyNotFoundException -> 404, ArgumentException -> 400 (invalid input),
/// InvalidOperationException -> 409 (business rule violation).
/// </summary>
public partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Business rule violation"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Request {Method} {Path} rejected with {StatusCode}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, statusCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError
                ? null // never leak internals for unexpected failures
                : UserFacingMessage(exception)
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    /// <summary>
    /// ArgumentException appends " (Parameter 'name')" to its message, which
    /// means nothing to the user reading the error; drop it. Matched as text
    /// because exceptions rethrown from grains keep the message but lose ParamName.
    /// </summary>
    private static string UserFacingMessage(Exception exception) =>
        exception is ArgumentException
            ? ParameterSuffix().Replace(exception.Message, string.Empty)
            : exception.Message;

    [System.Text.RegularExpressions.GeneratedRegex(@" \(Parameter '[^']*'\)$")]
    private static partial System.Text.RegularExpressions.Regex ParameterSuffix();
}
