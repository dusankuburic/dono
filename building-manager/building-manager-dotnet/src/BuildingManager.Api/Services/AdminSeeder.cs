using BuildingManager.Orleans.Interfaces;
using Orleans;

namespace BuildingManager.Api.Services;

/// <summary>
/// Seeds an admin account at startup from configuration (Auth:AdminEmail /
/// Auth:AdminPassword, optional Auth:AdminDisplayName). Skips silently when
/// not configured or when the account already exists; retries briefly while
/// the Orleans client is still connecting.
/// </summary>
public class AdminSeeder : IHostedService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(IServiceProvider services, IConfiguration configuration, ILogger<AdminSeeder> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Do not block startup; the silo may still be connecting.
        _ = Task.Run(() => SeedAsync(cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var email = _configuration["Auth:AdminEmail"];
        var password = _configuration["Auth:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return; // admin seeding not configured
        }

        email = email.Trim().ToLowerInvariant();
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var grainFactory = _services.GetRequiredService<IGrainFactory>();
                await grainFactory.GetGrain<IUserGrain>(email).RegisterAsync(new RegisterUserRequest
                {
                    Email = email,
                    DisplayName = _configuration["Auth:AdminDisplayName"] ?? "Administrator",
                    Role = UserRole.Admin,
                    Password = password
                });

                _logger.LogInformation("Seeded admin account {AdminEmail}", email);
                return;
            }
            catch (InvalidOperationException)
            {
                // Already registered. If someone self-registered this email
                // first, it is not an admin; surface that instead of silently
                // leaving the deployment without an admin.
                await WarnIfNotAdminAsync(email);
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                _logger.LogWarning(ex, "Admin seeding attempt {Attempt} failed; retrying", attempt);
                await Task.Delay(RetryDelay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin seeding failed after {Attempts} attempts", MaxAttempts);
            }
        }
    }

    private async Task WarnIfNotAdminAsync(string email)
    {
        try
        {
            var profile = await _services.GetRequiredService<IGrainFactory>()
                .GetGrain<IUserGrain>(email).GetProfileAsync();
            if (profile.Role is not UserRole.Admin)
            {
                _logger.LogWarning(
                    "Auth:AdminEmail {AdminEmail} is already registered with role {Role}, not Admin; no admin was seeded",
                    email, profile.Role);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check the existing account for {AdminEmail}", email);
        }
    }
}
