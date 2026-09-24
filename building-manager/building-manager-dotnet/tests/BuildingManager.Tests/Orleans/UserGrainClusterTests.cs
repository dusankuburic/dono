using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

/// <summary>
/// Authentication grains: user accounts (registration, credential checks) and
/// sessions (validation, termination, building access) running in the test cluster.
/// </summary>
public class UserGrainClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public UserGrainClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private IUserGrain GetUserGrain() =>
        GrainFactory.GetGrain<IUserGrain>($"user-{Guid.NewGuid():N}@example.com");

    private static RegisterUserRequest RegisterRequest(string password = "S3curePass!") => new()
    {
        Email = "ignored-by-grain-key",
        DisplayName = "Petar Petrovic",
        Role = UserRole.Manager,
        Password = password
    };

    [Fact]
    public async Task Register_Then_Validate_Password()
    {
        var grain = GetUserGrain();
        await grain.RegisterAsync(RegisterRequest());

        (await grain.ValidatePasswordAsync("S3curePass!")).Should().BeTrue();
        (await grain.ValidatePasswordAsync("wrong")).Should().BeFalse();
    }

    [Fact]
    public async Task Duplicate_Registration_Is_Rejected()
    {
        var grain = GetUserGrain();
        await grain.RegisterAsync(RegisterRequest());

        var act = () => grain.RegisterAsync(RegisterRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Unknown_Account_Validation_Returns_False_Like_A_Wrong_Password()
    {
        var grain = GetUserGrain();

        (await grain.ValidatePasswordAsync("S3curePass!")).Should().BeFalse();
    }

    [Fact]
    public async Task Profile_Of_Unregistered_Account_Throws()
    {
        var grain = GetUserGrain();

        var act = () => grain.GetProfileAsync();

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Registered_Profile_Has_No_Password_Material()
    {
        var grain = GetUserGrain();
        await grain.RegisterAsync(RegisterRequest());

        var profile = await grain.GetProfileAsync();

        profile.UserId.Should().NotBe(Guid.Empty);
        profile.DisplayName.Should().Be("Petar Petrovic");
        profile.Role.Should().Be(UserRole.Manager);
        profile.LastLoginAt.Should().BeNull("only successful logins update it");

        // A successful validation records the login.
        await grain.ValidatePasswordAsync("S3curePass!");
        (await grain.GetProfileAsync()).LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Registration_Validates_Password_Length()
    {
        var grain = GetUserGrain();

        var act = () => grain.RegisterAsync(RegisterRequest("short"));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ChangePassword_Requires_Correct_Current_Password()
    {
        var grain = GetUserGrain();
        await grain.RegisterAsync(RegisterRequest());

        var wrongCurrent = () => grain.ChangePasswordAsync("nope", "NewS3curePass!");
        await wrongCurrent.Should().ThrowAsync<InvalidOperationException>();

        await grain.ChangePasswordAsync("S3curePass!", "NewS3curePass!");
        (await grain.ValidatePasswordAsync("NewS3curePass!")).Should().BeTrue();
        (await grain.ValidatePasswordAsync("S3curePass!")).Should().BeFalse();
    }
}

public class UserSessionGrainClusterTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public UserSessionGrainClusterTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private static UserSession Session(DateTime? expiresAt = null) => new()
    {
        UserId = Guid.NewGuid().ToString(),
        Email = "user@example.com",
        Role = UserRole.Owner,
        AccessibleBuildingIds = new List<string>(),
        CreatedAt = DateTime.UtcNow,
        LastActivityAt = DateTime.UtcNow,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1)
    };

    [Fact]
    public async Task Started_Session_Is_Valid()
    {
        var grain = GrainFactory.GetGrain<IUserSessionGrain>(Guid.NewGuid().ToString("N"));
        await grain.StartSessionAsync(Session());

        (await grain.ValidateSessionAsync()).Should().BeTrue();
        (await grain.GetSessionAsync()).Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Unstarted_Session_Is_Invalid()
    {
        var grain = GrainFactory.GetGrain<IUserSessionGrain>(Guid.NewGuid().ToString("N"));

        (await grain.ValidateSessionAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Terminated_Session_Is_Invalid()
    {
        var grain = GrainFactory.GetGrain<IUserSessionGrain>(Guid.NewGuid().ToString("N"));
        await grain.StartSessionAsync(Session());

        await grain.TerminateAsync();

        (await grain.ValidateSessionAsync()).Should().BeFalse("logout must revoke the token");
    }

    [Fact]
    public async Task Expired_Session_Is_Invalid()
    {
        var grain = GrainFactory.GetGrain<IUserSessionGrain>(Guid.NewGuid().ToString("N"));
        await grain.StartSessionAsync(Session(expiresAt: DateTime.UtcNow.AddMinutes(-1)));

        (await grain.ValidateSessionAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Building_Access_Tracks_Add_And_Remove()
    {
        var grain = GrainFactory.GetGrain<IUserSessionGrain>(Guid.NewGuid().ToString("N"));
        await grain.StartSessionAsync(Session());

        await grain.AddBuildingAccessAsync("bldg-a");
        await grain.AddBuildingAccessAsync("bldg-b");
        await grain.AddBuildingAccessAsync("bldg-a"); // duplicate is a no-op

        var access = (await grain.GetAccessibleBuildingsAsync()).ToList();
        access.Should().BeEquivalentTo(new[] { "bldg-a", "bldg-b" });

        await grain.RemoveBuildingAccessAsync("bldg-a");
        (await grain.GetAccessibleBuildingsAsync()).Should().BeEquivalentTo(new[] { "bldg-b" });
    }
}
