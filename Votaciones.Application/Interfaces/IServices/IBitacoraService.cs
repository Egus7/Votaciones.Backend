using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IBitacoraService
    {
        Task<PaginacionDTO<BitacoraDTO>> ObtenerPaginacionAsync(int pagina, int pageSize, DateTime? fechaDesde = null,
            DateTime? fechaHasta = null, Guid? usuarioId = null, string? accion = null, string? tabla = null);
        Task<BitacoraDTO?> ObtenerPorIdAsync(Guid bitacoraId);
        Task<List<BitacoraDTO>> ObtenerPorRegistroAsync(string tabla, string idRegistro);
        Task RegistrarBitacoraAsync(string accion, string tabla, string idRegistro, string descripcion, 
            Guid? eleccionId = null, Guid? usuarioId = null, object? valoresAnteriores = null, object? valoresNuevos = null);

    }
}
