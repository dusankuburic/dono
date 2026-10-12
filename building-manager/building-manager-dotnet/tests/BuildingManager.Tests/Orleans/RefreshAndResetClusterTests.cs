using BuildingManager.Core.Security;
using BuildingManager.Orleans.Grains;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.TestingHost;
using Xunit;

namespace BuildingManager.Tests.Orleans;

public class RefreshAndResetClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public RefreshAndResetClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private async Task<IUserGrain> RegisteredUser(string email, string password = "S3curePass!")
    {
        var grain = GrainFactory.GetGrain<IUserGrain>(email);
        await grain.RegisterAsync(new RegisterUserRequest
        {
            Email = email,
            DisplayName = "Test User",
            Role = UserRole.Owner,
            Password = password
        });
        return grain;
    }

    private async Task<IUserSessionGrain> StartedSession()
    {
        var grain = GrainFactory.GetGrain<IUserSessionGrain>(Guid.NewGuid().ToString("N"));
        await grain.StartSessionAsync(new UserSession
        {
            UserId = Guid.NewGuid().ToString(),
            Email = "user@example.com",
            Role = UserRole.Owner,
            AccessibleBuildingIds = new List<string>(),
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        return grain;
    }

    [Fact]
    public async Task Refresh_Token_Rotates_And_Old_Token_Becomes_Unusable()
    {
        var session = await StartedSession();
        var first = TokenHasher.Hash(TokenHasher.Generate());
        var second = TokenHasher.Hash(TokenHasher.Generate());
        var third = TokenHasher.Hash(TokenHasher.Generate());

        await session.SetRefreshTokenAsync(first);

        (await session.TryRotateRefreshTokenAsync(first, second, DateTime.UtcNow.AddDays(7)))
            .Should().BeTrue();
        (await session.ValidateSessionAsync()).Should().BeTrue();

        (await session.TryRotateRefreshTokenAsync(first, third, DateTime.UtcNow.AddDays(7)))
            .Should().BeFalse();

        (await session.TryRotateRefreshTokenAsync(second, third, DateTime.UtcNow.AddDays(7)))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_Token_Hash_Is_Persisted_Not_Just_Held_In_Memory()
    {

        var session = await StartedSession();
        var hash = TokenHasher.Hash(TokenHasher.Generate());

        await session.SetRefreshTokenAsync(hash);

        var storage = ((InProcessSiloHandle)_fixture.Cluster.Primary).SiloHost.Services
            .GetRequiredKeyedService<IGrainStorage>("buildingStorage");
        var stored = new GrainState<UserSessionState>();
        await storage.ReadStateAsync("session", session.GetGrainId(), stored);

        stored.State.RefreshTokenHash.Should().Be(hash);
    }

    [Fact]
    public async Task Rotation_Wrong_Secret_Fails()
    {
        var session = await StartedSession();
        var stored = TokenHasher.Hash(TokenHasher.Generate());
        var presented = TokenHasher.Hash(TokenHasher.Generate());

        await session.SetRefreshTokenAsync(stored);

        (await session.TryRotateRefreshTokenAsync(presented, TokenHasher.Hash("next"), DateTime.UtcNow.AddDays(7)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Rotation_Extends_Session_Expiry()
    {
        var session = await StartedSession();
        var stored = TokenHasher.Hash(TokenHasher.Generate());
        await session.SetRefreshTokenAsync(stored);

        var newExpiry = DateTime.UtcNow.AddDays(14);
        await session.TryRotateRefreshTokenAsync(stored, TokenHasher.Hash("next"), newExpiry);

        (await session.GetSessionAsync()).ExpiresAt.Should().BeOnOrAfter(newExpiry.AddMinutes(-1));
    }

    [Fact]
    public async Task Rotation_Fails_After_Termination()
    {
        var session = await StartedSession();
        var stored = TokenHasher.Hash(TokenHasher.Generate());
        await session.SetRefreshTokenAsync(stored);

        await session.TerminateAsync();

        (await session.TryRotateRefreshTokenAsync(stored, TokenHasher.Hash("next"), DateTime.UtcNow.AddDays(7)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task Password_Reset_Full_Flow()
    {
        var email = $"reset-{Guid.NewGuid():N}@example.com";
        await RegisteredUser(email, "OldPass123!");

        var token = await GrainFactory.GetGrain<IUserGrain>(email).CreatePasswordResetTokenAsync();
        token.Should().NotBeNullOrEmpty();

        (await GrainFactory.GetGrain<IUserGrain>(email).ResetPasswordAsync("wrong-token", "NewPass456!"))
            .Should().BeFalse();

        (await GrainFactory.GetGrain<IUserGrain>(email).ResetPasswordAsync(token!, "NewPass456!"))
            .Should().BeTrue();

        var user = GrainFactory.GetGrain<IUserGrain>(email);
        (await user.ValidatePasswordAsync("OldPass123!")).Should().BeFalse();
        (await user.ValidatePasswordAsync("NewPass456!")).Should().BeTrue();

        (await user.ResetPasswordAsync(token!, "AgainPass78!")).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_Invalidates_An_Outstanding_Reset_Token()
    {
        var email = $"change-{Guid.NewGuid():N}@example.com";
        var user = await RegisteredUser(email, "OldPass123!");
        var token = await user.CreatePasswordResetTokenAsync();

        await user.ChangePasswordAsync("OldPass123!", "Chosen456!");

        (await user.ResetPasswordAsync(token!, "Attacker789!")).Should().BeFalse();
        (await user.ValidatePasswordAsync("Chosen456!")).Should().BeTrue();
    }

    [Fact]
    public async Task Reset_Token_For_Unknown_Account_Returns_Null()
    {
        var grain = GrainFactory.GetGrain<IUserGrain>($"ghost-{Guid.NewGuid():N}@example.com");

        (await grain.CreatePasswordResetTokenAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Reset_Token_Expires()
    {

        var email = $"expiry-{Guid.NewGuid():N}@example.com";
        await RegisteredUser(email);
        var grain = GrainFactory.GetGrain<IUserGrain>(email);

        var first = await grain.CreatePasswordResetTokenAsync();
        var second = await grain.CreatePasswordResetTokenAsync();

        first.Should().NotBe(second, "each request must generate a fresh token");

        (await grain.ResetPasswordAsync(first!, "NewPass456!")).Should().BeFalse();
        (await grain.ResetPasswordAsync(second!, "NewPass456!")).Should().BeTrue();
    }
}
