using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using ApiHelpFast.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatIaResultsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ChatIaResultsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.ChatIaResults.AsNoTracking().OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.ChatIaResults.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChatIaResult dto)
    {
        if (dto == null) return BadRequest(new { error = "Dados obrigatórios" });
        dto.CreatedAt = dto.CreatedAt == default ? DateTime.UtcNow : dto.CreatedAt;
        _db.ChatIaResults.Add(dto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }
}
