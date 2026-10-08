using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Domain.Models;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IUsuarioService
    {
        Task<PaginacionDTO<UsuarioDTO>> ObtenerPaginacionAsync(int pagina, int pageSize, string? buscar = null);
        Task<UsuarioDTO?> ObtenerPorIdAsync(Guid id);
        Task<List<string>> ObtenerPermisosUsuarioAsync();
        Task<AdmUsuario> CrearAsync(AdmUsuario usuario);
        Task<AdmUsuario> ActualizarAsync(Guid id, AdmUsuario usuario);
        Task<AdmUsuario> CambiarEstadoAsync(Guid id);
    }
}
