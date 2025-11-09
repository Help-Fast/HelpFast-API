using Microsoft.AspNetCore.Mvc;
using ApiHelpFast.Data;
using ApiHelpFast.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace ApiHelpFast.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoginController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<Usuario> _pwdHasher = new();

    public LoginController(ApplicationDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] LoginDbo dbo)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Busca por email - torna insensível a caixa para evitar erros de cadastro
        var email = dbo.Email?.Trim();
        if (string.IsNullOrEmpty(email)) return BadRequest(new { error = "Email obrigatório" });

        var user = await _db.Usuarios.Include(u => u.Cargo)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null) return Unauthorized(new { error = "Credenciais inválidas" });

        // Proteção extra: se senha não está setada, nega
        if (string.IsNullOrEmpty(user.Senha) || string.IsNullOrWhiteSpace(dbo.Senha))
            return Unauthorized(new { error = "Credenciais inválidas" });

        var dbHash = user.Senha.Trim();
        bool isValid = false;

        try
        {
            var verify = _pwdHasher.VerifyHashedPassword(user, dbHash, dbo.Senha);
            if (verify != PasswordVerificationResult.Failed)
                isValid = true;
        }
        catch
        {
        }

        if (!isValid)
        {
            try
            {
                var shaBytes = SHA256.HashData(Encoding.UTF8.GetBytes(dbo.Senha));
                var sb = new StringBuilder();
                foreach (var b in shaBytes) sb.Append(b.ToString("x2"));
                var shaHex = sb.ToString();

                var shaBase64 = Convert.ToBase64String(shaBytes);

                if (string.Equals(dbHash, shaHex, StringComparison.OrdinalIgnoreCase) || string.Equals(dbHash, shaBase64, StringComparison.Ordinal))
                    isValid = true;
            }
            catch
            {
            }
        }

        if (!isValid) return Unauthorized(new { error = "Credenciais inválidas" });

        return Ok(new { user.Id, user.Nome, Cargo = user.Cargo?.Nome, user.CargoId });
    }

    // NOVO: endpoint para registro de usuário
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDbo dbo)
    {
        if (dbo == null) return BadRequest(new { error = "Dados inválidos" });

        // Valida campos obrigatórios
        if (string.IsNullOrWhiteSpace(dbo.Nome)) return BadRequest(new { error = "Nome obrigatório" });
        if (string.IsNullOrWhiteSpace(dbo.Email)) return BadRequest(new { error = "Email obrigatório" });
        if (string.IsNullOrWhiteSpace(dbo.Senha)) return BadRequest(new { error = "Senha obrigatório" });
        if (string.IsNullOrWhiteSpace(dbo.Telefone)) return BadRequest(new { error = "Telefone obrigatório" });

        var email = dbo.Email.Trim();
        var exists = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        if (exists) return Conflict(new { error = "Email já cadastrado" });

        // define cargo padrão como Cliente (id = 1) caso não informado
        int cargoId = dbo.CargoId ?? 1;

        // valida se o cargo existe
        var cargoExists = await _db.Cargos.AnyAsync(c => c.Id == cargoId);
        if (!cargoExists) return BadRequest(new { error = "Cargo inválido" });

        var user = new Usuario
        {
            Nome = dbo.Nome.Trim(),
            Email = email,
            Telefone = dbo.Telefone.Trim(),
            CargoId = cargoId
        };

        user.Senha = _pwdHasher.HashPassword(user, dbo.Senha);

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        // Retorna localização para endpoint de usuários (GetById)
        return CreatedAtAction("GetById", "Usuarios", new { id = user.Id }, new { user.Id, user.Nome, user.Email, user.Telefone, user.CargoId });
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}

public class LoginDbo
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class RegisterDbo
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public int? CargoId { get; set; }
}
