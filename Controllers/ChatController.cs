using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using ApiHelpFast.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ChatController(ApplicationDbContext db) => _db = db;

    // GET /api/chat?chamadoId=123
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? chamadoId)
    {
        var q = _db.Chats.AsNoTracking().AsQueryable();
        if (chamadoId.HasValue)
            q = q.Where(c => c.ChamadoId == chamadoId.Value);

        var items = await q.OrderBy(c => c.DataEnvio).Select(c => new { c.Id, c.Mensagem, c.Tipo, c.DataEnvio, c.ChamadoId, c.ParentChatId }).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var chat = await _db.Chats.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (chat == null) return NotFound();
        return Ok(new { chat.Id, chat.Mensagem, chat.Tipo, chat.DataEnvio, chat.ChamadoId, chat.ParentChatId });
    }

    // POST /api/chat
    // Body: { "motivo": "mensagem aqui", "chamadoId": 123, "chatId": 456 (opcional) }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChatDto dto)
    {
        if (dto == null) return BadRequest(new { error = "Dados obrigatórios" });
        if (string.IsNullOrWhiteSpace(dto.Motivo)) return BadRequest(new { error = "Motivo (mensagem) obrigatório" });
        if (!dto.ChamadoId.HasValue || dto.ChamadoId.Value <= 0) return BadRequest(new { error = "chamadoId obrigatório" });

        // load chamado to get cliente/tecnico
        var chamado = await _db.Chamados.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.ChamadoId.Value);
        if (chamado == null) return NotFound(new { error = "Chamado não encontrado" });

        var now = DateTime.UtcNow;

        var messageValue = dto.Motivo.Trim();
        if (messageValue.Length > 2000) messageValue = messageValue.Substring(0, 2000);

        var chat = new Chat
        {
            ChamadoId = dto.ChamadoId.Value,
            Mensagem = messageValue,
            Tipo = "Usuario",
            DataEnvio = now,
            RemetenteId = chamado.ClienteId,
            DestinatarioId = chamado.TecnicoId,
            ParentChatId = dto.ChatId.HasValue && dto.ChatId.Value > 0 ? dto.ChatId : null
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();

        // return only the 3 fields requested by user
        return CreatedAtAction(nameof(GetById), new { id = chat.Id }, new { chatId = chat.Id, motivo = chat.Mensagem, chamadoId = chat.ChamadoId });
    }
}

public class CreateChatDto { public int? ChamadoId { get; set; } public string Motivo { get; set; } = string.Empty; public int? ChatId { get; set; } }