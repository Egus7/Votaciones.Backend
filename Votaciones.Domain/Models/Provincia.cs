using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Votaciones.Domain.Models
{
    [Table("Provincia")]
    public class Provincia
    {
        [Key]
        public Guid IdProvincia { get; set; }
        
        [Required]
        [MaxLength(2)]
        public string CodigoProvincia { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string NombreProvincia { get; set; } = string.Empty;

        [Required]
        public bool Activo { get; set; }
    }
}
