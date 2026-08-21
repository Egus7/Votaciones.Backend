using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface ICandidatoRepository
    {
        Task<IEnumerable<Candidato>> ObtenerTodosAsync();
        Task<Candidato?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Candidato>> ObtenerPorEleccionAsync(Guid eleccionId);
        Task AgregarAsync(Candidato candidato);       
    }
}
