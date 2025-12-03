using Microsoft.AspNetCore.Mvc;
using MToGo.AgentBonusService.Models;
using MToGo.AgentBonusService.Services;

namespace MToGo.AgentBonusService.Controllers;

/// <summary>
/// Controller for calculating agent delivery bonuses.
/// Orchestrates calls to Order Service, Feedback Hub, and Agent Service
/// to compute bonus amounts based on contribution and performance.
/// </summary>
[ApiController]
[Route("api/v1/agent-bonus")]
public class AgentBonusController : ControllerBase
{
    private readonly IAgentBonusService _bonusService;
    private readonly ILogger<AgentBonusController> _logger;

    public AgentBonusController(
        IAgentBonusService bonusService,
        ILogger<AgentBonusController> logger)
    {
        _bonusService = bonusService;
        _logger = logger;
    }

    /// <summary>
    /// Extract the Bearer token from the Authorization header.
    /// </summary>
    private string? GetAuthToken()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return null;
        }
        return authHeader.Substring("Bearer ".Length).Trim();
    }

    /// <summary>
    /// Calculate bonus for a specific agent within a date range.
    /// </summary>
    /// <param name="agentId">The agent's unique identifier</param>
    /// <param name="startDate">Start of the bonus period (inclusive, ISO 8601 format)</param>
    /// <param name="endDate">End of the bonus period (inclusive, ISO 8601 format)</param>
    /// <returns>Detailed bonus calculation breakdown</returns>
    /// <response code="200">Returns the bonus calculation result</response>
    /// <response code="400">If the date range is invalid</response>
    /// <response code="404">If the agent is not found</response>
    [HttpGet("{agentId:int}")]
    [ProducesResponseType(typeof(BonusCalculationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BonusCalculationResponse>> CalculateBonus(
        int agentId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        // Validate date range
        if (endDate < startDate)
        {
            _logger.LogWarning("Invalid date range: endDate {EndDate} is before startDate {StartDate}", 
                endDate, startDate);
            return BadRequest(new { error = "End date must be after start date" });
        }

        // Limit date range to prevent abuse (max 1 year)
        if ((endDate - startDate).TotalDays > 366)
        {
            _logger.LogWarning("Date range too large: {Days} days", (endDate - startDate).TotalDays);
            return BadRequest(new { error = "Date range cannot exceed 1 year" });
        }

        _logger.LogInformation(
            "Calculating bonus for agent {AgentId} from {StartDate} to {EndDate}",
            agentId, startDate, endDate);

        try
        {
            var authToken = GetAuthToken();
            var result = await _bonusService.CalculateBonusAsync(agentId, startDate, endDate, authToken);
            
            if (result == null)
            {
                _logger.LogWarning("Agent {AgentId} not found", agentId);
                return NotFound(new { error = $"Agent with ID {agentId} not found" });
            }

            _logger.LogInformation(
                "Bonus calculated for agent {AgentId}: {BonusAmount:C} ({DeliveryCount} deliveries, qualified: {Qualified})",
                agentId, result.BonusAmount, result.DeliveryCount, result.Qualified);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bonus for agent {AgentId}", agentId);
            return StatusCode(500, new { error = "An error occurred while calculating the bonus" });
        }
    }

    /// <summary>
    /// Get bonus preview with just the summary (lighter response).
    /// </summary>
    [HttpGet("{agentId:int}/preview")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetBonusPreview(
        int agentId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (endDate < startDate)
        {
            return BadRequest(new { error = "End date must be after start date" });
        }

        try
        {
            var authToken = GetAuthToken();
            var result = await _bonusService.CalculateBonusAsync(agentId, startDate, endDate, authToken);
            
            if (result == null)
            {
                return NotFound(new { error = $"Agent with ID {agentId} not found" });
            }

            // Return just the essential info
            return Ok(new
            {
                result.AgentId,
                result.AgentName,
                result.Period,
                result.Qualified,
                result.DeliveryCount,
                result.BonusAmount,
                result.Warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bonus preview for agent {AgentId}", agentId);
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Health check endpoint for the bonus service.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", service = "AgentBonusService" });
    }
}
