using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface ICandidatoRepository
    {
        IQueryable<Candidato> ObtenerQuery();
        Task<Candidato?> ObtenerPorIdAsync(Guid id);
        Task<bool> ExistePorEleccionListaTipoAsync(Guid eleccionId, Guid listaId, TipoCandidato tipoCandidato, Guid? excluirCandidatoId = null);
        Task<bool> ExisteOrdenPorEleccionListaTipoAsync(Guid eleccionId, Guid listaId, TipoCandidato tipoCandidato, 
                    int orden, Guid? excluirCandidatoId = null);
        Task<bool> ExisteListaPrincipalPorEleccionTipoAsync(Guid eleccionId, Guid listaElectoralId, TipoCandidato tipoCandidato);
        Task AgregarAsync(Candidato candidato);
        Task AgregarListaCandidatoAsync(ListaCandidato listaCandidato);
        void EliminarListaCandidatos(IEnumerable<ListaCandidato> listaCandidatos);
    }
}
