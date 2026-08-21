using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Votaciones.Domain.Models
{
    [Table("AdmRol")]
    public class AdmRol
    {
        [Key]
        public Guid IdRol { get; set; }
        [Required]
        public string NombreRol { get; set; } = string.Empty;
        public string? DescripcionRol { get; set; }
        public string PermisosRol { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
