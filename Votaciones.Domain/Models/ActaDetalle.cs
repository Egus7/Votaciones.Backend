using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("ActaDetalle")]
    public class ActaDetalle
    {
        [Key]
        public Guid IdActaDetalle { get; set; }

        [Required]
        public Guid ActaId { get; set; }

        [ForeignKey(nameof(ActaId))]
        [JsonIgnore]
        public ActaEleccion? ActaCab { get; set; }

        [Required]
        public Guid CandidatoId { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(CandidatoId))]
        public Candidato? Candidato { get; set; }

        [Required]
        public int Votos { get; set; }
    }
}
