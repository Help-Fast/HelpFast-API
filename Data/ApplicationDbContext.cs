using Microsoft.EntityFrameworkCore;
using ApiHelpFast.Models;

namespace ApiHelpFast.Data;
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Cargo> Cargos { get; set; } = null!;
    public DbSet<Chamado> Chamados { get; set; } = null!;
    public DbSet<HistoricoChamado> Historicos { get; set; } = null!;
    public DbSet<Faq> Faqs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // garante schema dbo para todas as tabelas
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<Cargo>(b =>
        {
            b.ToTable("Cargos", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(50).IsRequired();
            b.HasIndex(x => x.Nome).IsUnique();
        });

        modelBuilder.Entity<Usuario>(b =>
        {
            b.ToTable("Usuarios", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            b.Property(x => x.Email).HasMaxLength(200).IsRequired();
            b.Property(x => x.Senha).HasMaxLength(200).IsRequired();
            b.Property(x => x.Telefone).HasMaxLength(20).IsRequired(false);
            b.HasIndex(x => x.Email).IsUnique();
            b.HasOne(x => x.Cargo)
             .WithMany(c => c.Usuarios!)
             .HasForeignKey(x => x.CargoId)
             .OnDelete(DeleteBehavior.Restrict)
             .IsRequired();
        });

        modelBuilder.Entity<Chamado>(b =>
        {
            b.ToTable("Chamados", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Motivo).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Aberto");
            b.Property(x => x.DataAbertura).IsRequired();
            b.HasOne(x => x.Cliente)
             .WithMany(u => u.ChamadosComoCliente!)
             .HasForeignKey(x => x.ClienteId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired();

            b.HasOne(x => x.Tecnico)
             .WithMany(u => u.ChamadosComoTecnico!)
             .HasForeignKey(x => x.TecnicoId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        modelBuilder.Entity<HistoricoChamado>(b =>
        {
            b.ToTable("HistoricoChamados", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Acao).HasMaxLength(500).IsRequired();
            b.Property(x => x.Data).IsRequired();
            b.HasOne(x => x.Chamado)
             .WithMany(c => c.Historicos!)
             .HasForeignKey(x => x.ChamadoId)
             .OnDelete(DeleteBehavior.Cascade)
             .IsRequired();
        });

        modelBuilder.Entity<Faq>(b =>
        {
            b.ToTable("Faqs", "dbo");
            b.HasKey(x => x.Id);
            b.Property(x => x.Pergunta).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Resposta).HasMaxLength(4000).IsRequired();
        });

        // Seed básico de cargos (útil para registrar clientes e testes)
        modelBuilder.Entity<Cargo>().HasData(
            new Cargo { Id = 1, Nome = "Cliente" },
            new Cargo { Id = 2, Nome = "Tecnico" },
            new Cargo { Id = 3, Nome = "Admin" }
        );

        // Certifique-se de chamar o base para evitar problemas em alguns provedores/versões
        base.OnModelCreating(modelBuilder);
    }
}
