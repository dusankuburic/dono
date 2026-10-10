using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Orleans;

namespace BuildingManager.Api.Authorization;

/// <summary>
/// Requirement satisfied when the caller is allowed to operate on the
/// building in the route: admins always pass, everyone else needs explicit
/// access granted through their user grain.
/// </summary>
public class BuildingAccessRequirement : IAuthorizationRequirement
{
}

public class BuildingAccessHandler : AuthorizationHandler<BuildingAccessRequirement, HttpContext>
{
    private const string AdminRole = "Admin";
    private const string CompanyAdminRole = "CompanyAdmin";

    private readonly IGrainFactory _grainFactory;

    public BuildingAccessHandler(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BuildingAccessRequirement requirement,
        HttpContext httpContext)
    {
        var role = context.User.FindFirst("role")?.Value;
        if (role is AdminRole or CompanyAdminRole)
        {
            context.Succeed(requirement);
            return;
        }

        // Endpoints without a building id in the route (e.g. the building
        // list) have nothing to check here; they filter by access themselves.
        var buildingId = httpContext.Request.RouteValues["buildingId"] as string;
        if (string.IsNullOrEmpty(buildingId))
        {
            context.Succeed(requirement);
            return;
        }

        var email = context.User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return;
        }

        var userGrain = _grainFactory.GetGrain<IUserGrain>(email);
        if (await userGrain.HasBuildingAccessAsync(buildingId))
        {
            context.Succeed(requirement);
        }
    }
}
