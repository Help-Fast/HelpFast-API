using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiHelpFast.Models
{
    [Table("Chats", Schema = "dbo")]
    public class Chat
    {
        [Key]
        public int Id { get; set; }

        // FK para Chamado (opcional: permite associar uma mensagem a um chamado)
        public int? ChamadoId { get; set; }
        [ForeignKey(nameof(ChamadoId))]
        public Chamado? Chamado { get; set; }

        // Remetente / Destinatário (ids e navegações) - DB expects non-null values
        public int RemetenteId { get; set; }
        [ForeignKey(nameof(RemetenteId))]
        public Usuario? Remetente { get; set; }

        public int DestinatarioId { get; set; }
        [ForeignKey(nameof(DestinatarioId))]
        public Usuario? Destinatario { get; set; }

        [MaxLength(2000)]
        [Required]
        public string Mensagem { get; set; } = null!;

        public DateTime DataEnvio { get; set; }

        // Tipo de mensagem persistido: "Usuario" para cliente, "Assistente"/"Sistema" para IA/técnico
        [MaxLength(50)]
        [Required]
        public string Tipo { get; set; } = "Usuario";

        // Indica se a mensagem foi enviada pelo cliente (true) ou não — não mapeada (computed from Tipo)
        [NotMapped]
        public bool EnviadoPorCliente
        {
            get => string.Equals(Tipo, "Usuario", StringComparison.OrdinalIgnoreCase);
            set => Tipo = value ? "Usuario" : "Assistente";
        }

        // Propriedade de alias para compatibilidade com views
        [NotMapped]
        public DateTime DataHora
        {
            get { return DataEnvio; }
            set { DataEnvio = value; }
        }
    }
}