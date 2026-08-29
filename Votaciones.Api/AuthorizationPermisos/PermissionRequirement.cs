using Microsoft.AspNetCore.Authorization;

namespace Votaciones.Api.AuthorizationPermisos
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permiso { get; }

        public PermissionRequirement(string permiso)
        {
            Permiso = permiso;
        }

    }
}
