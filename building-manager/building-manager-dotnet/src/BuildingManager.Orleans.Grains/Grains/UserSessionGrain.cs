using BuildingManager.Orleans.Interfaces;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

/// <summary>
/// Per-token session grain (key = JWT jti). Terminated or expired sessions
/// fail validation, which is how logout revokes tokens before their expiry.
/// </summary>
public class UserSessionGrain : Grain, IUserSessionGrain
{
    private readonly IPersistentState<UserSessionState> _state;

    public UserSessionGrain(
        [PersistentState("session", "buildingStorage")]
        IPersistentState<UserSessionState> state)
    {
        _state = state;
    }

    public async Task StartSessionAsync(UserSession session)
    {
        _state.State.Session = session;
        _state.State.IsTerminated = false;
        await _state.WriteStateAsync();
    }

    public Task<bool> ValidateSessionAsync()
    {
        var session = _state.State.Session;
        var valid = session is not null
            && !_state.State.IsTerminated
            && session.ExpiresAt > DateTime.UtcNow;
        return Task.FromResult(valid);
    }

    public Task<UserSession> GetSessionAsync()
    {
        if (_state.State.Session is null)
            throw new KeyNotFoundException("Session has not been started.");

        return Task.FromResult(_state.State.Session);
    }

    public async Task UpdateLastActivityAsync()
    {
        if (_state.State.Session is null)
            throw new KeyNotFoundException("Session has not been started.");

        _state.State.Session = _state.State.Session with { LastActivityAt = DateTime.UtcNow };
        await _state.WriteStateAsync();
    }

    public async Task AddBuildingAccessAsync(string buildingId)
    {
        if (_state.State.Session is null)
            throw new KeyNotFoundException("Session has not been started.");

        var access = _state.State.Session.AccessibleBuildingIds.ToList();
        if (!access.Contains(buildingId))
        {
            access.Add(buildingId);
            _state.State.Session = _state.State.Session with { AccessibleBuildingIds = access };
            await _state.WriteStateAsync();
        }
    }

    public async Task RemoveBuildingAccessAsync(string buildingId)
    {
        if (_state.State.Session is null)
            throw new KeyNotFoundException("Session has not been started.");

        if (_state.State.Session.AccessibleBuildingIds.Contains(buildingId))
        {
            var access = _state.State.Session.AccessibleBuildingIds
                .Where(id => id != buildingId)
                .ToList();
            _state.State.Session = _state.State.Session with { AccessibleBuildingIds = access };
            await _state.WriteStateAsync();
        }
    }

    public Task<IEnumerable<string>> GetAccessibleBuildingsAsync()
    {
        return Task.FromResult(_state.State.Session?.AccessibleBuildingIds.AsEnumerable()
            ?? Enumerable.Empty<string>());
    }

    public async Task TerminateAsync()
    {
        if (_state.State.Session is null || _state.State.IsTerminated)
        {
            return;
        }

        _state.State.IsTerminated = true;
        await _state.WriteStateAsync();
    }
}

[GenerateSerializer]
public class UserSessionState
{
    [Id(0)] public UserSession? Session { get; set; }
    [Id(1)] public bool IsTerminated { get; set; }
}
