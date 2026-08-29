using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Application.Utils;

namespace Votaciones.Infrastructure.Security
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerarToken(Guid usuarioId, string nombreUsuario, Guid rolId, string nombreRol, List<string> permisos)
        {
            var jwtKey = _configuration["Jwt:Key"];
            
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("La clave JWT no está configurada.");

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var expiracionMinutos = _configuration.GetValue<int?>("Jwt:ExpirationMinutes") ?? 60;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
                new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
                new(ClaimTypes.Name, nombreUsuario),

                new("rolId", rolId.ToString()),
                new(ClaimTypes.Role, nombreRol)
            };

            foreach (var permiso in permisos.Distinct())
            {
                claims.Add(new Claim("permiso", permiso));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiracion = Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o")).AddMinutes(expiracionMinutos);

            var token = new JwtSecurityToken(issuer: issuer, audience: audience, claims: claims,
                expires: expiracion, signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public DateTime ObtenerExpiracion()
        {
            var expiracionMinutos = _configuration.GetValue<int?>("Jwt:ExpirationMinutes") ?? 60;

            return Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o")).AddMinutes(expiracionMinutos);
        }

    }
}
