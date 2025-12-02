namespace MToGo.AgentService.Models;

public class AgentRegisterRequest
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class AgentLoginRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class AgentLoginResponse
{
    public required string Jwt { get; set; }
}

public class CreateAgentResponse
{
    public int Id { get; set; }
}

public class AgentProfileResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateActiveStatusRequest
{
    public bool Active { get; set; }
}
