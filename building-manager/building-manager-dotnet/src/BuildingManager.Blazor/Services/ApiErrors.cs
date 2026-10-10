using System.Net;
using System.Net.Http.Json;

namespace BuildingManager.Blazor.Services;

/// <summary>Turns API error responses into exceptions with a message fit to show the user.</summary>
public static class ApiErrors
{
    /// <summary>
    /// Throws InvalidOperationException on a non-success response, using the
    /// API's ProblemDetails detail (business rule messages), the first
    /// validation error, or the fallback.
    /// </summary>
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        switch (response.StatusCode)
        {
            case HttpStatusCode.TooManyRequests:
                throw new InvalidOperationException("Too many attempts. Please wait a minute and try again.");
            case HttpStatusCode.Forbidden:
                throw new InvalidOperationException("You don't have permission to do that.");
        }

        ProblemResponse? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        }
        catch
        {
            // Not a ProblemDetails body; use the fallback message.
        }

        var validationError = problem?.Errors?.Values.SelectMany(e => e).FirstOrDefault();
        throw new InvalidOperationException(problem?.Detail ?? validationError ?? problem?.Title ?? fallbackMessage);
    }

    private sealed record ProblemResponse(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
