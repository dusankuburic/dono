using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

public interface IAuthService
{
    /// <summary>Logs in and stores the token pair. Throws HttpRequestException on bad credentials.</summary>
    Task<AuthResponse> LoginAsync(string email, string password);

    /// <summary>Registers, auto-logs-in and stores the token pair.</summary>
    Task<AuthResponse> RegisterAsync(string email, string displayName, string password, UserRole role);

    /// <summary>
    /// Silently refreshes the access token with the stored refresh token.
    /// Returns false (and clears the session) when the refresh token is
    /// invalid — the caller should redirect to the login page.
    /// Pass the access token that was rejected: if another request already
    /// replaced it, this returns true without refreshing again.
    /// </summary>
    Task<bool> TryRefreshAsync(string? rejectedToken = null);

    /// <summary>Revokes the session server-side (best effort) and clears the local tokens.</summary>
    Task LogoutAsync();

    Task<string?> GetTokenAsync();

    /// <summary>Requests a reset token by email. Succeeds whether or not the account exists.</summary>
    Task ForgotPasswordAsync(string email);

    /// <summary>Sets a new password with a reset token. Throws InvalidOperationException with the reason on failure.</summary>
    Task ResetPasswordAsync(string email, string resetToken, string newPassword);

    /// <summary>
    /// Changes the password. The API revokes every session, including this
    /// one, so the local tokens are cleared — redirect to login afterwards.
    /// Throws InvalidOperationException with the reason on failure.
    /// </summary>
    Task ChangePasswordAsync(string currentPassword, string newPassword);
}

public class AuthService : IAuthService
{
    public const string TokenStorageKey = "auth-token";
    public const string RefreshTokenStorageKey = "auth-refresh-token";

    // Serializes concurrent refresh attempts (e.g. several requests 401 at
    // once) so the single-use refresh token is rotated only once.
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
            // Several requests can 401 at once; the first one through the lock
            // refreshes, the rest see the new token and just retry. Refreshing
            // again would burn the auth rate limit for nothing.
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
                // Only a rejected token ends the session; a 429 or 5xx is
                // transient and the refresh token may still be good.
                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                    or System.Net.HttpStatusCode.BadRequest)
                {
                    // Tabs share storage but not this lock: another tab may have
                    // rotated the single-use token first. Its new tokens are
                    // already stored, so use them instead of logging out.
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
        // Uses the API HttpClient via the state provider is not possible here
        // (circular DI); the plain client plus the still-stored token would
        // work, but logout revocation is best-effort anyway: call it through
        // a throwaway request on the auth client with the header attached.
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
            // Best effort: the local token is cleared regardless.
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

        // The auth client has no auto-refresh, so retry once by hand if the
        // access token expired while the user was on the page.
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
