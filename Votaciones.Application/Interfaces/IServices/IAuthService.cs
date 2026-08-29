using Votaciones.Application.DTOs.SeguridadDTO;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginDTO dto);
    }
}
