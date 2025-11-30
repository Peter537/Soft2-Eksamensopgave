using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MToGo.CustomerService.Exceptions;
using MToGo.CustomerService.Models;
using MToGo.CustomerService.Services;
using MToGo.Shared.Models.Customer;
using MToGo.Shared.Security;

namespace MToGo.CustomerService.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IUserContextAccessor _userContextAccessor;

    public CustomersController(ICustomerService customerService, IUserContextAccessor userContextAccessor)
    {
        _customerService = customerService;
        _userContextAccessor = userContextAccessor;
    }

    /// <summary>
    /// Register a new customer
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CreateCustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] Customer request)
    {
        try
        {
            var result = await _customerService.RegisterCustomerAsync(request);
            return Created($"/api/v1/customers/{result.Id}", result);
        }
        catch (DuplicateEmailException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Login a customer
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CustomerLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] CustomerLoginRequest request)
    {
        try
        {
            var result = await _customerService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }
    }

    /// <summary>
    /// Get customer profile
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.CustomerOrManagement)]
    [ProducesResponseType(typeof(CustomerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(int id)
    {
        var userContext = _userContextAccessor.UserContext;
        
        // Customers can only access their own profile, Management can access all
        if (userContext.Role == UserRoles.Customer && userContext.UserId != id)
        {
            return Forbid();
        }

        try
        {
            var result = await _customerService.GetCustomerAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Customer not found." });
        }
    }

    /// <summary>
    /// Update customer profile
    /// </summary>
    [HttpPatch("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.CustomerOrManagement)]
    [ProducesResponseType(typeof(CustomerProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] CustomerUpdateRequest request)
    {
        var userContext = _userContextAccessor.UserContext;
        
        // Customers can only update their own profile, Management can update all
        if (userContext.Role == UserRoles.Customer && userContext.UserId != id)
        {
            return Forbid();
        }

        try
        {
            var result = await _customerService.UpdateCustomerAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Customer not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
