using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("Canton")]
    public class Canton
    {
        [Key]
        public Guid IdCanton { get; set; }

        [Required]
        public Guid ProvinciaId { get; set; }

        [ForeignKey(nameof(ProvinciaId))]
        [JsonIgnore]
        public Provincia? Provincia { get; set; }

        [Required]
        [MaxLength(100)]
        public string NombreCanton { get; set; } = string.Empty;

        [Required]
        public bool Activo { get; set; }
    }
}
