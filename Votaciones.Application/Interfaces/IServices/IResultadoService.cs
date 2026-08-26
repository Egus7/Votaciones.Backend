using Votaciones.Application.DTOs.VotacionesDTO;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IResultadoService
    {
        Task<ResultadoEleccionDTO> ObtenerPorEleccionAsync(Guid eleccionId);
        Task<ResultadoEleccionDTO> ObtenerPorCantonAsync(Guid eleccionId, Guid cantonId);
        Task<ResultadoEleccionDTO> ObtenerPorParroquiaAsync(Guid eleccionId, Guid parroquiaId);
        Task<ResultadoEleccionDTO> ObtenerPorZonaAsync(Guid eleccionId, Guid zonaId);
    }
}
