using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IBitacoraRepository
    {
        IQueryable<AdmBitacora> ObtenerQuery();
        Task<List<AdmBitacora>> ObtenerPorRegistroAsync(string tabla, string idRegistro);
        Task AgregarAsync(AdmBitacora bitacora);

    }
}
