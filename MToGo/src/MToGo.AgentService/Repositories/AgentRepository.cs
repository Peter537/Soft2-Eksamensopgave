using Microsoft.EntityFrameworkCore;
using MToGo.AgentService.Data;
using MToGo.AgentService.Entities;

namespace MToGo.AgentService.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly AgentDbContext _context;

    public AgentRepository(AgentDbContext context)
    {
        _context = context;
    }

    public async Task<Agent?> GetByIdAsync(int id)
    {
        return await _context.Agents
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
    }

    public async Task<Agent?> GetByEmailAsync(string email)
    {
        return await _context.Agents
            .FirstOrDefaultAsync(a => a.Email == email && !a.IsDeleted);
    }

    public async Task<Agent> CreateAsync(Agent agent)
    {
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();
        return agent;
    }

    public async Task<Agent> UpdateAsync(Agent agent)
    {
        agent.UpdatedAt = DateTime.UtcNow;
        _context.Agents.Update(agent);
        await _context.SaveChangesAsync();
        return agent;
    }

    public async Task DeleteAsync(int id)
    {
        var agent = await GetByIdAsync(id);
        if (agent != null)
        {
            agent.IsDeleted = true;
            agent.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Agents.AnyAsync(a => a.Email == email && !a.IsDeleted);
    }

    public async Task UpdateActiveStatusAsync(int id, bool isActive)
    {
        var agent = await GetByIdAsync(id);
        if (agent != null)
        {
            agent.IsActive = isActive;
            agent.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
