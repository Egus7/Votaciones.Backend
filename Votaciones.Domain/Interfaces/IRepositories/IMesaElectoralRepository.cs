using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IMesaElectoralRepository
    {
        Task<(IEnumerable<MesaElectoral> Items, int TotalRegistros)> ObtenerPaginacionAsync(int pagina, int pageSize);
        Task<MesaElectoral?> ObtenerPorIdAsync(Guid id);
        Task<MesaElectoral?> ObtenerPorCodigoMesaByEleccionAsync(string codigoMesa, Guid eleccionId, Guid? idMesaExcluir = null);
        Task<IEnumerable<MesaElectoral>> ObtenerPorEleccionAsync(Guid eleccionId);
        Task AgregarAsync(MesaElectoral mesaElectoral);
    }
}
