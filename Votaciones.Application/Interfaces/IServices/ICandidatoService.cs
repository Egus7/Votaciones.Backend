using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IServices
{
    public interface ICandidatoService
    {
        Task<PaginacionDTO<CandidatoDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize);
        Task<CandidatoDTO?> ObtenerPorIdAsync(Guid id);
        Task<Candidato> CrearAsync(Candidato candidato);
        Task<Candidato> ActualizarAsync(Guid id, Candidato candidato);
        Task<Candidato> CambiarEstadoAsync(Guid id);
    }
}
