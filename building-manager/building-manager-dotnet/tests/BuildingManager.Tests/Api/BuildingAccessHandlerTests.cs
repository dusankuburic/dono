using BuildingManager.Api.Authorization;
using BuildingManager.Orleans.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Orleans;
using System.Security.Claims;
using Xunit;

namespace BuildingManager.Tests.Api;

/// <summary>
/// BuildingAccessHandler decides, per request, whether the caller may operate
/// on the building in the route. These tests run it against the real user
/// grains in the test cluster.
/// </summary>
public class BuildingAccessHandlerTests : IClassFixture<Orleans.ClusterFixture>
{
    private readonly Orleans.ClusterFixture _fixture;

    public BuildingAccessHandlerTests(Orleans.ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    private IGrainFactory GrainFactory => _fixture.Cluster.GrainFactory;

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "test");
        return new ClaimsPrincipal(identity);
    }

    private static HttpContext HttpContextWithBuilding(string? buildingId)
    {
        var httpContext = new DefaultHttpContext();
        if (buildingId is not null)
        {
            httpContext.Request.RouteValues["buildingId"] = buildingId;
        }

        return httpContext;
    }

    private async Task<bool> Evaluate(ClaimsPrincipal user, HttpContext resource)
    {
        var handler = new BuildingAccessHandler(GrainFactory);
        var requirement = new BuildingAccessRequirement();
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    private async Task<IUserGrain> RegisteredUser(string email)
    {
        var grain = GrainFactory.GetGrain<IUserGrain>(email);
        await grain.RegisterAsync(new RegisterUserRequest
        {
            Email = email,
            DisplayName = "Test User",
            Role = UserRole.Owner,
            Password = "S3curePass!"
        });
        return grain;
    }

    [Fact]
    public async Task User_With_Granted_Access_Passes()
    {
        var email = $"access-{Guid.NewGuid():N}@example.com";
        var user = await RegisteredUser(email);
        await user.GrantBuildingAccessAsync("bldg-1");

        var result = await Evaluate(
            Principal(("email", email), ("role", "Owner")),
            HttpContextWithBuilding("bldg-1"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task User_Without_Access_Is_Denied()
    {
        var email = $"no-access-{Guid.NewGuid():N}@example.com";
        await RegisteredUser(email);

        var result = await Evaluate(
            Principal(("email", email), ("role", "Owner")),
            HttpContextWithBuilding("bldg-42"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_Bypasses_Access_Check()
    {
        var email = $"admin-{Guid.NewGuid():N}@example.com";

        var result = await Evaluate(
            Principal(("email", email), ("role", "Admin")),
            HttpContextWithBuilding("any-building"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CompanyAdmin_Bypasses_Access_Check()
    {
        var result = await Evaluate(
            Principal(("email", "boss@example.com"), ("role", "CompanyAdmin")),
            HttpContextWithBuilding("any-building"));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Revoked_Access_Is_Denied()
    {
        var email = $"revoke-{Guid.NewGuid():N}@example.com";
        var user = await RegisteredUser(email);
        await user.GrantBuildingAccessAsync("bldg-1");
        await user.RevokeBuildingAccessAsync("bldg-1");

        var result = await Evaluate(
            Principal(("email", email), ("role", "Manager")),
            HttpContextWithBuilding("bldg-1"));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Requests_Without_BuildingId_In_Route_Pass()
    {
        var email = $"list-{Guid.NewGuid():N}@example.com";
        await RegisteredUser(email);

        var result = await Evaluate(
            Principal(("email", email), ("role", "Owner")),
            HttpContextWithBuilding(buildingId: null));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Principal_Without_Email_Claim_Is_Denied()
    {
        var result = await Evaluate(
            Principal(("role", "Owner")),
            HttpContextWithBuilding("bldg-1"));

        result.Should().BeFalse();
    }
}
