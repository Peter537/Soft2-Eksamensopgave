using MToGo.AgentService.Models;

namespace MToGo.AgentService.Services;

public interface IAgentService
{
    Task<CreateAgentResponse> RegisterAgentAsync(AgentRegisterRequest request);
    Task<AgentLoginResponse> LoginAsync(AgentLoginRequest request);
    Task<AgentProfileResponse> GetAgentAsync(int id);
    Task DeleteAgentAsync(int id);
    Task<bool> SetActiveStatusAsync(int id, bool isActive);
}
