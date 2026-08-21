
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("Candidato")]
    public class Candidato
    {
        [Key]
        public Guid IdCandidato { get; set; }
        public Guid EleccionId { get; set; }

        [ForeignKey(nameof(EleccionId))]
        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }
        [Required]
        [MaxLength(200)]
        public string NombreCandidato { get; set; } = string.Empty;
        [Required]
        public int NumeroLista { get; set; }
        [MaxLength(200)]
        public string? Lista { get; set; }
        [Required]
        public bool Activo { get; set; }
    }
}
