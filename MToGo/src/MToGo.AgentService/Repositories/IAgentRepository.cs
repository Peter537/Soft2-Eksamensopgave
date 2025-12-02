using MToGo.AgentService.Entities;

namespace MToGo.AgentService.Repositories;

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(int id);
    Task<Agent?> GetByEmailAsync(string email);
    Task<Agent> CreateAsync(Agent agent);
    Task<Agent> UpdateAsync(Agent agent);
    Task DeleteAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task UpdateActiveStatusAsync(int id, bool isActive);
}
