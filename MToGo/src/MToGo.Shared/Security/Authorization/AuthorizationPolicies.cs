namespace MToGo.Shared.Security.Authorization;

/// <summary>
/// Policy names for authorization throughout the application.
/// Use these with [Authorize(Policy = AuthorizationPolicies.XXX)]
/// </summary>
public static class AuthorizationPolicies
{
    public const string CustomerOnly = "CustomerOnly";
    public const string PartnerOnly = "PartnerOnly";
    public const string AgentOnly = "AgentOnly";
    public const string ManagementOnly = "ManagementOnly";
    public const string CustomerOrManagement = "CustomerOrManagement";
    public const string PartnerOrManagement = "PartnerOrManagement";
    public const string AgentOrManagement = "AgentOrManagement";
    public const string AllAuthenticated = "AllAuthenticated";
}
