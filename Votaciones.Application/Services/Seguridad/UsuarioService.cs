using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Application.Helpers;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Utils;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;

namespace Votaciones.Application.Services.Seguridad
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IRolRepository _rolRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ICurrentService _currentService;
        //bitacora
        private string tabla = "AdmUsuario";

        public UsuarioService(IUsuarioRepository usuarioRepository, IRolRepository rolRepository, IBitacoraService bitacoraService, 
                IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, IMapper mapper, ICurrentService currentService)
        {
            _usuarioRepository = usuarioRepository;
            _rolRepository = rolRepository;
            _bitacoraService = bitacoraService;
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _currentService = currentService;
        }

        public async Task<PaginacionDTO<UsuarioDTO>> ObtenerPaginacionAsync(int pagina, int pageSize, string? buscar = null)
        {
            var query = _usuarioRepository.ObtenerQuery();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var busqueda = buscar.Trim();

                query = query.Where(x => x.NombreUsuario.Contains(buscar) ||
                    (x.EmailUsuario != null && x.EmailUsuario.Contains(buscar)));
            }

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.NombreUsuario)
                .ProjectTo<UsuarioDTO>(_mapper.ConfigurationProvider)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginacionDTO<UsuarioDTO>
            {
                Items = items,
                PageActual = pagina,
                PageSize = pageSize,
                TotalRegistros = totalRegistros,
                TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize)
            };
        }

        public async Task<UsuarioDTO?> ObtenerPorIdAsync(Guid id)
        {
            return await _usuarioRepository.ObtenerQuery().Where(x => x.IdUsuario == id)
                .ProjectTo<UsuarioDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<AdmUsuario> CrearAsync(AdmUsuario usuario)
        {
            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario!))
                throw new ArgumentException("El nombre de usuario es obligatorio.");
            if (string.IsNullOrWhiteSpace(usuario.EmailUsuario!))
                throw new ArgumentException("El email es obligatorio.");
            if (string.IsNullOrWhiteSpace(usuario!.Password!))
                throw new ArgumentException("La contraseña es obligatoria.");
            //existe nombreUsuario
            var existeNombreUsuario = await _usuarioRepository.ExisteNombreUsuarioAsync(usuario.NombreUsuario.Trim());
            if (existeNombreUsuario)
                throw new InvalidOperationException("El nombre de usuario ingresado ya existe.");
            // existe emailUsuario 
            var existeEmailUsuario = await _usuarioRepository.ExisteEmailUsuarioAsync(usuario.EmailUsuario);
            if (existeEmailUsuario)
                throw new InvalidOperationException("El email ingresado ya existe.");
            // verificar Rol
            var rol = await _rolRepository.ObtenerPorIdAsync(usuario.RolId);
            if (rol == null)
                throw new KeyNotFoundException("El rol especificado no existe.");
            // rol activo
            if (!rol.Activo)
                throw new InvalidOperationException("El rol seleccionado está inactivo.");

            usuario.IdUsuario = Guid.NewGuid();
            usuario.Password = _passwordHasher.Hash(usuario.Password);
            usuario.Estado = true;
            usuario.FechaCreacion = Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o"));

            await _usuarioRepository.AgregarAsync(usuario);

            #region Registrar bitacora
            //valores nuevos
            var valoresNuevos = ObtenerValoresAuditoria(usuario, rol);

            await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, usuario.IdUsuario.ToString(),
                $"Usuario creado '{usuario.NombreUsuario}'.", null, _currentService.UsuarioId, null, valoresNuevos);
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return usuario;
        }

        public async Task<AdmUsuario> ActualizarAsync(Guid id, AdmUsuario usuario)
        {
            var existente = await _usuarioRepository.ObtenerPorIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException("El usuario no existe.");

            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
                throw new ArgumentException("El nombre de usuario es obligatorio.");
            if (string.IsNullOrWhiteSpace(usuario.EmailUsuario))
                throw new ArgumentException("El email es obligatorio.");
            // verificar nombreUsuario
            var existeNombreUsuario = await _usuarioRepository.ExisteNombreUsuarioAsync(usuario.NombreUsuario.Trim(), id);
            if (existeNombreUsuario)
                throw new InvalidOperationException("El nombre de usuario ingresado ya existe.");
            // verificar emailUsuario
            var existeEmailUsuario = await _usuarioRepository.ExisteEmailUsuarioAsync(usuario.EmailUsuario, id);
            if (existeEmailUsuario)
                throw new InvalidOperationException("El email ingresado ya existe.");

            var rol = await _rolRepository.ObtenerPorIdAsync(usuario.RolId);
            if (rol == null)
                throw new KeyNotFoundException("El rol especificado no existe.");
            // rol activo
            if (!rol.Activo)
                throw new InvalidOperationException("El rol seleccionado está inactivo.");

            var valoresAnteriores = ObtenerValoresAuditoria(existente, rol);
            // actualizar propiedades
            existente.NombreUsuario = usuario.NombreUsuario.Trim();
            existente.EmailUsuario = usuario.EmailUsuario?.Trim();
            existente.RolId = usuario.RolId;

            #region Registrar bitácora
            var valoresNuevos = ObtenerValoresAuditoria(existente, rol);
            //obtener solo cambios  
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);
            //Registrar bitacora solo si hay cambios
            if (cambios.Nuevos.Any())
            {
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdUsuario.ToString(),
                $"Usuario modificado '{existente.NombreUsuario}'", null, _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return existente;
        }

        public async Task<AdmUsuario> CambiarEstadoAsync(Guid id)
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);
            if (usuario == null)
                throw new KeyNotFoundException("El usuario no existe.");

            // valores anteriores
            var valoresAntes = BitacoraHelper.ObtenerValores(usuario, CamposAuditablesUsuario.Campos);
            // cambiar estado
            usuario.Estado = !usuario.Estado;

            #region Registrar bitacora
            // valores nuevos
            var valoresNuevos = BitacoraHelper.ObtenerValores(usuario, CamposAuditablesUsuario.Campos);
            // obtener cambios
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAntes, valoresNuevos);
            // Si no hubo cambios, no actualiza ni genera auditoría.
            if (cambios.Anteriores.Count > 0 || cambios.Nuevos.Count > 0)
            {
                //bitacora
                string descripcion = $"Usuario modificado {usuario.NombreUsuario} el estado a '{(usuario.Estado ? "Activo" : "Inactivo")}'";
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, usuario.IdUsuario.ToString(), descripcion, null,
                    _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return usuario;
        }

        #region Metodos privados
        private Dictionary<string, object?> ObtenerValoresAuditoria(AdmUsuario ususario, AdmRol rol)
        {
            var valores = BitacoraHelper.ObtenerValores(ususario, CamposAuditablesUsuario.Campos);

            BitacoraHelper.AgregarRelacion(valores, "Rol", rol.NombreRol);

            return valores;
        }

        #endregion

    }
}
