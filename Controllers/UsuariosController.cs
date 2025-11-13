using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using ApiHelpFast.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<Usuario> _pwdHasher = new();

    public UsuariosController(ApplicationDbContext db) => _db = db;

    // GET /api/usuarios
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Usuarios.Include(u => u.Cargo)
            .Select(u => new { u.Id, u.Nome, u.Email, u.Telefone, Cargo = u.Cargo != null ? u.Cargo.Nome : null, u.CargoId })
            .ToListAsync();
        return Ok(list);
    }

    // GET /api/usuarios/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Usuarios.Include(u => u.Cargo)
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.Nome, u.Email, u.Telefone, Cargo = u.Cargo != null ? u.Cargo.Nome : null, u.CargoId })
            .FirstOrDefaultAsync();
        if (user == null) return NotFound(new { error = "Usuário não encontrado", id });
        return Ok(user);
    }

    // POST /api/usuarios
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioDbo dbo)
    {
        if (dbo == null) return BadRequest(new { error = "Dados inválidos" });
        if (string.IsNullOrWhiteSpace(dbo.Nome)) return BadRequest(new { error = "Nome obrigatório" });
        if (string.IsNullOrWhiteSpace(dbo.Email)) return BadRequest(new { error = "Email obrigatório" });
        if (string.IsNullOrWhiteSpace(dbo.Senha)) return BadRequest(new { error = "Senha obrigatório" });

        var email = dbo.Email.Trim();
        if (await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower())) return Conflict(new { error = "Email já cadastrado" });

        int cargoId = dbo.CargoId ?? 1;
        if (!await _db.Cargos.AnyAsync(c => c.Id == cargoId)) return BadRequest(new { error = "Cargo inválido" });

        var user = new Usuario { Nome = dbo.Nome.Trim(), Email = email, Telefone = dbo.Telefone?.Trim(), CargoId = cargoId };
        user.Senha = _pwdHasher.HashPassword(user, dbo.Senha);

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { user.Id, user.Nome, user.Email, user.Telefone, user.CargoId });
    }

    // DELETE /api/usuarios/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Try to load user; if not present, treat as idempotent success
        var user = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NoContent();
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                // 1) Find chamados where user is cliente (ids)
                var chamadosComoClienteIds = await _db.Chamados.Where(c => c.ClienteId == id).Select(c => c.Id).ToListAsync();

                if (chamadosComoClienteIds.Count > 0)
                {
                    // delete related historicos and chats for these chamados, then delete chamados
                    var ids = string.Join(",", chamadosComoClienteIds);
                    // use parameterized deletes per idlist not supported; execute per id to avoid SQL concat
                    foreach (var cid in chamadosComoClienteIds)
                    {
                        await _db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.HistoricoChamados WHERE ChamadoId = {0}", cid);
                        await _db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.Chats WHERE ChamadoId = {0}", cid);
                        await _db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.Chamados WHERE Id = {0}", cid);
                    }
                }

                // 2) For chamados where user is tecnico, unset tecnico and insert historico
                var chamadosComoTecnicoIds = await _db.Chamados.Where(c => c.TecnicoId == id).Select(c => c.Id).ToListAsync();
                var now = DateTime.UtcNow;
                foreach (var cid in chamadosComoTecnicoIds)
                {
                    await _db.Database.ExecuteSqlRawAsync("UPDATE dbo.Chamados SET TecnicoId = NULL, Status = {1} WHERE Id = {0}", cid, "Aberto");

                    var acao = $"Técnico {user.Nome} removido do chamado";
                    // Insert historico
                    await _db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO dbo.HistoricoChamados (ChamadoId, Acao, Data, UsuarioId) VALUES ({0}, {1}, {2}, {3})",
                        cid, acao, now, id);
                }

                // 3) Remove historicos that reference this user (UsuarioId)
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.HistoricoChamados WHERE UsuarioId = {0}", id);

                // 4) Nullify user references in chats (RemetenteId, DestinatarioId)
                await _db.Database.ExecuteSqlRawAsync("UPDATE dbo.Chats SET RemetenteId = NULL WHERE RemetenteId = {0}", id);
                await _db.Database.ExecuteSqlRawAsync("UPDATE dbo.Chats SET DestinatarioId = NULL WHERE DestinatarioId = {0}", id);

                // 5) Finally delete the user
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.Usuarios WHERE Id = {0}", id);

                await tx.CommitAsync();
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Falha ao remover usuário", detail = ex.Message });
        }

        return NoContent();
    }
}

public class CreateUsuarioDbo
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public int? CargoId { get; set; }
}