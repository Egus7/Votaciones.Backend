using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IServices
{
    public interface IEleccionService
    {
        Task<IEnumerable<Eleccion>> ObtenerTodosAsync();
        Task<Eleccion?> ObtenerPorIdAsync(Guid id);
        Task<Eleccion> CrearAsync(Eleccion eleccion);
        Task<Eleccion> ActualizarAsync(Guid id, Eleccion eleccion);
    }
}
