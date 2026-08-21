using Votaciones.Application.Helpers;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;

namespace Votaciones.Application.Services.Votaciones
{
    public class CandidatoService : ICandidatoService
    {
        private readonly IEleccionRepository _eleccionRepository;
        private readonly ICandidatoRepository _candidatoRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IUnitOfWork _unitOfWork;
        //bitacora
        private string tabla = "Candidato";

        public CandidatoService(IEleccionRepository eleccionRepository, ICandidatoRepository candidatoRepository, 
            IBitacoraService bitacoraService, IUnitOfWork unitOfWork)
        {
            _eleccionRepository = eleccionRepository;
            _candidatoRepository = candidatoRepository;
            _bitacoraService = bitacoraService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Candidato>> ObtenerTodosAsync()
        {
            return await _candidatoRepository.ObtenerTodosAsync();
        }
        public async Task<Candidato?> ObtenerPorIdAsync(Guid id)
        {
            return await _candidatoRepository.ObtenerPorIdAsync(id);
        }
        public async Task<IEnumerable<Candidato>> ObtenerPorEleccionAsync(Guid eleccionId)
        {
            return await _candidatoRepository.ObtenerPorEleccionAsync(eleccionId);
        }

        public async Task<Candidato> CrearAsync(Candidato candidato)
        {
            if (string.IsNullOrWhiteSpace(candidato.NombreCandidato))
                throw new ArgumentException("El nombre del candidato es obligatorio.");

            if (candidato.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");
            // verificar que la Elección exista
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(candidato.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe");
            // Lista debe ser mayor a 0
            if (candidato.NumeroLista <= 0)
                throw new ArgumentException("El número de lista debe ser mayor a 0.");

            candidato.IdCandidato = Guid.NewGuid();
            candidato.Activo = true;

            await _candidatoRepository.AgregarAsync(candidato);

            #region Registrar bitacora
            //valores nuevos
            var valoresNuevos = ObtenerValoresAuditoria(candidato, eleccion);

            await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, candidato.IdCandidato.ToString(),
                $"Candidato creado '{candidato.NombreCandidato}'", candidato.EleccionId, null, null, valoresNuevos);
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return candidato;
        }

        public async Task<Candidato> ActualizarAsync(Guid id, Candidato candidato)
        { 
            var existente = await _candidatoRepository.ObtenerPorIdAsync(id);

            if (existente == null)
                throw new KeyNotFoundException("El candidato no existe.");

            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(candidato.EleccionId);
            if (eleccion == null)
                throw new KeyNotFoundException("La elección del candidato no existe.");

            //valores antiguos
            var valoresAnteriores = ObtenerValoresAuditoria(existente, eleccion);
            // actualizar campos
            existente.NombreCandidato = candidato.NombreCandidato;
            existente.NumeroLista = candidato.NumeroLista;
            existente.Lista = candidato.Lista;
            existente.Activo = candidato.Activo;

            //valores nuevos
            var valoresNuevos = ObtenerValoresAuditoria(existente, eleccion);
            //obtener solo cambios  
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);

            #region Registrar bitacora
            //Registrar bitacora solo si hay cambios
            if (cambios.Nuevos.Any())
            {
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdCandidato.ToString(),
                    $"Candidato modificado '{existente.NombreCandidato}'", existente.EleccionId, null, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion
            // Guardar cambios
            await _unitOfWork.SaveChangesAsync();

            return existente;
        }

        public async Task<Candidato> CambiarEstadoAsync(Guid id)
        {
            var existente = await _candidatoRepository.ObtenerPorIdAsync(id);

            if (existente == null)
                throw new KeyNotFoundException("El candidato no existe.");

            // valores antiguos
            var valoresAntes = BitacoraHelper.ObtenerValores(existente, CamposAuditablesCandidato.Campos);
            // cambiarEstado
            existente.Activo = !existente.Activo;

            #region Registrar bitacora
            // valores nuevos
            var valoresNuevos = BitacoraHelper.ObtenerValores(existente, CamposAuditablesCandidato.Campos);        
            // obtener cambios
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAntes, valoresNuevos);
            // Si no hubo cambios, no actualiza ni genera auditoría.
            if (cambios.Anteriores.Count > 0 || cambios.Nuevos.Count > 0)
            {
                //bitacora
                string descripcion = $"Candidato {existente.NombreCandidato} modificado estado a '{(existente.Activo ? "Activo" : "Inactivo")}'";
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdCandidato.ToString(), descripcion,
                    existente.EleccionId, null, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return existente;
        }

        #region Metodos privados
        private Dictionary<string, object?> ObtenerValoresAuditoria(Candidato candidato, Eleccion eleccion)
        {
            var valores = BitacoraHelper.ObtenerValores(candidato, CamposAuditablesCandidato.Campos);

            BitacoraHelper.AgregarRelacion(valores, "Eleccion", eleccion.NombreEleccion);

            return valores;
        }

        #endregion

    }
}
