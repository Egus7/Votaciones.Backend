using Votaciones.Application.DTOs.VotacionesDTO;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IResultadoService
    {
        Task<ResultadoEleccionDTO> ObtenerPorEleccionAsync(Guid eleccionId, TipoCandidato tipoCandidato, TipoResultado tipoResultado);
        Task<ResultadoEleccionDTO> ObtenerPorProvinciaAsync(Guid eleccionId, Guid provinciaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado);
        Task<ResultadoEleccionDTO> ObtenerPorCantonAsync(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato, TipoResultado tipoResultado);
        Task<ResultadoEleccionDTO> ObtenerPorParroquiaAsync(Guid eleccionId, Guid parroquiaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado);
        Task<ResultadoEleccionDTO> ObtenerPorZonaAsync(Guid eleccionId, Guid zonaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado);
        Task<ResultadoEleccionDTO> ObtenerPorMesaAsync(Guid eleccionId, Guid mesaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado);
        Task<AsignacionConcejalesDTO> ObtenerAsignacionConcejalesAsync(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato, TipoResultado tipoResultado);
        Task<byte[]> ObtenerAsignacionConcejalesExcelAsync(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato, TipoResultado tipoResultado);
    }
}
