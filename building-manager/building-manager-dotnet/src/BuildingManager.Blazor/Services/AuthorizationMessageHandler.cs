using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace BuildingManager.Blazor.Services;

/// <summary>
/// Attaches the stored JWT to every outgoing API request and, on a 401 from a
/// non-auth endpoint, tries one silent token refresh before giving up and
/// redirecting to the login page.
/// </summary>
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly IAuthService _authService;
    private readonly NavigationManager _navigation;

    public AuthorizationMessageHandler(
        ILocalStorageService localStorage,
        IAuthService authService,
        NavigationManager navigation)
    {
        _localStorage = localStorage;
        _authService = authService;
        _navigation = navigation;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsync<string>(AuthService.TokenStorageKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && !request.RequestUri!.AbsolutePath.Contains("/api/auth/", StringComparison.OrdinalIgnoreCase))
        {
            if (await _authService.TryRefreshAsync(rejectedToken: token))
            {
                response.Dispose();
                var newToken = await _localStorage.GetItemAsync<string>(AuthService.TokenStorageKey);
                using var retry = await CloneForRetryAsync(request, newToken);
                response = await base.SendAsync(retry, cancellationToken);
            }
            else if (!IsOnPublicAuthPage())
            {
                // The layout loads data on every page, including the login
                // page itself; redirecting from there would reload forever.
                _navigation.NavigateTo("login", forceLoad: true);
            }
        }

        return response;
    }

    private static readonly string[] PublicAuthPages =
        { "login", "register", "forgot-password", "reset-password" };

    private bool IsOnPublicAuthPage()
    {
        var path = _navigation.ToBaseRelativePath(_navigation.Uri).Split('?', '#')[0].TrimEnd('/');
        return PublicAuthPages.Contains(path, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Clones a request with buffered content so it can be resent with the new token.</summary>
    private static async Task<HttpRequestMessage> CloneForRetryAsync(HttpRequestMessage request, string? newToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);
            if (request.Content.Headers.ContentType is not null)
            {
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
            }
        }

        foreach (var header in request.Headers)
        {
            if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(newToken))
        {
            clone.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
        }

        return clone;
    }
}
