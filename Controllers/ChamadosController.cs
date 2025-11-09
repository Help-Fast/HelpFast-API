using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using ApiHelpFast.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChamadosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ChamadosController(ApplicationDbContext db) => _db = db;

    // GET /api/chamados
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.Chamados
            .AsNoTracking()
            .Select(c => new { c.Id, c.Motivo, c.ClienteId, c.TecnicoId, c.Status, c.DataAbertura, c.DataFechamento })
            .ToListAsync();
        return Ok(items);
    }

    // POST /api/chamados/abrir
    [AllowAnonymous]
    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirChamadoDto dto)
    {
        if (dto == null) return BadRequest(new { error = "Dados do chamado obrigatórios" });
        if (dto.ClienteId <= 0) return BadRequest(new { error = "ClienteId obrigatório" });
        if (string.IsNullOrWhiteSpace(dto.Motivo)) return BadRequest(new { error = "Motivo obrigatório" });

        // validate cliente exists
        var cliente = await _db.Usuarios.FindAsync(dto.ClienteId);
        if (cliente == null) return BadRequest(new { error = "Cliente inválido" });

        var now = DateTime.UtcNow;
        var strategy = _db.Database.CreateExecutionStrategy();

        int createdId = 0;
        string createdStatus = "Aberto";

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                var chamado = new Chamado
                {
                    ClienteId = dto.ClienteId,
                    Motivo = dto.Motivo.Trim(),
                    Status = "Aberto",
                    DataAbertura = now,
                };

                _db.Chamados.Add(chamado);
                await _db.SaveChangesAsync();

                // Optionally auto-assign a technician
                var tecnico = await _db.Usuarios.Include(u => u.Cargo).FirstOrDefaultAsync(u => u.Cargo != null && u.Cargo.Nome == "Tecnico");
                if (tecnico != null)
                {
                    chamado.TecnicoId = tecnico.Id;
                    chamado.Status = "Em Atendimento";
                    _db.Historicos.Add(new HistoricoChamado { ChamadoId = chamado.Id, Acao = $"Atribuído a técnico {tecnico.Nome}", Data = now, UsuarioId = tecnico.Id });
                    await _db.SaveChangesAsync();
                }

                createdId = chamado.Id;
                createdStatus = chamado.Status ?? createdStatus;

                await tx.CommitAsync();
            });

            return CreatedAtAction(nameof(GetById), new { id = createdId }, new { Id = createdId, Status = createdStatus });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, detail = ex.InnerException?.Message });
        }
    }

    // GET /api/chamados/{id}
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var chamado = await _db.Chamados
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
        if (chamado == null) return NotFound();

        return Ok(new
        {
            chamado.Id,
            chamado.Motivo,
            chamado.ClienteId,
            chamado.TecnicoId,
            chamado.Status,
            chamado.DataAbertura,
            chamado.DataFechamento
        });
    }

    // GET /api/chamados/meus/{clienteId}
    [AllowAnonymous]
    [HttpGet("meus/{clienteId}")]
    public async Task<IActionResult> MeusChamados(int clienteId)
    {
        var list = await _db.Chamados
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .OrderByDescending(c => c.DataAbertura)
            .Select(c => new { c.Id, c.Motivo, c.Status, c.DataAbertura, c.TecnicoId })
            .ToListAsync();
        return Ok(list);
    }

    // GET status for polling
    [AllowAnonymous]
    [HttpGet("status/{id}")]
    public async Task<IActionResult> GetStatus(int id)
    {
        var chamado = await _db.Chamados.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (chamado == null) return NotFound(new { success = false, error = "Chamado não encontrado" });
        return Ok(new { success = true, status = chamado.Status });
    }

    // PUT /api/chamados/{id}/status
    [Authorize]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Status)) return BadRequest(new { error = "Status obrigatório" });

        var chamado = await _db.Chamados.FindAsync(id);
        if (chamado == null) return NotFound();

        var status = dto.Status.Trim();
        if (string.Equals(status, "andamento", StringComparison.OrdinalIgnoreCase) || string.Equals(status, "em andamento", StringComparison.OrdinalIgnoreCase))
            status = "Em Atendimento";

        var allowed = new[] { "Aberto", "Em Atendimento", "Finalizado", "Cancelado" };
        if (!allowed.Contains(status)) return BadRequest(new { error = "Status inválido" });

        chamado.Status = status;
        if ((status == "Finalizado" || status == "Cancelado") && chamado.DataFechamento == null)
            chamado.DataFechamento = DateTime.UtcNow;

        if (status == "Em Atendimento" && dto.TecnicoId.HasValue)
            chamado.TecnicoId = dto.TecnicoId.Value;

        await _db.SaveChangesAsync();
        return Ok(new { chamado.Id, chamado.Status, chamado.TecnicoId, chamado.DataFechamento });
    }
}

public class AbrirChamadoDto { public int ClienteId { get; set; } public string Motivo { get; set; } = null!; }
public class UpdateStatusDto { public string Status { get; set; } = string.Empty; public int? TecnicoId { get; set; } }
