using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IServices
{
    public interface IBitacoraService
    {
        Task<List<AdmBitacora>> ObtenerPorEleccionAsync(Guid eleccionId);
        Task<List<AdmBitacora>> ObtenerPorRegistroAsync(string tabla, string idRegistro);
        Task RegistrarBitacoraAsync(string accion, string tabla, string idRegistro, string descripcion, 
            Guid? eleccionId = null, Guid? usuarioId = null, object? valoresAnteriores = null, object? valoresNuevos = null);

    }
}
