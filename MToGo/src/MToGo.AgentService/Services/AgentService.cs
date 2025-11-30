using MToGo.AgentService.Entities;
using MToGo.AgentService.Exceptions;
using MToGo.AgentService.Models;
using MToGo.AgentService.Repositories;
using MToGo.Shared.Security;

namespace MToGo.AgentService.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IAgentRepository agentRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        ILogger<AgentService> logger)
    {
        _agentRepository = agentRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<CreateAgentResponse> RegisterAgentAsync(AgentRegisterRequest request)
    {
        _logger.LogInformation("Registering new agent with email: {Email}", request.Email);

        if (await _agentRepository.EmailExistsAsync(request.Email))
        {
            _logger.LogWarning("Agent registration failed: Email already exists - {Email}", request.Email);
            throw new DuplicateEmailException("An agent with this email already exists.");
        }

        var agent = new Agent
        {
            Name = request.Name,
            Email = request.Email,
            Password = _passwordHasher.HashPassword(request.Password),
            IsActive = true
        };

        var createdAgent = await _agentRepository.CreateAsync(agent);
        _logger.LogInformation("Agent registered successfully with ID: {Id}", createdAgent.Id);

        return new CreateAgentResponse { Id = createdAgent.Id };
    }

    public async Task<AgentLoginResponse> LoginAsync(AgentLoginRequest request)
    {
        _logger.LogInformation("Agent login attempt for email: {Email}", request.Email);

        var agent = await _agentRepository.GetByEmailAsync(request.Email);

        if (agent == null || !_passwordHasher.VerifyPassword(request.Password, agent.Password))
        {
            _logger.LogWarning("Agent login failed: Invalid credentials for email - {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = _jwtTokenService.GenerateToken(agent.Id, agent.Email, UserRoles.Agent, agent.Name);
        _logger.LogInformation("Agent logged in successfully: {Id}", agent.Id);

        return new AgentLoginResponse { Jwt = token };
    }

    public async Task<AgentProfileResponse> GetAgentAsync(int id)
    {
        var agent = await _agentRepository.GetByIdAsync(id);

        if (agent == null)
        {
            throw new KeyNotFoundException("Agent not found.");
        }

        return new AgentProfileResponse
        {
            Id = agent.Id,
            Name = agent.Name,
            Email = agent.Email,
            IsActive = agent.IsActive
        };
    }

    public async Task DeleteAgentAsync(int id)
    {
        var agent = await _agentRepository.GetByIdAsync(id);

        if (agent == null)
        {
            throw new KeyNotFoundException("Agent not found.");
        }

        await _agentRepository.DeleteAsync(id);
        _logger.LogInformation("Agent deleted: {Id}", id);
    }
}
