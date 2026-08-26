using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IActaRepository
    {
        IQueryable<ActaEleccion> ObtenerQuery();
        Task<ActaEleccion?> ObtenerPorIdAsync(Guid id);
        Task<ActaEleccion?> ObtenerPorMesaAsync(Guid mesaId);
        Task AgregarAsync(ActaEleccion acta);
    }
}
