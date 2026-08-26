using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("Zona")]
    public class Zona
    {
        [Key]
        public Guid IdZona { get; set; }

        [Required]
        public Guid ParroquiaId { get; set; }

        [ForeignKey(nameof(ParroquiaId))]
        [JsonIgnore]
        public Parroquia? Parroquia { get; set; }

        [Required]
        [MaxLength(150)]
        public string NombreZona { get; set; } = string.Empty;

        [Required]
        public bool Activo { get; set; }
    }
}
