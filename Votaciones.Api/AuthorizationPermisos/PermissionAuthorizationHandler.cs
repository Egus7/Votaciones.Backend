using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;
using Votaciones.Domain.Interfaces.IRepositories;

namespace Votaciones.Api.AuthorizationPermisos
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IRolRepository _rolRepository;

        public PermissionAuthorizationHandler(IRolRepository rolRepository)
        {
            _rolRepository = rolRepository;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // Obtener RolId desde el JWT
            var rolIdClaim = context.User.FindFirst("rolId")?.Value;

            if (string.IsNullOrWhiteSpace(rolIdClaim))
                return;

            if (!Guid.TryParse(rolIdClaim, out var rolId))
                return;

            // Obtener el rol desde BD
            var rol = await _rolRepository.ObtenerPorIdAsync(rolId);
            if (rol == null)
                return;

            if (string.IsNullOrWhiteSpace(rol.PermisosRol))
            {
                context.Fail(new AuthorizationFailureReason(this, $"El rol {rol.NombreRol} no tiene permisos configurados."));
                return;
            }

            // Convertir JSON a lista de permisos
            var permisos = JsonSerializer.Deserialize<List<string>>(rol.PermisosRol);
            if (permisos == null)
                return;

            // Verificar si tiene el permiso solicitado
            if (permisos.Contains(requirement.Permiso, StringComparer.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
            }

        }
    }
}
