using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Orleans.Interfaces;

namespace BuildingManager.Blazor.Services;

public interface IAuthService
{
    /// <summary>Logs in and stores the token. Throws HttpRequestException on bad credentials.</summary>
    Task<AuthResponse> LoginAsync(string email, string password);

    /// <summary>Registers, auto-logs-in and stores the token.</summary>
    Task<AuthResponse> RegisterAsync(string email, string displayName, string password, UserRole role);

    /// <summary>Revokes the session server-side (best effort) and clears the local token.</summary>
    Task LogoutAsync();

    Task<string?> GetTokenAsync();
}

public class AuthService : IAuthService
{
    public const string TokenStorageKey = "auth-token";

    private readonly HttpClient _httpClient;
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
    private readonly JwtAuthenticationStateProvider _stateProvider;

    public AuthService(
        HttpClient httpClient,
        Blazored.LocalStorage.ILocalStorageService localStorage,
        JwtAuthenticationStateProvider stateProvider)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _stateProvider = stateProvider;
    }

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("The API returned an empty login response.");

        await StoreTokenAsync(auth);
        return auth;
    }

    public async Task<AuthResponse> RegisterAsync(string email, string displayName, string password, UserRole role)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, DisplayName = displayName, Password = password, Role = role });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("The API returned an empty registration response.");

        await StoreTokenAsync(auth);
        return auth;
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Token still present on this call by design; the handler attaches it.
            await _httpClient.PostAsync("/api/auth/logout", null);
        }
        catch
        {
            // Best effort: the local token is cleared regardless.
        }

        await _localStorage.RemoveItemAsync(TokenStorageKey);
        await _stateProvider.NotifyLoggedOutAsync();
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenStorageKey);
    }

    private async Task StoreTokenAsync(AuthResponse auth)
    {
        await _localStorage.SetItemAsync(TokenStorageKey, auth.Token);
        await _stateProvider.NotifyLoggedInAsync(auth.Token);
    }
}
