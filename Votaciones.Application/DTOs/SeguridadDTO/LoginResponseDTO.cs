
namespace Votaciones.Application.DTOs.SeguridadDTO
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiracionToken { get; set; }
        public Guid IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public Guid RolId { get; set; }
        public string NombreRol { get; set; } = string.Empty;
    }
}
