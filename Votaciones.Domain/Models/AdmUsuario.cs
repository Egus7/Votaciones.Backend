using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("AdmUsuario")]
    public class AdmUsuario
    {
        [Key]
        public Guid IdUsuario { get; set; }

        [Required]
        [MaxLength(400)]
        public string NombreUsuario { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string? EmailUsuario { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty;

        public Guid RolId { get; set; }
        [ForeignKey(nameof(RolId))]
        [JsonIgnore]
        public AdmRol? Rol {  get; set; }

        [Required]
        public bool Estado { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
