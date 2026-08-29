using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Helpers;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Utils;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Services.Votaciones
{
    public class ActaService : IActaService
    {
        private readonly IActaRepository _actaRepository;
        private readonly IMesaElectoralRepository _mesaElectoralRepository;
        private readonly IEleccionRepository _eleccionRepository;
        private readonly ICandidatoRepository _candidatoRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentService _currentService;
        //bitacora
        private string tabla = "ActaEleccion";

        public ActaService (IActaRepository actaRepository, IMesaElectoralRepository mesaElectoralRepository, IEleccionRepository eleccionRepository,
                ICandidatoRepository candidatoRepository, IBitacoraService bitacoraService, IMapper mapper, IUnitOfWork unitOfWork, ICurrentService currentService)
        {
            _actaRepository = actaRepository;
            _mesaElectoralRepository = mesaElectoralRepository;
            _eleccionRepository = eleccionRepository;
            _candidatoRepository = candidatoRepository;
            _bitacoraService = bitacoraService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _currentService = currentService;
        }

        public async Task<PaginacionDTO<ActaDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize)
        {
            var query = _actaRepository.ObtenerQuery().Where(x => x.EleccionId == eleccionId);

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.FechaRegistro)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<ActaDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            // armado para la Api
            return new PaginacionDTO<ActaDTO>
            {
                Items = items,
                PageActual = pagina,
                PageSize = pageSize,
                TotalRegistros = totalRegistros,
                TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize)
            };
        }

        public async Task<ActaDTO?> ObtenerPorIdAsync(Guid id)
        {
            var query = _actaRepository.ObtenerQuery();

            return await query.Where(x => x.IdActa == id)
                .ProjectTo<ActaDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<ActaDTO?> ObtenerPorMesaAsync(Guid mesaId)
        {
            var query = _actaRepository.ObtenerQuery();

            return await query.Where(x => x.MesaElectoralId == mesaId)
                .ProjectTo<ActaDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<ActaEleccion> CrearAsync(ActaEleccion acta)
        {
            if (acta.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");
            // verificar que la Elección exista
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(acta.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe");
            // validarEstadoEleccion
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");
            // validarMesa
            if (acta.MesaElectoralId == Guid.Empty)
                throw new ArgumentException("La mesa electoral es obligatorio.");
            // verificar que la mesa exista
            var mesaElec = await _mesaElectoralRepository.ObtenerPorIdAsync(acta.MesaElectoralId);
            if (mesaElec == null)
                throw new ArgumentException("La mesa electoral ingresada no existe");

            if (mesaElec.EleccionId != acta.EleccionId)
                throw new ArgumentException("La mesa electoral no pertenece a la elección.");

            if (!mesaElec.Activa)
                throw new ArgumentException("La mesa ya está cerrada.");
            // verificar que no tenga acta existente
            var actaExistente = await _actaRepository.ObtenerPorMesaAsync(acta.MesaElectoralId);
            if (actaExistente != null)
                throw new ArgumentException($"La mesa electoral '{mesaElec.CodigoMesa}', ya tiene un acta registrada.");
            // Validar votos
            if (acta.VotosBlancos < 0)
                throw new ArgumentException("Los votos blancos no pueden ser negativos.");
            if (acta.VotosNulos < 0)
                throw new ArgumentException("Los votos nulos no pueden ser negativos.");

            if (acta.ActaDetalles == null || !acta.ActaDetalles.Any())
                throw new ArgumentException("El acta debe tener al menos un candidato.");
            // Verificar candidatos repetidos
            if (acta.ActaDetalles.GroupBy(x => x.CandidatoId).Any(x => x.Count() > 1))
            {
                throw new ArgumentException("No puede existir el mismo candidato más de una vez en la misma acta.");
            }

            // crearActa
            acta.IdActa = Guid.NewGuid();
            acta.FechaRegistro = Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o"));
            acta.UsuarioRegistroId = _currentService.UsuarioId ?? Guid.Empty;
            acta.Estado = EstadoActa.Registrada;
            acta.FechaModificacion = null;
            acta.UsuarioModificacionId = null;
            //Crear detalles
            foreach (var detalleDto in acta.ActaDetalles)
            {
                if (detalleDto.Votos < 0)
                    throw new ArgumentException("Los votos no pueden ser negativos.");

                var candidato = await _candidatoRepository.ObtenerPorIdAsync(detalleDto.CandidatoId);
                if (candidato == null)
                    throw new KeyNotFoundException($"El candidato {candidato?.NombreCandidato} no existe.");

                if (!candidato.Activo)
                    throw new InvalidOperationException($"El candidato '{candidato.NombreCandidato}' está inactivo.");

                if (candidato.EleccionId != acta.EleccionId)
                    throw new InvalidOperationException($"El candidato '{candidato.NombreCandidato}' no pertenece a esta elección.");

                detalleDto.IdActaDetalle = Guid.NewGuid();
                detalleDto.ActaId = acta.IdActa;
            }
            // Calcular total
            acta.TotalVotos = acta.ActaDetalles.Sum(x => x.Votos) + acta.VotosBlancos + acta.VotosNulos;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _actaRepository.AgregarAsync(acta);

                #region Registrar bitacora
                // Bitácora
                var valoresNuevos = ObtenerValoresAuditoria(acta, eleccion, mesaElec);

                await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, acta.IdActa.ToString(), 
                    $"Acta registrada para la mesa '{mesaElec.CodigoMesa}'.", acta.EleccionId, _currentService.UsuarioId, null, valoresNuevos);
                #endregion

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return acta;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ActaEleccion> ActualizarAsync(Guid id, ActaEleccion acta)
        {
            var existente = await _actaRepository.ObtenerPorIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException("El acta no existe.");

            if (existente.Estado != EstadoActa.Registrada && existente.Estado != EstadoActa.ConInconsistencia)
            {
                throw new InvalidOperationException($"El acta no puede ser modificada en su estado actual {existente.Estado}.");
            }

            #region Validar Acta
            // validar Acta
            if (acta.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");
            // verificar que la Elección exista
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(acta.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe");
            // validarEstadoEleccion
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");
            // validarMesa
            if (acta.MesaElectoralId == Guid.Empty)
                throw new ArgumentException("La mesa electoral es obligatorio.");
            // verificar que la mesa exista
            var mesaElec = await _mesaElectoralRepository.ObtenerPorIdAsync(acta.MesaElectoralId);
            if (mesaElec == null)
                throw new ArgumentException("La mesa electoral ingresada no existe");

            if (mesaElec.EleccionId != acta.EleccionId)
                throw new ArgumentException("La mesa electoral no pertenece a la elección.");
            if (!mesaElec.Activa)
                throw new ArgumentException("La mesa ya está cerrada.");
            
            // Validar votos
            if (acta.VotosBlancos < 0)
                throw new ArgumentException("Los votos blancos no pueden ser negativos.");
            if (acta.VotosNulos < 0)
                throw new ArgumentException("Los votos nulos no pueden ser negativos.");

            if (acta.ActaDetalles == null || !acta.ActaDetalles.Any())
                throw new ArgumentException("El acta debe tener al menos un candidato.");
            // Verificar candidatos repetidos
            if (acta.ActaDetalles.GroupBy(x => x.CandidatoId).Any(x => x.Count() > 1))
            {
                throw new ArgumentException("No puede existir el mismo candidato más de una vez en la misma acta.");
            }
            #endregion

            //Valores anteriores
            var valoresAnteriores = ObtenerValoresAuditoria(existente, eleccion, mesaElec);
            // Actualizar cabecera
            existente.VotosBlancos = acta.VotosBlancos;
            existente.VotosNulos = acta.VotosNulos;

            // Actualizar detalles
            foreach (var detalleDto in acta.ActaDetalles)
            {
                if (detalleDto.Votos < 0)
                    throw new ArgumentException("Los votos no pueden ser negativos.");

                var candidato = await _candidatoRepository.ObtenerPorIdAsync(detalleDto.CandidatoId);
                if (candidato == null)
                    throw new KeyNotFoundException($"El candidato {candidato?.NombreCandidato} no existe.");
                if (!candidato.Activo)
                    throw new InvalidOperationException($"El candidato '{candidato.NombreCandidato}' está inactivo.");
                if (candidato.EleccionId != acta.EleccionId)
                    throw new InvalidOperationException($"El candidato '{candidato.NombreCandidato}' no pertenece a esta elección.");

                var detalle = existente.ActaDetalles.FirstOrDefault(x => x.CandidatoId == candidato.IdCandidato);
                if (detalle == null)
                    throw new KeyNotFoundException($"El candidato '{candidato.NombreCandidato}' no pertenece al acta.");

                detalle.Votos = detalleDto.Votos;
            }

            // Recalcular total
            existente.TotalVotos = existente.ActaDetalles.Sum(x => x.Votos) + existente.VotosBlancos + existente.VotosNulos;
            //existente.UsuarioModificacionId = usuarioId;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                //valores nuevos 
                var valoresNuevos = ObtenerValoresAuditoria(existente, eleccion, mesaElec);
                //cambios
                var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);
                if (cambios.Nuevos.Any())
                {
                    existente.UsuarioModificacionId = _currentService.UsuarioId;
                    existente.FechaModificacion = Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o"));
                    // Bitácora
                    await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, acta.IdActa.ToString(),
                        $"Acta modificada para la mesa '{mesaElec.CodigoMesa}'.", acta.EleccionId, _currentService.UsuarioId, 
                        cambios.Anteriores, cambios.Nuevos);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return acta;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<ActaEleccion> CambiarEstadoAsync(Guid id, EstadoActa nuevoEstado)
        {
            var acta = await _actaRepository.ObtenerPorIdAsync(id);
            if (acta == null)
                throw new KeyNotFoundException("El acta no existe.");

            #region Validar cambio estado
            var estadoAnterior = acta.Estado;
            // No permitir cambiar al mismo estado
            if (estadoAnterior == nuevoEstado)
                throw new InvalidOperationException($"El acta ya se encuentra en estado '{nuevoEstado}'.");
            // Validar transición
            ValidarCambioEstado(estadoAnterior, nuevoEstado);
            // validar Eleccion
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(acta.EleccionId);
            if (eleccion == null)
                throw new KeyNotFoundException("La elección del acta no existe.");
            // validar Mesa Electoral
            var mesa = await _mesaElectoralRepository.ObtenerPorIdAsync(acta.MesaElectoralId);
            if (mesa == null)
                throw new KeyNotFoundException("La mesa electoral del acta no existe.");
            #endregion

            // Valores anteriores
            var valoresAnteriores = ObtenerValoresAuditoria(acta, eleccion, mesa);
            // Cambiar estado
            acta.Estado = nuevoEstado;
            acta.FechaModificacion = Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o"));
            
            // Si se valida, cerrar mesa
            if (nuevoEstado == EstadoActa.Validada)
                mesa.Activa = false;
            // Si se anula, NO cerrar mesa
            if (nuevoEstado == EstadoActa.Anulada)
                mesa.Activa = true;

            // Valores nuevos
            var valoresNuevos = ObtenerValoresAuditoria(acta, eleccion, mesa);
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, acta.IdActa.ToString(), 
                    $"Estado del acta modificado a '{nuevoEstado}'.", acta.EleccionId, _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return acta;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        #region Metodos privados
        private Dictionary<string, object?> ObtenerValoresAuditoria(ActaEleccion acta, Eleccion eleccion, MesaElectoral mesa)
        {
            var valores = BitacoraHelper.ObtenerValores(acta, CamposAuditablesActa.Campos);
            BitacoraHelper.AgregarRelacion(valores, "Eleccion", eleccion.NombreEleccion);
            BitacoraHelper.AgregarRelacion(valores, "Mesa", mesa.CodigoMesa);
            //Obtener zona
            var zona = _mesaElectoralRepository.ObtenerZonaAsync(mesa.ZonaId);
            BitacoraHelper.AgregarRelacion(valores, "Zona", zona.Result!.NombreZona);
            // Detalles del acta
            valores["Detalles"] = acta.ActaDetalles
                .Select(d => new { Candidato = d.Candidato?.NombreCandidato, Votos = d.Votos })
                .ToList();

            return valores;
        }

        private void ValidarCambioEstado(EstadoActa estadoActual, EstadoActa nuevoEstado)
        {
            var permitido = estadoActual switch
            {
                EstadoActa.Registrada =>
                    nuevoEstado == EstadoActa.EnRevision || nuevoEstado == EstadoActa.Anulada,

                EstadoActa.EnRevision =>
                    nuevoEstado == EstadoActa.ConInconsistencia ||
                    nuevoEstado == EstadoActa.Validada || nuevoEstado == EstadoActa.Anulada,

                EstadoActa.ConInconsistencia =>
                    nuevoEstado == EstadoActa.Registrada ||
                    nuevoEstado == EstadoActa.EnRevision || nuevoEstado == EstadoActa.Anulada,

                EstadoActa.Validada => false,
                EstadoActa.Anulada => false,

                _ => false
            };

            if (!permitido)
            {
                throw new InvalidOperationException($"No se permite cambiar el acta de " + 
                    $"'{estadoActual}' a '{nuevoEstado}'.");
            }
        }

        #endregion

    }
}
