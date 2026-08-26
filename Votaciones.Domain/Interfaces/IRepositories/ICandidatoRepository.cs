using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface ICandidatoRepository
    {
        IQueryable<Candidato> ObtenerQuery();
        Task<Candidato?> ObtenerPorIdAsync(Guid id);
        Task AgregarAsync(Candidato candidato);       
    }
}
