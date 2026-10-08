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

        [Required]
        [MaxLength(200)]
        public string NombreCandidato { get; set; } = string.Empty;
        public TipoCandidato TipoCandidato { get; set; }
        public int? Orden { get; set; }
        
        // Ámbito territorial del candidato
        public Guid? ProvinciaId { get; set; }
        [ForeignKey(nameof(ProvinciaId))]
        [JsonIgnore]
        public Provincia? Provincia { get; set; }
        public Guid? CantonId { get; set; }
        [ForeignKey(nameof(CantonId))]
        [JsonIgnore]
        public Canton? Canton { get; set; }
        public Guid? ParroquiaId { get; set; }
        [ForeignKey(nameof(ParroquiaId))]
        [JsonIgnore]
        public Parroquia? Parroquia { get; set; }
        public bool Activo { get; set; }
        // Listas que respaldan al candidato
        public List<ListaCandidato> ListaCandidatos { get; set; } = new List<ListaCandidato>();
    }
}
