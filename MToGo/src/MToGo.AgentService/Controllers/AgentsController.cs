using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MToGo.AgentService.Exceptions;
using MToGo.AgentService.Models;
using MToGo.AgentService.Services;
using MToGo.Shared.Security.Authorization;
using MToGo.Shared.Security.Context;

namespace MToGo.AgentService.Controllers;

[ApiController]
[Route("api/v1/agents")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IUserContextAccessor _userContextAccessor;

    public AgentsController(IAgentService agentService, IUserContextAccessor userContextAccessor)
    {
        _agentService = agentService;
        _userContextAccessor = userContextAccessor;
    }

    /// <summary>
    /// Register a new agent
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CreateAgentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] AgentRegisterRequest request)
    {
        try
        {
            var result = await _agentService.RegisterAgentAsync(request);
            return Created($"/api/v1/agents/{result.Id}", result);
        }
        catch (DuplicateEmailException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Login an agent
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AgentLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AgentLoginRequest request)
    {
        try
        {
            var result = await _agentService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }
    }

    /// <summary>
    /// Get agent profile
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AgentOrManagement)]
    [ProducesResponseType(typeof(AgentProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(int id)
    {
        var userContext = _userContextAccessor.UserContext;

        // Agents can only view their own profile, Management can view any
        if (userContext.Role != UserRoles.Management && userContext.Id != id)
        {
            return Forbid();
        }

        try
        {
            var result = await _agentService.GetAgentAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Agent not found." });
        }
    }

    /// <summary>
    /// Delete agent account (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.AgentOrManagement)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var userContext = _userContextAccessor.UserContext;

        // Agents can only delete their own account, Management can delete any
        if (userContext.Role != UserRoles.Management && userContext.Id != id)
        {
            return Forbid();
        }

        try
        {
            await _agentService.DeleteAgentAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Agent not found." });
        }
    }
}
