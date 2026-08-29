using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IRolRepository
    {
        IQueryable<AdmRol> ObtenerQuery();
        Task<AdmRol?> ObtenerPorIdAsync(Guid id);
        Task<AdmRol?> ObtenerPorNombreAsync(string nombreRol);
        Task<bool> ExisteNombreAsync(string nombreRol, Guid? idExcluir = null);
        Task AgregarAsync(AdmRol rol);
    }
}
