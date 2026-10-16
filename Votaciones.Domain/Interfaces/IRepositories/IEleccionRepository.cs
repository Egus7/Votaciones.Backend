using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IEleccionRepository
    {
        IQueryable<Eleccion> ObtenerQuery();
        Task<Eleccion?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Eleccion>> ObtenerTodosAsync();
        Task AgregarAsync(Eleccion eleccion);
    }
}
