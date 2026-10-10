using System.Security.Claims;

namespace BuildingManager.Blazor.Services;

/// <summary>Role checks for showing management UI. The API enforces the same rules regardless.</summary>
public static class UserRoles
{
    /// <summary>Mirrors the API's ManageOnly policy.</summary>
    private static readonly string[] ManageRoles = { "Manager", "SubstituteManager", "Admin", "CompanyAdmin" };

    public static bool CanManage(ClaimsPrincipal user) => ManageRoles.Any(user.IsInRole);
}
