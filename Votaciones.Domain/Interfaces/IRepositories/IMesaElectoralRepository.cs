using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IMesaElectoralRepository
    {
        IQueryable<MesaElectoral> ObtenerQuery();
        Task<MesaElectoral?> ObtenerPorIdAsync(Guid id);
        Task<int> ObtenerSiguienteNumeroMesaAsync(Guid eleccionId, Guid zonaId, TipoMesa tipoMesa);
        Task<MesaElectoral?> ObtenerPorCodigoMesaByEleccionAsync(string codigoMesa, Guid eleccionId, Guid zonaId, Guid? idMesaExcluir = null);
        Task<Zona?> ObtenerZonaAsync(Guid zonaId);
        Task AgregarAsync(MesaElectoral mesaElectoral);
    }
}
