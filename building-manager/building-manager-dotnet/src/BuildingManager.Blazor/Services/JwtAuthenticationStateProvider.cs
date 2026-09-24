using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BuildingManager.Blazor.Services;

/// <summary>
/// Builds the client-side authentication state from the JWT stored in local
/// storage. Signature validation is not possible (nor needed) here: the API
/// validates the token on every call; this only drives the UI.
/// </summary>
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;

    public JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>(AuthService.TokenStorageKey);
            if (string.IsNullOrWhiteSpace(token))
            {
                return Anonymous();
            }

            var claims = JwtParser.ParseClaims(token);
            if (JwtParser.IsExpired(claims))
            {
                await _localStorage.RemoveItemAsync(AuthService.TokenStorageKey);
                return Anonymous();
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return Anonymous();
        }
    }

    public Task NotifyLoggedInAsync(string token)
    {
        var claims = JwtParser.ParseClaims(token);
        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        var state = new AuthenticationState(new ClaimsPrincipal(identity));
        NotifyAuthenticationStateChanged(Task.FromResult(state));
        return Task.CompletedTask;
    }

    public Task NotifyLoggedOutAsync()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
        return Task.CompletedTask;
    }

    private static AuthenticationState Anonymous() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));
}
