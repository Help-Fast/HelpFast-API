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
        if (user == null) return NotFound();
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
        var user = await _db.Usuarios.FindAsync(id);
        if (user == null) return NotFound();

        // Não permitir deletar admin padrão (opcional) ou se vinculado a chamados importantes
        // Para simplicidade, permitimos remoção; cascades serão aplicadas conforme model
        _db.Usuarios.Remove(user);
        await _db.SaveChangesAsync();

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