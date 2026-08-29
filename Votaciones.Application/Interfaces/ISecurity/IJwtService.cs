
namespace Votaciones.Application.Interfaces.ISecurity
{
    public interface IJwtService
    {
        string GenerarToken(Guid usuarioId, string nombreUsuario, Guid rolId, string nombreRol, List<string> permisos);
        DateTime ObtenerExpiracion();
    }
}
