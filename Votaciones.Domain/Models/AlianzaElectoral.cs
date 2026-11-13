using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("AlianzaElectoral")]
    public class AlianzaElectoral
    {
        [Key]
        public Guid IdAlianzaElectoral { get; set; }

        [Required]
        [MaxLength(200)]
        public string NombreAlianza { get; set; } = string.Empty;
        public Guid EleccionId { get; set; }

        [ForeignKey(nameof(EleccionId))]
        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }

        // Ámbito territorial
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
        // Listas que pertenecen a la alianza
        public List<AlianzaDetalle> AlianzaDetalles { get; set; } = new List<AlianzaDetalle>();
    }
}
