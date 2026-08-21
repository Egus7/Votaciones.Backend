using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Domain.Models
{
    [Table("Eleccion")]
    public class Eleccion
    {
        [Key]
        public Guid IdEleccion { get; set; }

        [Required]
        [MaxLength(200)]
        public string NombreEleccion { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Descripcion { get; set; }
        [Required]
        public DateTime FechaEleccion { get; set; }
        public EstadoEleccion Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
    }
}
