using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiHelpFast.Models
{
    [Table("Chamados", Schema = "dbo")]
    public class Chamado
    {
        [Key]
        public int Id { get; set; }

        // Campo apenas para UI (não mapeado no banco)
        [NotMapped]
        public string? Assunto { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Motivo { get; set; } = null!;

        // FK para Cliente (usuario que abriu)
        public int ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public Usuario? Cliente { get; set; }

        // FK para Técnico (pode ser nulo até ser atribuído)
        public int? TecnicoId { get; set; }
        [ForeignKey(nameof(TecnicoId))]
        public Usuario? Tecnico { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        public DateTime DataAbertura { get; set; }
        public DateTime? DataFechamento { get; set; }

        // Navegação para histórico de alterações do chamado
        public ICollection<HistoricoChamado> Historicos { get; set; } = new List<HistoricoChamado>();
    }
}