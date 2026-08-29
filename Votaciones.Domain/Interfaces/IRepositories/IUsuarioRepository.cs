using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IUsuarioRepository
    {
        IQueryable<AdmUsuario> ObtenerQuery();
        Task<AdmUsuario?> ObtenerPorIdAsync(Guid id);
        Task<AdmUsuario?> ObtenerPorNombreUsuarioOEmailAsync(string identificador);
        Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, Guid? idExcluir = null);
        Task<bool> ExisteEmailUsuarioAsync(string emailUsuario, Guid? idExcluir = null);
        Task AgregarAsync(AdmUsuario usuario);
    }
}
