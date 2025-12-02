using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MToGo.PartnerService.Exceptions;
using MToGo.PartnerService.Logging;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Services;
using MToGo.Shared.Security.Authorization;

namespace MToGo.PartnerService.Controllers;

[ApiController]
[Route("api/v1/partners")]
public class PartnersController : ControllerBase
{
    private readonly IPartnerService _partnerService;
    private readonly ILogger<PartnersController> _logger;

    public PartnersController(IPartnerService partnerService, ILogger<PartnersController> logger)
    {
        _partnerService = partnerService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new partner with menu
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CreatePartnerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] PartnerRegisterRequest request)
    {
        try
        {
            var result = await _partnerService.RegisterPartnerAsync(request);
            return Created($"/api/v1/partners/{result.Id}", result);
        }
        catch (EmptyMenuException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DuplicateEmailException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Log in an existing partner
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PartnerLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] PartnerLoginRequest request)
    {
        try
        {
            var result = await _partnerService.LoginAsync(request);
            return Ok(result);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }
    }

    /// <summary>
    /// Get partner details including menu items
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = UserRoles.Partner)]
    [ProducesResponseType(typeof(PartnerDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPartner(int id)
    {
        // Verify the authenticated user is the partner
        var userIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || userId != id)
        {
            return Forbid();
        }

        var result = await _partnerService.GetPartnerByIdAsync(id);
        if (result == null)
        {
            return NotFound(new { error = $"Partner with ID {id} not found." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Add a new menu item to the partner's menu
    /// </summary>
    [HttpPost("{id}/menu/items")]
    [Authorize(Roles = UserRoles.Partner)]
    [ProducesResponseType(typeof(CreateMenuItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMenuItem(int id, [FromBody] CreateMenuItemRequest request)
    {
        _logger.ReceivedAddMenuItemRequest(id);

        // Verify the authenticated user is the partner
        var userIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || userId != id)
        {
            _logger.AddMenuItemFailed(id, "Unauthorized access");
            return Forbid();
        }

        try
        {
            var result = await _partnerService.AddMenuItemAsync(id, request);
            _logger.AddMenuItemCompleted(id, result.Id);
            return Created($"/api/v1/partners/{id}/menu/items/{result.Id}", result);
        }
        catch (PartnerNotFoundException ex)
        {
            _logger.AddMenuItemFailed(id, ex.Message);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing menu item
    /// </summary>
    [HttpPatch("{id}/menu/items/{foodItemId}")]
    [Authorize(Roles = UserRoles.Partner)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMenuItem(int id, int foodItemId, [FromBody] UpdateMenuItemRequest request)
    {
        _logger.ReceivedUpdateMenuItemRequest(id, foodItemId);

        // Verify the authenticated user is the partner
        var userIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || userId != id)
        {
            _logger.UpdateMenuItemFailed(id, foodItemId, "Unauthorized access");
            return Forbid();
        }

        // Validate that at least one field is provided
        if (string.IsNullOrWhiteSpace(request.Name) && !request.Price.HasValue)
        {
            _logger.UpdateMenuItemFailed(id, foodItemId, "No fields to update");
            return BadRequest(new { error = "At least one field (name or price) must be provided." });
        }

        // Validate price if provided
        if (request.Price.HasValue && request.Price.Value <= 0)
        {
            _logger.UpdateMenuItemFailed(id, foodItemId, "Invalid price");
            return BadRequest(new { error = "Price must be greater than 0." });
        }

        try
        {
            await _partnerService.UpdateMenuItemAsync(id, foodItemId, request);
            _logger.UpdateMenuItemCompleted(id, foodItemId);
            return Ok();
        }
        catch (PartnerNotFoundException ex)
        {
            _logger.UpdateMenuItemFailed(id, foodItemId, ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (MenuItemNotFoundException ex)
        {
            _logger.UpdateMenuItemFailed(id, foodItemId, ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedMenuItemAccessException ex)
        {
            _logger.UpdateMenuItemFailed(id, foodItemId, ex.Message);
            return Forbid();
        }
    }

    /// <summary>
    /// Delete a menu item
    /// </summary>
    [HttpDelete("{id}/menu/items/{foodItemId}")]
    [Authorize(Roles = UserRoles.Partner)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMenuItem(int id, int foodItemId)
    {
        _logger.ReceivedDeleteMenuItemRequest(id, foodItemId);

        // Verify the authenticated user is the partner
        var userIdClaim = User.FindFirst("id")?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || userId != id)
        {
            _logger.DeleteMenuItemFailed(id, foodItemId, "Unauthorized access");
            return Forbid();
        }

        try
        {
            await _partnerService.DeleteMenuItemAsync(id, foodItemId);
            _logger.DeleteMenuItemCompleted(id, foodItemId);
            return Ok();
        }
        catch (PartnerNotFoundException ex)
        {
            _logger.DeleteMenuItemFailed(id, foodItemId, ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (MenuItemNotFoundException ex)
        {
            _logger.DeleteMenuItemFailed(id, foodItemId, ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedMenuItemAccessException ex)
        {
            _logger.DeleteMenuItemFailed(id, foodItemId, ex.Message);
            return Forbid();
        }
    }
}
