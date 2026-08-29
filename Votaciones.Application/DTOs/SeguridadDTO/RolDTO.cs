
namespace Votaciones.Application.DTOs.SeguridadDTO
{
    public class RolDTO
    {
        public Guid IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string? DescripcionRol { get; set; }
        public List<string> Permisos { get; set; } = [];
        public bool Activo { get; set; }
    }
}
