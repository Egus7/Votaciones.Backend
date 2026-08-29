using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Votaciones.Api.AuthorizationPermisos
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Crear automáticamente una policy usando el nombre del permiso
            var policy = new AuthorizationPolicyBuilder().AddRequirements(new PermissionRequirement(policyName)).Build();

            return await Task.FromResult(policy);
        }

    }
}
