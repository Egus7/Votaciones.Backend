using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("Parroquia")]
    public class Parroquia
    {
        [Key]
        public Guid IdParroquia { get; set; }

        [Required]
        public Guid CantonId { get; set; }

        [ForeignKey(nameof(CantonId))]
        [JsonIgnore]
        public Canton? Canton { get; set; }

        [Required]
        [MaxLength(100)]
        public string NombreParroquia { get; set; } = string.Empty;

        [Required]
        public bool Activo { get; set; }
    }

}
