using Votaciones.Application.Helpers;
using Votaciones.Application.Utils;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Services.Votaciones
{
    public class EleccionService : IEleccionService
    {
        private readonly IEleccionRepository _eleccionRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IUnitOfWork _unitOfWork;
        //bitacora
        private string tabla = "Eleccion";

        public EleccionService(IEleccionRepository eleccionRepository, IBitacoraService bitacoraService, IUnitOfWork  unitOfWork)
        {
            _eleccionRepository = eleccionRepository;
            _bitacoraService = bitacoraService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Eleccion>> ObtenerTodosAsync()
        {
            return await _eleccionRepository.ObtenerTodosAsync();
        }

        public async Task<Eleccion?> ObtenerPorIdAsync(Guid id)
        {
            return await _eleccionRepository.ObtenerPorIdAsync(id);
        }

        public async Task<Eleccion> CrearAsync(Eleccion eleccion)
        {
            if (string.IsNullOrWhiteSpace(eleccion.NombreEleccion))
                throw new ArgumentException("El nombre de la elección es obligatorio.");

            eleccion.IdEleccion = Guid.NewGuid();
            eleccion.Estado = EstadoEleccion.Planificada;
            eleccion.FechaCreacion = Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o"));

            await _eleccionRepository.AgregarAsync(eleccion);

            #region Registrar bitacora
            var valoresNuevos = BitacoraHelper.ObtenerValores(eleccion, CamposAuditablesEleccion.Campos);
            // Bitacora
            await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, eleccion.IdEleccion.ToString(), 
                $"Elección creada '{eleccion.NombreEleccion}'", eleccion.IdEleccion, null, null, valoresNuevos);
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return eleccion;
        }

        public async Task<Eleccion> ActualizarAsync(Guid id, Eleccion eleccion)
        {
            var existente = await _eleccionRepository.ObtenerPorIdAsync(id);

            if (existente == null)
                throw new KeyNotFoundException("La elección no existe.");

            // Valores ANTES de modificar
            var valoresAnteriores = BitacoraHelper.ObtenerValores(existente, CamposAuditablesEleccion.Campos);
            // valores a modificar 
            existente.NombreEleccion = eleccion.NombreEleccion;
            existente.Descripcion = eleccion.Descripcion;
            existente.FechaEleccion = eleccion.FechaEleccion;
            existente.Estado = eleccion.Estado;

            // Valores DESPUÉS de modificar
            var valoresNuevos = BitacoraHelper.ObtenerValores(existente, CamposAuditablesEleccion.Campos);
            // Detectar solamente cambios
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);

            #region Registrar bitacora
            // Solo generar bitácora si realmente hubo cambios
            if (cambios.Nuevos.Any())
            {
                existente.FechaModificacion = Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o"));
                // Bitacora
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdEleccion.ToString(),
                $"Elección modificada '{existente.NombreEleccion}'", existente.IdEleccion, null, 
                cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return existente;
        }

    }
}
