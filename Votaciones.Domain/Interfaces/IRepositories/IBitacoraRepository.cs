using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IBitacoraRepository
    {
        Task<List<AdmBitacora>> ObtenerPorEleccionAsync(Guid eleccionId);
        Task<List<AdmBitacora>> ObtenerPorRegistroAsync(string tabla, string idRegistro);
        Task AgregarAsync(AdmBitacora bitacora);

    }
}
