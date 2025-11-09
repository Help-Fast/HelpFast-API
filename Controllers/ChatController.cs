using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ChatController(ApplicationDbContext db) => _db = db;

    // All chat endpoints are disabled because this database schema does not include a Chats table.
    [HttpGet]
    public IActionResult GetAll()
    {
        return NotFound(new { error = "Chat functionality is not available in this deployment." });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return NotFound(new { error = "Chat functionality is not available in this deployment." });
    }

    [HttpPost]
    public IActionResult Create()
    {
        return NotFound(new { error = "Chat functionality is not available in this deployment." });
    }
}
