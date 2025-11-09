using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiHelpFast.Models
{
    [Table("ChatIaResults", Schema = "dbo")]
    public class ChatIaResult
    {
        [Key]
        public int Id { get; set; }

        public int ChatId { get; set; }

        [MaxLength(4000)]
        public string? ResultJson { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
