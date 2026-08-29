
namespace Votaciones.Application.DTOs.SeguridadDTO
{
    public class UsuarioDTO
    {
        public Guid IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string? EmailUsuario { get; set; }
        public Guid RolId { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
