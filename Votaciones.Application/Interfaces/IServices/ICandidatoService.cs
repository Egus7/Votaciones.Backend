using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface ICandidatoService
    {
        Task<PaginacionDTO<CandidatoDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize, string? busqueda = null,
            TipoCandidato? tipoCandidato = null, Guid? listaElectoralId = null);
        Task<CandidatoDTO?> ObtenerPorIdAsync(Guid id);
        Task<List<AlianzaElectoralDTO>> ObtenerAlianzasDisponiblesAsync(Guid eleccionId, Guid? provinciaId, Guid? cantonId, Guid? parroquiaId, List<Guid> listasElectorales);
        Task<Candidato> CrearAsync(Candidato candidato, string? nombreAlianza = null);
        Task<List<Candidato>> CrearVariosAsync(List<Candidato> candidatos, string? nombreAlianza = null);
        Task<Candidato> ActualizarAsync(Guid id, Candidato candidato, string? nombreAlianza = null);
        Task<Candidato> CambiarEstadoAsync(Guid id);
    }
}
