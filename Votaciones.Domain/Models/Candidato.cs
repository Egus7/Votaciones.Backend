using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static Votaciones.Domain.Enums.EnumsEleccion;

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

        public Guid ListaElectoralId { get; set; }
        [ForeignKey(nameof(ListaElectoralId))]
        [JsonIgnore]
        public ListaElectoral? ListaElectoral { get; set; }

        [Required]
        [MaxLength(200)]
        public string NombreCandidato { get; set; } = string.Empty;
        public TipoCandidato TipoCandidato { get; set; }
        public int? Orden { get; set; }
        public bool Activo { get; set; }
    }
}
