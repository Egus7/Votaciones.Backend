using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Domain.Models;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IRolService
    {
        Task<PaginacionDTO<RolDTO>> ObtenerPaginacionAsync(int pagina, int pageSize, string? buscar = null);
        Task<RolDTO?> ObtenerPorIdAsync(Guid id);
        Task<RolDTO> CrearAsync(RolDTO dto);
        Task<RolDTO> ActualizarAsync(Guid id, RolDTO dto);
        Task<AdmRol> CambiarEstadoAsync(Guid id);
    }
}
