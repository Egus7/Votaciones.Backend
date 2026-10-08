using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Domain.Models
{
    [Table("MesaElectoral")]
    public class MesaElectoral
    {
        [Key]
        public Guid IdMesaElectoral { get; set; }

        public Guid EleccionId { get; set; }
        [ForeignKey(nameof(EleccionId))]
        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }
        [Required]
        public Guid ZonaId { get; set; }
        [ForeignKey(nameof(ZonaId))]
        [JsonIgnore]
        public Zona? Zona { get; set; }

        [MaxLength(20)]
        public string CodigoMesa { get; set; } = string.Empty;
        [Required]
        public TipoMesa TipoMesa { get; set; }
        [MaxLength(200)]
        public string? Descripcion { get; set; }
        [Required]
        public bool Activa { get; set; }
    }
}
