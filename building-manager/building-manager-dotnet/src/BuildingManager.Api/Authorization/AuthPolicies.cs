namespace BuildingManager.Api.Authorization;

/// <summary>Policy names used in [Authorize] attributes.</summary>
public static class AuthPolicies
{
    public const string ManageOnly = "ManageOnly";
    public const string BuildingAccess = "BuildingAccess";

    /// <summary>Roles that satisfy ManageOnly; also used for in-action checks.</summary>
    public static readonly string[] ManageRoles = { "Manager", "SubstituteManager", "Admin", "CompanyAdmin" };
}
