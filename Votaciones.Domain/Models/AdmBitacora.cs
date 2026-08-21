using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Votaciones.Domain.Models
{
    [Table("AdmBitacora")]
    public class AdmBitacora
    {
        [Key]
        public Guid IdBitacora { get; set; }

        public Guid? EleccionId { get; set; }
        [ForeignKey(nameof(EleccionId))]
        [JsonIgnore]
        public Eleccion? Eleccion { get; set; }

        public Guid? UsuarioId { get; set; }
        [ForeignKey(nameof(UsuarioId))]
        [JsonIgnore]
        public AdmUsuario? Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string Tabla { get; set; } = string.Empty;
        public string? IdRegistro { get; set; }
        public string? Descripcion { get; set; }
        // Deben contener JSON válido.
        public string? ValoresAnteriores { get; set; }
        public string? ValoresNuevos { get; set; }
    }
}
