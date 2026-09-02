using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface ICandidatoRepository
    {
        IQueryable<Candidato> ObtenerQuery();
        Task<Candidato?> ObtenerPorIdAsync(Guid id);
        Task<bool> ExistePorEleccionListaTipoAsync(Guid eleccionId, Guid listaId, TipoCandidato tipoCandidato);
        Task AgregarAsync(Candidato candidato);       
    }
}
