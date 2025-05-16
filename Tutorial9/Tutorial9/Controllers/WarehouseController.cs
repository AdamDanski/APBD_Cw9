using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> RegisterProduct([FromBody] RegisterProductDTO dto)
    {
        try
        {
            var id = await _dbService.RegisterProductAsync(dto);
            return Ok(new { id });
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return NotFound(e.Message);
        }
    }
    [HttpPost("procedure")]
    public async Task<IActionResult> RegisterWithProcedure([FromBody] RegisterProductDTO dto)
    {
        try
        {
            var id = await _dbService.ProcedureAsync(dto);
            return Ok(new { id });
        }
        catch (SqlException e)
        {
            return BadRequest($"SQL Error: {e.Message}");
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

}