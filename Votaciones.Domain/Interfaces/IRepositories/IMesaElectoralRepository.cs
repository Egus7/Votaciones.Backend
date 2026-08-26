using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IMesaElectoralRepository
    {
        IQueryable<MesaElectoral> ObtenerQuery();
        Task<MesaElectoral?> ObtenerPorIdAsync(Guid id);
        Task<MesaElectoral?> ObtenerPorCodigoMesaByEleccionAsync(string codigoMesa, Guid eleccionId, Guid zonaId, Guid? idMesaExcluir = null);
        Task<Zona?> ObtenerZonaAsync(Guid zonaId);
        Task AgregarAsync(MesaElectoral mesaElectoral);
    }
}
