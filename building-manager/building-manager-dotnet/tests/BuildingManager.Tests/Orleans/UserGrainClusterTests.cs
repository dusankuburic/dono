using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Orleans;
using Xunit;

namespace BuildingManager.Tests.Orleans;

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

    [Fact]
    public async Task Building_Access_Grant_And_Revoke_Roundtrip()
    {
        var grain = GetUserGrain();
        await grain.RegisterAsync(RegisterRequest());

        (await grain.HasBuildingAccessAsync("bldg-1")).Should().BeFalse();

        await grain.GrantBuildingAccessAsync("bldg-1");
        await grain.GrantBuildingAccessAsync("bldg-2");
        await grain.GrantBuildingAccessAsync("bldg-1");

        (await grain.HasBuildingAccessAsync("bldg-1")).Should().BeTrue();
        (await grain.GetAccessibleBuildingIdsAsync()).Should().BeEquivalentTo(new[] { "bldg-1", "bldg-2" });

        await grain.RevokeBuildingAccessAsync("bldg-1");

        (await grain.HasBuildingAccessAsync("bldg-1")).Should().BeFalse();
        (await grain.GetAccessibleBuildingIdsAsync()).Should().BeEquivalentTo(new[] { "bldg-2" });
    }

    [Fact]
    public async Task Grant_Access_To_Unregistered_Account_Throws()
    {
        var grain = GetUserGrain();

        var act = () => grain.GrantBuildingAccessAsync("bldg-1");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Unregistered_Account_Has_No_Access()
    {
        var grain = GetUserGrain();

        (await grain.HasBuildingAccessAsync("bldg-1")).Should().BeFalse();
        (await grain.GetAccessibleBuildingIdsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Grant_And_Revoke_Keep_The_Building_Member_Index_In_Sync()
    {

        var buildingId = $"bldg-{Guid.NewGuid():N}";
        var alice = GetUserGrain();
        var bob = GetUserGrain();
        await alice.RegisterAsync(RegisterRequest());
        await bob.RegisterAsync(RegisterRequest());
        var index = GrainFactory.GetGrain<IBuildingAccessGrain>(buildingId);

        await alice.GrantBuildingAccessAsync(buildingId);
        await bob.GrantBuildingAccessAsync(buildingId);
        await alice.GrantBuildingAccessAsync(buildingId);

        (await index.GetMemberEmailsAsync()).Should().BeEquivalentTo(
            alice.GetPrimaryKeyString(), bob.GetPrimaryKeyString());

        await alice.RevokeBuildingAccessAsync(buildingId);

        (await index.GetMemberEmailsAsync()).Should().BeEquivalentTo(bob.GetPrimaryKeyString());
    }

    [Fact]
    public async Task Failed_Grant_To_Unregistered_Account_Does_Not_Touch_The_Index()
    {
        var buildingId = $"bldg-{Guid.NewGuid():N}";

        var act = () => GetUserGrain().GrantBuildingAccessAsync(buildingId);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await GrainFactory.GetGrain<IBuildingAccessGrain>(buildingId).GetMemberEmailsAsync()).Should().BeEmpty();
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
        await grain.AddBuildingAccessAsync("bldg-a");

        var access = (await grain.GetAccessibleBuildingsAsync()).ToList();
        access.Should().BeEquivalentTo(new[] { "bldg-a", "bldg-b" });

        await grain.RemoveBuildingAccessAsync("bldg-a");
        (await grain.GetAccessibleBuildingsAsync()).Should().BeEquivalentTo(new[] { "bldg-b" });
    }
}
