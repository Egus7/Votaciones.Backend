using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IServices
{
    public interface IMesaElectoralService
    {
        Task<PaginacionDTO<MesaElectoralDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize);
        Task<MesaElectoralDTO?> ObtenerPorIdAsync(Guid id);
        Task<MesaElectoral> CrearAsync(MesaElectoral mesaElectoral);
        Task<MesaElectoral> ActualizarAsync(Guid id, MesaElectoral mesaElectoral);
        Task<MesaElectoral> CambiarEstadoAsync(Guid id);
    }
}
