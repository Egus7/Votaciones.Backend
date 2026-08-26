using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IActaService
    {
        Task<PaginacionDTO<ActaDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize);
        Task<ActaDTO?> ObtenerPorIdAsync(Guid id);
        Task<ActaDTO?> ObtenerPorMesaAsync(Guid mesaId);
        Task<ActaEleccion> CrearAsync(ActaEleccion acta);
        Task<ActaEleccion> ActualizarAsync(Guid id, ActaEleccion acta);
        Task<ActaEleccion> CambiarEstadoAsync(Guid id, EstadoActa nuevoEstado);
    }
}
