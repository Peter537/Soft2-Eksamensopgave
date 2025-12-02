using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MToGo.PartnerService.Exceptions;
using MToGo.PartnerService.Models;
using MToGo.PartnerService.Services;

namespace MToGo.PartnerService.Controllers;

[ApiController]
[Route("api/v1/partners")]
public class PartnersController : ControllerBase
{
    private readonly IPartnerService _partnerService;

    public PartnersController(IPartnerService partnerService)
    {
        _partnerService = partnerService;
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
}
