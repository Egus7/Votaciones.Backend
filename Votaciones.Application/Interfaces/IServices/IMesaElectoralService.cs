using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IServices
{
    public interface IMesaElectoralService
    {
        Task<PaginacionDTO<MesaElectoral>> ObtenerPaginacionAsync(int pagina, int PageSize);
        Task<MesaElectoral?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<MesaElectoral>> ObtenerPorEleccionAsync(Guid eleccionId);
        Task<MesaElectoral> CrearAsync(MesaElectoral mesaElectoral);
        Task<MesaElectoral> ActualizarAsync(Guid id, MesaElectoral mesaElectoral);
        Task<MesaElectoral> CambiarEstadoAsync(Guid id);
    }
}
