using System.Net;
using System.Net.Http.Json;

namespace BuildingManager.Blazor.Services;

public static class ApiErrors
{
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

        }

        var validationError = problem?.Errors?.Values.SelectMany(e => e).FirstOrDefault();
        throw new InvalidOperationException(problem?.Detail ?? validationError ?? problem?.Title ?? fallbackMessage);
    }

    private sealed record ProblemResponse(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
