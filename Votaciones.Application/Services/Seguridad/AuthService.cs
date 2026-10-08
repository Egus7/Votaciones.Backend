using System.Text.Json;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Domain.Interfaces.IRepositories;

namespace Votaciones.Application.Services.Seguridad
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtService _jwtService;
        public AuthService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher, IJwtService jwtService)
        {
            _usuarioRepository = usuarioRepository;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Usuario))
                throw new ArgumentException("El usuario o correo es obligatorio.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("La contraseña es obligatoria.");
            //validar usuario
            var identificador = dto.Usuario.Trim();

            var usuario = await _usuarioRepository.ObtenerPorNombreUsuarioOEmailAsync(identificador);
            if (usuario == null)
                throw new UnauthorizedAccessException($"El usuario {identificador} no existe.");

            if (!usuario.Estado)
                throw new UnauthorizedAccessException("El usuario se encuentra inactivo.");
            if (usuario.Rol == null)
                throw new UnauthorizedAccessException("El usuario no tiene un rol asignado.");
            if (!usuario.Rol.Activo)
                throw new UnauthorizedAccessException("El rol del usuario se encuentra inactivo.");

            var passwordValida = _passwordHasher.Verify(dto.Password, usuario.Password);

            if (!passwordValida)
                throw new UnauthorizedAccessException("La contraseña es incorrecta.");

            var token = _jwtService.GenerarToken(usuario.IdUsuario, usuario.NombreUsuario, usuario.Rol.IdRol, 
                    usuario.Rol.NombreRol);

            return new LoginResponseDTO
            {
                Token = token,
                ExpiracionToken = _jwtService.ObtenerExpiracion(),
                IdUsuario = usuario.IdUsuario,
                NombreUsuario = dto.Usuario,
                RolId = usuario.Rol.IdRol,
                NombreRol = usuario.Rol.NombreRol
            };
        }

    }
}
