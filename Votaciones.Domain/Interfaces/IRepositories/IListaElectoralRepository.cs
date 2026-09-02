using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IListaElectoralRepository
    {
        IQueryable<ListaElectoral> ObtenerQuery();
        Task<ListaElectoral?> ObtenerPorIdAsync(Guid id);
        Task<ListaElectoral?> ObtenerNumeroListaAsync(int nroLista, Guid? idListaExcluir = null);
        Task AgregarAsync(ListaElectoral listaElectoral);
    }
}
