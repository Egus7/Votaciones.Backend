using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Domain.Models;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IListaElectoralService
    {
        Task<PaginacionDTO<ListaElectoral>> ObtenerPaginacionAsync(int pagina, int pageSize, string? buscar = null);
        Task<ListaElectoral?> ObtenerPorIdAsync(Guid id);
        Task<ListaElectoral> CrearAsync(ListaElectoral listaElectoral);
        Task<ListaElectoral> ActualizarAsync(Guid id, ListaElectoral listaElectoral);
        Task<ListaElectoral> CambiarEstadoAsync(Guid id);
    }
}
