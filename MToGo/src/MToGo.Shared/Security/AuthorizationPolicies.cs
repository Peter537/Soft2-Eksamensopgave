namespace MToGo.Shared.Security;

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
