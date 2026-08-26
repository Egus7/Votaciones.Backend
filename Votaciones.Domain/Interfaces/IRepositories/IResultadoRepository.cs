using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IResultadoRepository
    {
        IQueryable<ActaEleccion> ObtenerActasQuery();

        IQueryable<MesaElectoral> ObtenerMesasQuery();
    }
}
