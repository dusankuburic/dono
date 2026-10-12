namespace BuildingManager.Api.Authorization;

public static class AuthPolicies
{
    public const string ManageOnly = "ManageOnly";
    public const string BuildingAccess = "BuildingAccess";

        public static readonly string[] ManageRoles = { "Manager", "SubstituteManager", "Admin", "CompanyAdmin" };

        public static bool IsManager(System.Security.Claims.ClaimsPrincipal user) => ManageRoles.Any(user.IsInRole);
}
