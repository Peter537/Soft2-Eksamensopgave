using LegacyMToGoSystem.DTOs;
using LegacyMToGoSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace LegacyMToGoSystem.Controllers;

[ApiController]
[Route("api/businesspartners")]
public class BusinessPartnersController : ControllerBase
{
    private readonly IBusinessPartnerService _service;

    public BusinessPartnersController(IBusinessPartnerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusinessPartnerResponseDto>>> GetAll()
    {
        var partners = await _service.GetAllPartnersAsync();
        return Ok(partners);
    }

    [HttpGet("{id}/menu")]
    public async Task<ActionResult<MenuResponseDto>> GetMenu(int id)
    {
        var menu = await _service.GetPartnerMenuAsync(id);
        if (menu == null)
            return NotFound($"Business partner {id} not found");

        return Ok(menu);
    }

    [HttpPost]
    public async Task<ActionResult<BusinessPartnerResponseDto>> Create([FromBody] CreateBusinessPartnerDto dto)
    {
        var created = await _service.CreatePartnerAsync(dto);
        return CreatedAtAction(nameof(GetMenu), new { id = created.Id }, created);
    }
}
