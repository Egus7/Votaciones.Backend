using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IServices
{
    public interface ICandidatoService
    {
        Task<IEnumerable<Candidato>> ObtenerTodosAsync();
        Task<Candidato?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Candidato>> ObtenerPorEleccionAsync(Guid eleccionId);
        Task<Candidato> CrearAsync(Candidato candidato);
        Task<Candidato> ActualizarAsync(Guid id, Candidato candidato);
        Task<Candidato> CambiarEstadoAsync(Guid id);
    }
}
