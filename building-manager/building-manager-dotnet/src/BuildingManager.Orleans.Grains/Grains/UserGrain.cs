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
    private const int MaxTrackedSessions = 20;

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

    public async Task GrantBuildingAccessAsync(string buildingId)
    {
        if (string.IsNullOrEmpty(_state.State.PasswordHash))
            throw new KeyNotFoundException($"No account for '{this.GetPrimaryKeyString()}'.");

        if (string.IsNullOrWhiteSpace(buildingId))
            throw new ArgumentException("Building id is required", nameof(buildingId));

        if (!_state.State.AccessibleBuildingIds.Contains(buildingId))
        {
            _state.State.AccessibleBuildingIds.Add(buildingId);
            await _state.WriteStateAsync();
        }

        // Always (idempotently) update the index, so retrying a grant whose
        // index write failed repairs it.
        await GrainFactory.GetGrain<IBuildingAccessGrain>(buildingId)
            .AddMemberAsync(this.GetPrimaryKeyString());
    }

    public async Task RevokeBuildingAccessAsync(string buildingId)
    {
        if (_state.State.AccessibleBuildingIds.Remove(buildingId))
        {
            await _state.WriteStateAsync();
        }

        await GrainFactory.GetGrain<IBuildingAccessGrain>(buildingId)
            .RemoveMemberAsync(this.GetPrimaryKeyString());
    }

    public Task<bool> HasBuildingAccessAsync(string buildingId)
    {
        return Task.FromResult(_state.State.AccessibleBuildingIds.Contains(buildingId));
    }

    public Task<List<string>> GetAccessibleBuildingIdsAsync()
    {
        return Task.FromResult(_state.State.AccessibleBuildingIds.ToList());
    }

    public async Task<string?> CreatePasswordResetTokenAsync()
    {
        if (string.IsNullOrEmpty(_state.State.PasswordHash))
            return null;

        var token = TokenHasher.Generate();
        _state.State.PasswordResetTokenHash = TokenHasher.Hash(token);
        _state.State.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);
        await _state.WriteStateAsync();
        return token;
    }

    public async Task<bool> ResetPasswordAsync(string resetToken, string newPassword)
    {
        if (string.IsNullOrEmpty(_state.State.PasswordHash)
            || string.IsNullOrEmpty(_state.State.PasswordResetTokenHash)
            || _state.State.PasswordResetExpiresAt is not { } expiresAt
            || expiresAt <= DateTime.UtcNow
            || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(_state.State.PasswordResetTokenHash),
                System.Text.Encoding.UTF8.GetBytes(TokenHasher.Hash(resetToken))))
        {
            return false;
        }

        if (newPassword.Length < MinimumPasswordLength)
            throw new ArgumentException($"Password must be at least {MinimumPasswordLength} characters", nameof(newPassword));

        _state.State.PasswordHash = PasswordHasher.Hash(newPassword);
        _state.State.PasswordResetTokenHash = null;
        _state.State.PasswordResetExpiresAt = null;
        await _state.WriteStateAsync();

        // A reset password must invalidate every device logged in before it.
        await TerminateAllSessionsAsync();
        return true;
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
        // A reset code requested before the change must not be able to
        // override the password the user just chose.
        _state.State.PasswordResetTokenHash = null;
        _state.State.PasswordResetExpiresAt = null;
        await _state.WriteStateAsync();

        // Includes the session that requested the change: clients redirect to
        // the login page afterwards.
        await TerminateAllSessionsAsync();
    }

    public async Task RegisterSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session id is required", nameof(sessionId));

        if (_state.State.ActiveSessionIds.Contains(sessionId))
            return;

        // Cap the list so it cannot bloat the state. Evicted sessions must be
        // terminated, not just forgotten: an untracked session would survive
        // a password reset, and refreshing keeps a session alive indefinitely.
        if (_state.State.ActiveSessionIds.Count >= MaxTrackedSessions)
        {
            var evictCount = _state.State.ActiveSessionIds.Count - MaxTrackedSessions + 1;
            foreach (var evicted in _state.State.ActiveSessionIds.Take(evictCount).ToList())
            {
                await GrainFactory.GetGrain<IUserSessionGrain>(evicted).TerminateAsync();
            }
            _state.State.ActiveSessionIds.RemoveRange(0, evictCount);
        }

        _state.State.ActiveSessionIds.Add(sessionId);
        await _state.WriteStateAsync();
    }

    public async Task UnregisterSessionAsync(string sessionId)
    {
        if (_state.State.ActiveSessionIds.Remove(sessionId))
        {
            await _state.WriteStateAsync();
        }
    }

    public async Task TerminateAllSessionsAsync()
    {
        foreach (var sessionId in _state.State.ActiveSessionIds)
        {
            await GrainFactory.GetGrain<IUserSessionGrain>(sessionId).TerminateAsync();
        }

        if (_state.State.ActiveSessionIds.Count > 0)
        {
            _state.State.ActiveSessionIds.Clear();
            await _state.WriteStateAsync();
        }
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
    [Id(7)] public List<string> AccessibleBuildingIds { get; set; } = new();
    [Id(8)] public string? PasswordResetTokenHash { get; set; }
    [Id(9)] public DateTime? PasswordResetExpiresAt { get; set; }
    [Id(10)] public List<string> ActiveSessionIds { get; set; } = new();
}
