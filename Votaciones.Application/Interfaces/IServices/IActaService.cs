using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IActaService
    {
        Task<PaginacionDTO<ActaDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize, Guid? provinciaId = null, Guid? cantonId = null,
            Guid? parroquiaId = null, Guid? zonaId = null, Guid? mesaId = null, EstadoActa? estadoActa = null, TipoCandidato? tipoCandidato = null);
        Task<ActaDTO?> ObtenerPorIdAsync(Guid id);
        Task<List<ActaDetalleDTO>> ObtenerCandidatoListaPorTipoZonaAsync(Guid eleccionId, TipoCandidato tipoCandidato, Guid? provinciaId = null,
            Guid? cantonId = null, Guid? parroquiaId = null);   
        Task<ActaDTO?> ObtenerPorMesaAsync(Guid mesaId);
        Task<ActaEleccion> CrearAsync(ActaEleccion acta);
        Task<ActaEleccion> ActualizarAsync(Guid id, ActaEleccion acta);
        Task<ActaEleccion> CambiarEstadoAsync(Guid id, EstadoActa nuevoEstado);
    }
}
