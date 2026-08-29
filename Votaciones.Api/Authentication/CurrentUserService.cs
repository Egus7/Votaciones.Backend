using System.Security.Claims;
using Votaciones.Application.Interfaces.ISecurity;

namespace Votaciones.Api.Authentication
{
    public class CurrentUserService : ICurrentService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? UsuarioId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                return Guid.TryParse(value, out var id) ? id : null;
            }
        }

    }
}
