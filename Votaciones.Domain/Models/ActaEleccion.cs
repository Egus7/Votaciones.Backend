using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static Votaciones.Domain.Enums.EnumsEleccion;


namespace Votaciones.Domain.Models
{
    [Table("ActaEleccion")]
    public class ActaEleccion
    {
        [Key]
        public Guid IdActa { get; set; }
        public Guid EleccionId { get; set; }

        [ForeignKey(nameof(EleccionId))]
        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }

        public Guid MesaElectoralId { get; set; }
        [ForeignKey(nameof(MesaElectoralId))]
        [JsonIgnore]
        public MesaElectoral? MesaElectoral { get; set; }

        [Required]
        public DateTime FechaRegistro { get; set; }
        public Guid UsuarioRegistroId { get; set; }
        [Required]
        public int VotosBlancos { get; set; }
        [Required]
        public int VotosNulos { get; set; }
        [Required]
        public int TotalVotos { get; set; }
        [Required]
        public EstadoActa Estado { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public Guid? UsuarioModificacionId { get; set; }
        //detalle
        public List<ActaDetalle> ActaDetalles { get; set; } = new();
    }
}
