using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Helpers;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Services.Votaciones
{
    public class MesaElectoralService : IMesaElectoralService
    {
        private readonly IMesaElectoralRepository _mesaElectoralRepository;
        private readonly IEleccionRepository _eleccionRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        //bitacora
        private string tabla = "MesaElectoral";

        public MesaElectoralService(IMesaElectoralRepository mesaElectoralRepository, IEleccionRepository eleccionRepository, 
            IBitacoraService bitacoraService, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mesaElectoralRepository = mesaElectoralRepository;
            _eleccionRepository = eleccionRepository;
            _bitacoraService = bitacoraService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<PaginacionDTO<MesaElectoralDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize)
        {
            var query = _mesaElectoralRepository.ObtenerQuery().Where(x => x.EleccionId == eleccionId);

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Zona!.NombreZona)
                .ThenBy(x => x.CodigoMesa)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<MesaElectoralDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            // armado para la Api
            return new PaginacionDTO<MesaElectoralDTO>
            {
                Items = items,
                PageActual = pagina,
                PageSize = pageSize,
                TotalRegistros = totalRegistros,
                TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize)
            };
        }

        public async Task<MesaElectoralDTO?> ObtenerPorIdAsync(Guid id)
        {
            var query = _mesaElectoralRepository.ObtenerQuery();

            return await query.Where(x => x.IdMesaElectoral == id)
                .ProjectTo<MesaElectoralDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<MesaElectoral> CrearAsync(MesaElectoral mesaElectoral)
        {
            if (string.IsNullOrWhiteSpace(mesaElectoral.CodigoMesa))
                throw new ArgumentException("El código de la mesa es obligatorio.");

            if (mesaElectoral.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");
            // verificar que la Elección exista
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(mesaElectoral.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe");
            //validar estadoEleccion
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");
            //verificar que exista zona
            var zona = await _mesaElectoralRepository.ObtenerZonaAsync(mesaElectoral.ZonaId);
            if (zona == null)
                throw new ArgumentException("La zona ingresada no existe");
            // verificar que no exista el mismo codigo de mesa para una eleccion
            var codigoMesa = await _mesaElectoralRepository.ObtenerPorCodigoMesaByEleccionAsync(mesaElectoral.CodigoMesa, mesaElectoral.EleccionId, mesaElectoral!.ZonaId);
            if (codigoMesa != null)
                throw new ArgumentException("El código mesa ingresado ya existe para esta elección electoral");

            mesaElectoral.IdMesaElectoral = Guid.NewGuid();
            mesaElectoral.Activa = true;

            await _mesaElectoralRepository.AgregarAsync(mesaElectoral);

            #region Registrar bitacora
            //valores nuevos
            var valoresNuevos = ObtenerValoresAuditoria(mesaElectoral, eleccion, zona);

            await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, mesaElectoral.IdMesaElectoral.ToString(),
                $"Mesa electoral creada '{mesaElectoral.CodigoMesa}' - Zona: {zona.NombreZona}.", 
                mesaElectoral.EleccionId, null, null, valoresNuevos);
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return mesaElectoral;
        }

        public async Task<MesaElectoral> ActualizarAsync(Guid id, MesaElectoral mesaElectoral)
        {
            // Validaciones básicas de entrada
            if (id == Guid.Empty)
                throw new ArgumentException("El id de la mesa es obligatorio.");

            if (string.IsNullOrWhiteSpace(mesaElectoral.CodigoMesa))
                throw new ArgumentException("El código de la mesa es obligatorio.");

            if (mesaElectoral.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");

            // Verificar que la mesa a editar realmente exista
            var mesaExistente = await _mesaElectoralRepository.ObtenerPorIdAsync(id);
            if (mesaExistente == null)
                throw new ArgumentException("La mesa electoral a actualizar no existe.");

            // Verificar que la Elección exista
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(mesaElectoral.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe.");
            // validarEstadoEleccion
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");
            //verificar que exista zona
            var zona = await _mesaElectoralRepository.ObtenerZonaAsync(mesaElectoral.ZonaId);
            if (zona == null)
                throw new ArgumentException("La zona ingresada no existe");
            // Validar que el código no exista en OTRA mesa de la misma elección (Excluyendo la actual)
            var existeCodigoEnOtraMesa = await _mesaElectoralRepository.ObtenerPorCodigoMesaByEleccionAsync(
                mesaElectoral.CodigoMesa, mesaElectoral.EleccionId, mesaElectoral.ZonaId, id); // Excluimos el id de la mesa actual
            // validar
            if (existeCodigoEnOtraMesa != null)
                throw new ArgumentException("El código de mesa ingresado ya existe en esta elección electoral.");

            #region Capturar valores anteriores para bitácora
            var valoresAnteriores = ObtenerValoresAuditoria(mesaExistente, eleccion, zona);
            #endregion

            // actualizar propiedades en la entidad
            mesaExistente.CodigoMesa = mesaElectoral.CodigoMesa;
            mesaExistente.EleccionId = mesaElectoral.EleccionId;
            mesaExistente.TipoMesa = mesaElectoral.TipoMesa;
            mesaExistente.Descripcion = mesaElectoral.Descripcion;

            #region Registrar bitácora
            var valoresNuevos = ObtenerValoresAuditoria(mesaExistente, eleccion, zona);
            //obtener solo cambios  
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);
            //Registrar bitacora solo si hay cambios
            if (cambios.Nuevos.Any())
            {
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, mesaExistente.IdMesaElectoral.ToString(),
                $"Mesa electoral modificada '{mesaExistente.CodigoMesa}'", mesaExistente.EleccionId, null, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return mesaExistente;
        }

        public async Task<MesaElectoral> CambiarEstadoAsync(Guid id)
        {
            var existente = await _mesaElectoralRepository.ObtenerPorIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException("La mesa electoral no existe.");

            // valores antiguos
            var valoresAntes = BitacoraHelper.ObtenerValores(existente, CamposAuditablesMesaElectoral.Campos);
            // cambiarEstado
            existente.Activa = !existente.Activa;

            #region Registrar bitacora
            // valores nuevos
            var valoresNuevos = BitacoraHelper.ObtenerValores(existente, CamposAuditablesMesaElectoral.Campos);
            // obtener cambios
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAntes, valoresNuevos);
            // Si no hubo cambios, no actualiza ni genera auditoría.
            if (cambios.Anteriores.Count > 0 || cambios.Nuevos.Count > 0)
            {
                //bitacora
                string descripcion = $"Mesa electoral {existente.CodigoMesa} modificado estado a '{(existente.Activa ? "Activo" : "Inactivo")}'";
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdMesaElectoral.ToString(), descripcion,
                    existente.EleccionId, null, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return existente;
        }

        #region Metodos privados
        private Dictionary<string, object?> ObtenerValoresAuditoria(MesaElectoral mesaElectoral, Eleccion eleccion, Zona zona)
        {
            var valores = BitacoraHelper.ObtenerValores(mesaElectoral, CamposAuditablesMesaElectoral.Campos);

            BitacoraHelper.AgregarRelacion(valores, "Eleccion", eleccion.NombreEleccion);
            BitacoraHelper.AgregarRelacion(valores, "Zona", zona.NombreZona);

            return valores;
        }

        #endregion

    }
}
