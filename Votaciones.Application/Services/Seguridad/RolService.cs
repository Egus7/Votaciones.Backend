using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Application.Helpers;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Security;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;

namespace Votaciones.Application.Services.Seguridad
{
    public class RolService : IRolService
    {
        private readonly IRolRepository _rolRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentService _currentService;
        //bitacora
        private string tabla = "AdmRol";

        public RolService(IRolRepository rolRepository, IBitacoraService bitacoraService, IUnitOfWork unitOfWork, IMapper mapper, 
                ICurrentService currentService)
        {
            _rolRepository = rolRepository;
            _bitacoraService = bitacoraService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentService = currentService;
        }

        public async Task<PaginacionDTO<RolDTO>> ObtenerPaginacionAsync(int pagina, int pageSize, string? buscar = null)
        {
            var query = _rolRepository.ObtenerQuery();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var busqueda = buscar.Trim();

                query = query.Where(x => x.NombreRol.Contains(busqueda));
            }

            var totalRegistros = await query.CountAsync();

            var roles = await query
                .OrderBy(x => x.NombreRol)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = roles.Select(MapearRolDTO).ToList();

            return new PaginacionDTO<RolDTO>
            {
                Items = items,
                PageActual = pagina,
                PageSize = pageSize,
                TotalRegistros = totalRegistros,
                TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize)
            };
        }

        public async Task<RolDTO?> ObtenerPorIdAsync(Guid id)
        {
            var rol = await _rolRepository.ObtenerQuery().FirstOrDefaultAsync(x => x.IdRol == id);

            if (rol == null)
                throw new ArgumentException("El rol no existe.");

            return MapearRolDTO(rol);
        }

        public async Task<RolDTO> CrearAsync(RolDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreRol))
                throw new ArgumentException("El nombre del rol es obligatorio.");

            var nombreRol = dto.NombreRol.Trim();
            var existe = await _rolRepository.ExisteNombreAsync(nombreRol);
            if (existe)
                throw new InvalidOperationException("El nombre del rol ya existe.");

            //validar permisos
            ValidarPermisos(dto.Permisos);

            var rol = new AdmRol
            {
                IdRol = Guid.NewGuid(),
                NombreRol = nombreRol,
                DescripcionRol = dto.DescripcionRol?.Trim(),
                PermisosRol = JsonSerializer.Serialize(dto.Permisos),
                Activo = true
            };

            await _rolRepository.AgregarAsync(rol);

            #region Registrar bitacora
            //valores nuevos
            var valoresNuevos = ObtenerValoresAuditoria(rol);

            await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, rol.IdRol.ToString(),
                $"Rol creado '{rol.NombreRol}'.", null, _currentService.UsuarioId, null, valoresNuevos);
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return MapearRolDTO(rol);
        }

        public async Task<RolDTO> ActualizarAsync(Guid id, RolDTO dto)
        {
            var rolExistente = await _rolRepository.ObtenerPorIdAsync(id);
            if (rolExistente == null)
                throw new KeyNotFoundException("El rol no existe.");

            if (string.IsNullOrWhiteSpace(dto.NombreRol))
                throw new ArgumentException("El nombre del rol es obligatorio.");

            var nombreRol = dto.NombreRol.Trim();
            var existe = await _rolRepository.ExisteNombreAsync(nombreRol, id);
            if (existe)
                throw new InvalidOperationException("El nombre del rol ya existe.");
            //validar permisos
            ValidarPermisos(dto.Permisos);

            // valores anteriores
            var valoresAnteriores = ObtenerValoresAuditoria(rolExistente);
            // actualizar propiedas
            rolExistente.NombreRol = nombreRol;
            rolExistente.DescripcionRol = dto.DescripcionRol?.Trim();
            rolExistente.PermisosRol = JsonSerializer.Serialize(dto.Permisos);

            #region Registrar bitácora
            var valoresNuevos = ObtenerValoresAuditoria(rolExistente);
            //obtener solo cambios  
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);
            //Registrar bitacora solo si hay cambios
            if (cambios.Nuevos.Any())
            {
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, rolExistente.IdRol.ToString(),
                $"Rol modificado '{rolExistente.NombreRol}'", null, _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return MapearRolDTO(rolExistente);
        }

        public async Task<AdmRol> CambiarEstadoAsync(Guid id)
        {
            var rol = await _rolRepository.ObtenerPorIdAsync(id);
            if (rol == null)
                throw new KeyNotFoundException("El rol no existe.");

            // valores anteriores
            var valoresAntes = BitacoraHelper.ObtenerValores(rol, CamposAuditablesRol.Campos);
            // cambiar estado
            rol.Activo = !rol.Activo;

            #region Registrar bitacora
            // valores nuevos
            var valoresNuevos = BitacoraHelper.ObtenerValores(rol, CamposAuditablesRol.Campos);
            // obtener cambios
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAntes, valoresNuevos);
            // Si no hubo cambios, no actualiza ni genera auditoría.
            if (cambios.Anteriores.Count > 0 || cambios.Nuevos.Count > 0)
            {
                //bitacora
                string descripcion = $"Rol modificado {rol.NombreRol} el estado a '{(rol.Activo ? "Activo" : "Inactivo")}'";
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, rol.IdRol.ToString(), descripcion, null,
                   _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return rol;
        }


        #region Metodos privados
        private RolDTO MapearRolDTO(AdmRol rol)
        {
            var dto = _mapper.Map<RolDTO>(rol);
                
            dto.Permisos = string.IsNullOrWhiteSpace(rol.PermisosRol) ? [] 
            : JsonSerializer.Deserialize<List<string>>(rol.PermisosRol) ?? [];

            return dto;
        }

        private static void ValidarPermisos(List<string> permisos)
        {
            var permisosValidos = RolPermisos.ObtenerTodos().ToHashSet(StringComparer.OrdinalIgnoreCase);

            var permisosInvalidos = permisos.Where(x => !permisosValidos.Contains(x)).ToList();

            if (permisosInvalidos.Any())
            {
                throw new ArgumentException($"Los siguientes permisos no son válidos: " +
                        $"{string.Join(", ", permisosInvalidos)}");
            }
        }

        private Dictionary<string, object?> ObtenerValoresAuditoria(AdmRol rol)
        {
            var valores = BitacoraHelper.ObtenerValores(rol, CamposAuditablesRol.Campos);

            return valores;
        }

        #endregion



    }
}
