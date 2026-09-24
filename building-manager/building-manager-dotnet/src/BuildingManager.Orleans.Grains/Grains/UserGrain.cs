using BuildingManager.Core.Security;
using BuildingManager.Orleans.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

/// <summary>
/// Per-user account grain keyed by normalized email. Password hashes are
/// stored; plaintext passwords never touch persisted state.
/// </summary>
public class UserGrain : Grain, IUserGrain
{
    private const int MinimumPasswordLength = 8;

    private readonly IPersistentState<UserAccountState> _state;

    public UserGrain(
        [PersistentState("user", "buildingStorage")]
        IPersistentState<UserAccountState> state)
    {
        _state = state;
    }

    public async Task RegisterAsync(RegisterUserRequest request)
    {
        if (!string.IsNullOrEmpty(_state.State.PasswordHash))
            throw new InvalidOperationException($"An account for '{this.GetPrimaryKeyString()}' already exists.");

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw new ArgumentException("Display name is required", nameof(request));

        if (request.Password.Length < MinimumPasswordLength)
            throw new ArgumentException($"Password must be at least {MinimumPasswordLength} characters", nameof(request));

        _state.State.UserId = Guid.NewGuid();
        _state.State.Email = this.GetPrimaryKeyString();
        _state.State.DisplayName = request.DisplayName;
        _state.State.Role = request.Role;
        _state.State.PasswordHash = PasswordHasher.Hash(request.Password);
        _state.State.CreatedAt = DateTime.UtcNow;

        await _state.WriteStateAsync();
    }

    public async Task<bool> ValidatePasswordAsync(string password)
    {
        // Unknown account and wrong password are indistinguishable to callers.
        if (!PasswordHasher.Verify(password, _state.State.PasswordHash))
        {
            return false;
        }

        _state.State.LastLoginAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
        return true;
    }

    public Task<UserAccount> GetProfileAsync()
    {
        if (string.IsNullOrEmpty(_state.State.PasswordHash))
            throw new KeyNotFoundException($"No account for '{this.GetPrimaryKeyString()}'.");

        return Task.FromResult(new UserAccount
        {
            UserId = _state.State.UserId,
            Email = _state.State.Email,
            DisplayName = _state.State.DisplayName,
            Role = _state.State.Role,
            CreatedAt = _state.State.CreatedAt,
            LastLoginAt = _state.State.LastLoginAt
        });
    }

    public async Task ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(_state.State.PasswordHash))
            throw new KeyNotFoundException($"No account for '{this.GetPrimaryKeyString()}'.");

        if (!PasswordHasher.Verify(currentPassword, _state.State.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        if (newPassword.Length < MinimumPasswordLength)
            throw new ArgumentException($"Password must be at least {MinimumPasswordLength} characters", nameof(newPassword));

        _state.State.PasswordHash = PasswordHasher.Hash(newPassword);
        await _state.WriteStateAsync();
    }
}

[GenerateSerializer]
public class UserAccountState
{
    [Id(0)] public Guid UserId { get; set; }
    [Id(1)] public string Email { get; set; } = string.Empty;
    [Id(2)] public string DisplayName { get; set; } = string.Empty;
    [Id(3)] public UserRole Role { get; set; } = UserRole.Owner;
    [Id(4)] public string PasswordHash { get; set; } = string.Empty;
    [Id(5)] public DateTime CreatedAt { get; set; }
    [Id(6)] public DateTime? LastLoginAt { get; set; }
}
