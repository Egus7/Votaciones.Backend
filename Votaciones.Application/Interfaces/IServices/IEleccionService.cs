using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IEleccionService
    {
        Task<PaginacionDTO<Eleccion>> ObtenerPaginacionAsync(int pagina, int pageSize, string? busqueda = null, DateTime? fechaInicio = null, 
            DateTime? fechaFin = null, TipoEleccion? tipoEleccion = null);
        Task<IEnumerable<Eleccion>> ObtenerTodosAsync();
        Task<Eleccion?> ObtenerPorIdAsync(Guid id);
        Task<Eleccion> CrearAsync(Eleccion eleccion);
        Task<Eleccion> ActualizarAsync(Guid id, Eleccion eleccion);
    }
}
