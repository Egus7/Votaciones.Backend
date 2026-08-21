using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.Helpers;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;

namespace Votaciones.Application.Services.Votaciones
{
    public class MesaElectoralService : IMesaElectoralService
    {
        private readonly IMesaElectoralRepository _mesaElectoralRepository;
        private readonly IEleccionRepository _eleccionRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IUnitOfWork _unitOfWork;
        //bitacora
        private string tabla = "MesaElectoral";

        public MesaElectoralService(IMesaElectoralRepository mesaElectoralRepository, IEleccionRepository eleccionRepository, 
            IBitacoraService bitacoraService, IUnitOfWork unitOfWork)
        {
            _mesaElectoralRepository = mesaElectoralRepository;
            _eleccionRepository = eleccionRepository;
            _bitacoraService = bitacoraService;
            _unitOfWork = unitOfWork;
        }

        public async Task<PaginacionDTO<MesaElectoral>> ObtenerPaginacionAsync(int pagina, int pageSize)
        {
            var (items, totalRegistros) = await _mesaElectoralRepository.ObtenerPaginacionAsync(pagina, pageSize);

            // armado para la Api
            return new PaginacionDTO<MesaElectoral>
            {
                Items = items,
                PageActual = pagina,
                PageSize = pageSize,
                TotalRegistros = totalRegistros,
                TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize)
            };
        }

        public async Task<MesaElectoral?> ObtenerPorIdAsync(Guid id)
        {
            return await _mesaElectoralRepository.ObtenerPorIdAsync(id);
        }

        public async Task<IEnumerable<MesaElectoral>> ObtenerPorEleccionAsync(Guid eleccionId)
        {
            return await _mesaElectoralRepository.ObtenerPorEleccionAsync(eleccionId);
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
            // verificar que no exista el mismo codigo de mesa para una eleccion
            var codigoMesa = await _mesaElectoralRepository.ObtenerPorCodigoMesaByEleccionAsync(mesaElectoral.CodigoMesa, mesaElectoral.EleccionId);
            if (codigoMesa != null)
                throw new ArgumentException("El código mesa ingresado ya existe para esta elección electoral");

            mesaElectoral.IdMesaElectoral = Guid.NewGuid();
            mesaElectoral.Activa = true;

            await _mesaElectoralRepository.AgregarAsync(mesaElectoral);

            #region Registrar bitacora
            //valores nuevos
            var valoresNuevos = ObtenerValoresAuditoria(mesaElectoral, eleccion);

            await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, mesaElectoral.IdMesaElectoral.ToString(),
                $"Mesa electoral creada '{mesaElectoral.CodigoMesa}'", mesaElectoral.EleccionId, null, null, valoresNuevos);
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

            // Validar que el código no exista en OTRA mesa de la misma elección (Excluyendo la actual)
            var existeCodigoEnOtraMesa = await _mesaElectoralRepository.ObtenerPorCodigoMesaByEleccionAsync(
                mesaElectoral.CodigoMesa, mesaElectoral.EleccionId, id); // Excluimos el id de la mesa actual
            // validar
            if (existeCodigoEnOtraMesa != null)
                throw new ArgumentException("El código de mesa ingresado ya existe en esta elección electoral.");

            #region Capturar valores anteriores para bitácora
            var valoresAnteriores = ObtenerValoresAuditoria(mesaExistente, eleccion);
            #endregion

            // actualizar propiedades en la entidad
            mesaExistente.CodigoMesa = mesaElectoral.CodigoMesa;
            mesaExistente.EleccionId = mesaElectoral.EleccionId;
            mesaExistente.TipoMesa = mesaElectoral.TipoMesa;
            mesaExistente.Descripcion = mesaElectoral.Descripcion;

            #region Registrar bitácora
            var valoresNuevos = ObtenerValoresAuditoria(mesaExistente, eleccion);
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
        private Dictionary<string, object?> ObtenerValoresAuditoria(MesaElectoral mesaElectoral, Eleccion eleccion)
        {
            var valores = BitacoraHelper.ObtenerValores(mesaElectoral, CamposAuditablesMesaElectoral.Campos);

            BitacoraHelper.AgregarRelacion(valores, "Eleccion", eleccion.NombreEleccion);

            return valores;
        }

        #endregion

    }
}
