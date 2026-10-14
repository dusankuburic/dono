using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

public class SessionRevocationClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public SessionRevocationClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private async Task<IUserGrain> RegisteredUser(string email)
    {
        var grain = GrainFactory.GetGrain<IUserGrain>(email);
        await grain.RegisterAsync(new RegisterUserRequest
        {
            Email = email,
            DisplayName = "Revocation User",
            Role = UserRole.Owner,
            Password = "S3curePass!"
        });
        return grain;
    }

        private async Task<string> LoginSession(IUserGrain user)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        await GrainFactory.GetGrain<IUserSessionGrain>(sessionId).StartSessionAsync(new UserSession
        {
            UserId = Guid.NewGuid().ToString(),
            Email = user.GetPrimaryKeyString(),
            Role = UserRole.Owner,
            AccessibleBuildingIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await user.RegisterSessionAsync(sessionId);
        return sessionId;
    }

    private Task<bool> SessionValid(string sessionId) =>
        GrainFactory.GetGrain<IUserSessionGrain>(sessionId).ValidateSessionAsync();

    [Fact]
    public async Task Password_Reset_Terminates_All_Tracked_Sessions()
    {
        var user = await RegisteredUser($"revoke-{Guid.NewGuid():N}@example.com");
        var sessionA = await LoginSession(user);
        var sessionB = await LoginSession(user);

        (await SessionValid(sessionA)).Should().BeTrue();
        (await SessionValid(sessionB)).Should().BeTrue();

        var token = await user.CreatePasswordResetTokenAsync();
        await user.ResetPasswordAsync(token!, "NewS3curePass!");

        (await SessionValid(sessionA)).Should().BeFalse("reset must revoke all devices");
        (await SessionValid(sessionB)).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_Terminates_All_Tracked_Sessions()
    {
        var user = await RegisteredUser($"change-{Guid.NewGuid():N}@example.com");
        var session = await LoginSession(user);

        (await SessionValid(session)).Should().BeTrue();

        await user.ChangePasswordAsync("S3curePass!", "NewS3curePass!");

        (await SessionValid(session)).Should().BeFalse("password change must revoke the session");
        (await user.ValidatePasswordAsync("NewS3curePass!")).Should().BeTrue();
    }

    [Fact]
    public async Task LoggedOut_Session_Is_Not_Tracked_Anymore()
    {
        var user = await RegisteredUser($"logout-{Guid.NewGuid():N}@example.com");
        var loggedOut = await LoginSession(user);
        var active = await LoginSession(user);

        await GrainFactory.GetGrain<IUserSessionGrain>(loggedOut).TerminateAsync();
        await user.UnregisterSessionAsync(loggedOut);

        await user.TerminateAllSessionsAsync();

        (await SessionValid(loggedOut)).Should().BeFalse();
        (await SessionValid(active)).Should().BeFalse();
    }

    [Fact]
    public async Task Sessions_Beyond_The_Tracking_Cap_Cannot_Survive_A_Reset()
    {

        var user = await RegisteredUser($"cap-{Guid.NewGuid():N}@example.com");
        var oldest = await LoginSession(user);
        var newer = new List<string>();
        for (var i = 0; i < 20; i++)
        {
            newer.Add(await LoginSession(user));
        }

        (await SessionValid(oldest)).Should().BeFalse("the evicted session must be terminated");
        (await SessionValid(newer[0])).Should().BeTrue();

        var token = await user.CreatePasswordResetTokenAsync();
        await user.ResetPasswordAsync(token!, "NewS3curePass!");

        foreach (var session in newer.Append(oldest))
        {
            (await SessionValid(session)).Should().BeFalse("reset must leave no session alive");
        }
    }

    [Fact]
    public async Task TerminateAllSessions_With_No_Sessions_Is_Harmless()
    {
        var user = await RegisteredUser($"empty-{Guid.NewGuid():N}@example.com");

        var act = () => user.TerminateAllSessionsAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Terminated_Session_State_Is_Cleared_So_Nothing_Of_It_Remains()
    {
        var user = await RegisteredUser($"clear-{Guid.NewGuid():N}@example.com");
        var sessionId = await LoginSession(user);
        var session = GrainFactory.GetGrain<IUserSessionGrain>(sessionId);
        await session.SetRefreshTokenAsync("hash");

        await session.TerminateAsync();

        (await session.ValidateSessionAsync()).Should().BeFalse();
        await session.Invoking(s => s.GetSessionAsync()).Should().ThrowAsync<KeyNotFoundException>();
        (await session.TryRotateRefreshTokenAsync("hash", "new", DateTime.UtcNow.AddDays(7))).Should().BeFalse();
        await session.Invoking(s => s.TerminateAsync()).Should().NotThrowAsync();
    }
}
