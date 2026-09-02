using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IActaRepository
    {
        IQueryable<ActaEleccion> ObtenerQuery();
        Task<ActaEleccion?> ObtenerPorIdAsync(Guid id);
        Task<ActaEleccion?> ObtenerPorMesaAsync(Guid mesaId, TipoCandidato tipoCandidato, Guid? idActaExcluir = null);
        Task AgregarAsync(ActaEleccion acta);
    }
}
