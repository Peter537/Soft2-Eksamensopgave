namespace MToGo.Shared.Security;

public static class UserRoles
{
    public const string Customer = "Customer";
    public const string Partner = "Partner";
    public const string Agent = "Agent";
    public const string Management = "Management";
    
    public static readonly string[] AllRoles = { Customer, Partner, Agent, Management };
}
