using System.Security.Claims;

namespace BuildingManager.Blazor.Services;

public static class UserRoles
{
        private static readonly string[] ManageRoles = { "Manager", "SubstituteManager", "Admin", "CompanyAdmin" };

    public static bool CanManage(ClaimsPrincipal user) => ManageRoles.Any(user.IsInRole);
}
