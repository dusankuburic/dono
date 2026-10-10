namespace BuildingManager.Api.Authorization;

/// <summary>Policy names used in [Authorize] attributes.</summary>
public static class AuthPolicies
{
    public const string ManageOnly = "ManageOnly";
    public const string BuildingAccess = "BuildingAccess";
}
