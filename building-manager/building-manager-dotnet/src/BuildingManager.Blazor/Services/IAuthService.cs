using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

public interface IAuthService
{
        Task<AuthResponse> LoginAsync(string email, string password);

        Task<AuthResponse> RegisterAsync(string email, string displayName, string password, UserRole role);

        Task<bool> TryRefreshAsync(string? rejectedToken = null);

        Task LogoutAsync();

    Task<string?> GetTokenAsync();

        Task ForgotPasswordAsync(string email);

        Task ResetPasswordAsync(string email, string resetToken, string newPassword);

        Task ChangePasswordAsync(string currentPassword, string newPassword);
}

public class AuthService : IAuthService
{
    public const string TokenStorageKey = "auth-token";
    public const string RefreshTokenStorageKey = "auth-refresh-token";

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private readonly HttpClient _httpClient;
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
    private readonly JwtAuthenticationStateProvider _stateProvider;

    public AuthService(
        [FromKeyedServices("AuthHttpClient")] HttpClient httpClient,
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

        await StoreTokensAsync(auth);
        return auth;
    }

    public async Task<AuthResponse> RegisterAsync(string email, string displayName, string password, UserRole role)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, DisplayName = displayName, Password = password, Role = role });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>()
            ?? throw new InvalidOperationException("The API returned an empty registration response.");

        await StoreTokensAsync(auth);
        return auth;
    }

    public async Task<bool> TryRefreshAsync(string? rejectedToken = null)
    {
        await _refreshLock.WaitAsync();
        try
        {

            if (rejectedToken is not null)
            {
                var current = await _localStorage.GetItemAsync<string>(TokenStorageKey);
                if (!string.IsNullOrWhiteSpace(current) && current != rejectedToken)
                {
                    return true;
                }
            }

            var refreshToken = await _localStorage.GetItemAsync<string>(RefreshTokenStorageKey);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh",
                new { RefreshToken = refreshToken });

            if (!response.IsSuccessStatusCode)
            {

                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                    or System.Net.HttpStatusCode.BadRequest)
                {

                    var latest = await _localStorage.GetItemAsync<string>(RefreshTokenStorageKey);
                    if (!string.IsNullOrWhiteSpace(latest) && latest != refreshToken)
                    {
                        return true;
                    }

                    await ClearTokensAsync();
                }
                return false;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth is null)
            {
                await ClearTokensAsync();
                return false;
            }

            await StoreTokensAsync(auth);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task LogoutAsync()
    {

        try
        {
            var token = await _localStorage.GetItemAsync<string>(TokenStorageKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                await _httpClient.SendAsync(request);
            }
        }
        catch
        {

        }

        await ClearTokensAsync();
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(TokenStorageKey);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });
        await ApiErrors.EnsureSuccessAsync(response, "Could not request a password reset.");
    }

    public async Task ResetPasswordAsync(string email, string resetToken, string newPassword)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/auth/reset-password",
            new { Email = email, ResetToken = resetToken, NewPassword = newPassword });
        await ApiErrors.EnsureSuccessAsync(response, "Could not reset the password.");
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var body = new { CurrentPassword = currentPassword, NewPassword = newPassword };

        var response = await SendWithTokenAsync(body);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await TryRefreshAsync())
        {
            response = await SendWithTokenAsync(body);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Your session has expired. Please sign in again.");
        }

        await ApiErrors.EnsureSuccessAsync(response, "Could not change the password.");
        await ClearTokensAsync();

        async Task<HttpResponseMessage> SendWithTokenAsync(object content)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/change-password")
            {
                Content = JsonContent.Create(content)
            };
            var token = await _localStorage.GetItemAsync<string>(TokenStorageKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            return await _httpClient.SendAsync(request);
        }
    }

    private async Task StoreTokensAsync(AuthResponse auth)
    {
        await _localStorage.SetItemAsync(TokenStorageKey, auth.Token);
        await _localStorage.SetItemAsync(RefreshTokenStorageKey, auth.RefreshToken);
        await _stateProvider.NotifyLoggedInAsync(auth.Token);
    }

    private async Task ClearTokensAsync()
    {
        await _localStorage.RemoveItemAsync(TokenStorageKey);
        await _localStorage.RemoveItemAsync(RefreshTokenStorageKey);
        await _stateProvider.NotifyLoggedOutAsync();
    }
}
