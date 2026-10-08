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
        private readonly IListaElectoralRepository _listaElectoralRepository;
        private readonly ICandidatoRepository _candidatoRepository;
        private readonly IZonaRepository _zonaRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentService _currentService;
        //bitacora
        private string tabla = "ActaEleccion";

        public ActaService (IActaRepository actaRepository, IMesaElectoralRepository mesaElectoralRepository, IEleccionRepository eleccionRepository,
                IListaElectoralRepository listaElectoralRepository, ICandidatoRepository candidatoRepository, IZonaRepository zonaRepository, 
                IBitacoraService bitacoraService, IMapper mapper, IUnitOfWork unitOfWork, ICurrentService currentService)
        {
            _actaRepository = actaRepository;
            _mesaElectoralRepository = mesaElectoralRepository;
            _eleccionRepository = eleccionRepository;
            _listaElectoralRepository = listaElectoralRepository;
            _candidatoRepository = candidatoRepository;
            _zonaRepository = zonaRepository;
            _bitacoraService = bitacoraService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _currentService = currentService;
        }

        public async Task<PaginacionDTO<ActaDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize, Guid? provinciaId = null, 
            Guid? cantonId = null, Guid? parroquiaId = null, Guid? zonaId = null, Guid? mesaId = null, EstadoActa? estadoActa = null, TipoCandidato? tipoCandidato = null)
        {
            var query = _actaRepository.ObtenerQuery().Where(x => x.EleccionId == eleccionId);

            if (provinciaId.HasValue && provinciaId != Guid.Empty)
            {
                query = query.Where(x => x.MesaElectoral!.Zona!.Parroquia!.Canton!.ProvinciaId == provinciaId);
            }
            if (cantonId.HasValue && cantonId != Guid.Empty)
            {
                query = query.Where(x => x.MesaElectoral!.Zona!.Parroquia!.CantonId == cantonId);
            }
            if (parroquiaId.HasValue && parroquiaId != Guid.Empty)
            {
                query = query.Where(x => x.MesaElectoral!.Zona!.ParroquiaId == parroquiaId);
            }
            if (zonaId.HasValue && zonaId != Guid.Empty)
            {
                query = query.Where(x => x.MesaElectoral!.ZonaId == zonaId);
            }
            if (mesaId.HasValue && mesaId != Guid.Empty)
            {
                query = query.Where(x => x.MesaElectoralId == mesaId);
            }

            if (estadoActa.HasValue)
            {
                query = query.Where(x => x.Estado == estadoActa);
            }

            if (tipoCandidato.HasValue)
            {
                query = query.Where(x => x.TipoCandidato == tipoCandidato);
            }

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.MesaElectoral!.Zona!.NombreZona)
                .ThenBy(x => x.MesaElectoral!.CodigoMesa)
                .ThenBy(x => x.TipoCandidato)
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

            var acta = await query.Where(x => x.IdActa == id)
                .ProjectTo<ActaDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (acta == null)
                return null;

            acta.ActasDetalle = acta.ActasDetalle
                .OrderBy(x => x.NumeroLista)
                .ThenBy(x => x.NombreCandidato)
                .ToList();

            return acta;
        }

        public async Task<List<ActaDetalleDTO>> ObtenerCandidatoListaPorTipoZonaAsync(Guid eleccionId, TipoCandidato tipoCandidato, Guid? provinciaId = null,
            Guid? cantonId = null, Guid? parroquiaId = null)
        {
            var query = _candidatoRepository.ObtenerQuery()
                .Where(x => x.EleccionId == eleccionId && x.TipoCandidato == tipoCandidato);

            switch (tipoCandidato)
            {
                // Sin ámbito territorial
                case TipoCandidato.Presidente:
                    break;
                // Solo provincia
                case TipoCandidato.Prefecto:
                    if (provinciaId.HasValue)
                        query = query.Where(x => x.ProvinciaId == provinciaId);

                    break;
                // Provincia + cantón
                case TipoCandidato.Alcalde:
                case TipoCandidato.ConcejalUrbano:
                    if (provinciaId.HasValue)
                        query = query.Where(x => x.ProvinciaId == provinciaId);
                    if (cantonId.HasValue)
                        query = query.Where(x => x.CantonId == cantonId);

                    break;
                // Provincia + cantón + parroquia
                case TipoCandidato.ConcejalRural:
                    if (provinciaId.HasValue)
                        query = query.Where(x => x.ProvinciaId == provinciaId);
                    if (cantonId.HasValue)
                        query = query.Where(x => x.CantonId == cantonId);
                    if (parroquiaId.HasValue)
                        query = query.Where(x => x.ParroquiaId == parroquiaId);

                    break;
            }

            if (tipoCandidato == TipoCandidato.ConcejalUrbano || tipoCandidato == TipoCandidato.ConcejalRural)
            {
                var detalles = await query.SelectMany(c => c.ListaCandidatos.Where(c => c.ListaPrincipal)
                .Select(l => new ActaDetalleDTO
                {
                    CandidatoId = null,
                    NombreCandidato = null,
                    ListaElectoralId = l.ListaElectoralId,
                    NombreLista = l.ListaElectoral!.NombreLista,
                    NumeroLista = l.ListaElectoral.NumeroLista,
                    Votos = 0
                })).ToListAsync();

                return detalles.GroupBy(x => x.ListaElectoralId).Select(x => x.First())
                    .OrderBy(x => x.NumeroLista).ToList();
            }
            else
            {
                return await query.SelectMany(c => c.ListaCandidatos.Where(c => c.ListaPrincipal)
                .Select(l => new ActaDetalleDTO
                {
                    CandidatoId = c.IdCandidato,
                    NombreCandidato = c.NombreCandidato,
                    ListaElectoralId = l.ListaElectoralId,
                    NombreLista = l.ListaElectoral!.NombreLista,
                    NumeroLista = l.ListaElectoral.NumeroLista,
                    Votos = 0
                }))
                .OrderBy(x => x.NumeroLista)
                .ThenBy(x => x.NombreCandidato)
                .ToListAsync();
            }
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

            // validar tipo de candidato
            if (!Enum.IsDefined(typeof(TipoCandidato), acta.TipoCandidato))
                throw new ArgumentException("El tipo de candidato no es válido.");
            // verificar que no tenga acta existente
            var actaExistente = await _actaRepository.ObtenerPorMesaAsync(acta.MesaElectoralId, acta.TipoCandidato);
            if (actaExistente != null)
                throw new ArgumentException($"La mesa electoral '{mesaElec.CodigoMesa}-{mesaElec.Zona!.NombreZona}', " +
                        $"ya tiene un acta registrada para {acta.TipoCandidato}.");
            // Validar votos
            if (acta.VotosBlancos < 0)
                throw new ArgumentException("Los votos blancos no pueden ser negativos.");
            if (acta.VotosNulos < 0)
                throw new ArgumentException("Los votos nulos no pueden ser negativos.");

            if (acta.ActaDetalles == null || !acta.ActaDetalles.Any())
                throw new ArgumentException("El acta debe tener al menos un candidato o lista electoral.");
            // Verificar candidatos o listas repetidas
            if (acta.TipoCandidato == TipoCandidato.ConcejalUrbano || acta.TipoCandidato == TipoCandidato.ConcejalRural)
            {
                if (acta.ActaDetalles.GroupBy(x => x.ListaElectoralId).Any(x => x.Count() > 1))
                    throw new ArgumentException("No puede existir la misma lista electoral más de una vez en la misma acta.");
            }
            else
            {
                if (acta.ActaDetalles.GroupBy(x => x.CandidatoId).Any(x => x.Count() > 1))
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

                detalleDto.IdActaDetalle = Guid.NewGuid();
                detalleDto.ActaId = acta.IdActa;

                // CONCEJALES
                if (acta.TipoCandidato == TipoCandidato.ConcejalUrbano || acta.TipoCandidato == TipoCandidato.ConcejalRural)
                {
                    //verificar listaElectoral
                    if (detalleDto.ListaElectoralId == Guid.Empty)
                        throw new ArgumentException("La lista electoral es obligatoria.");
                    var listaElectoral = await _listaElectoralRepository.ObtenerPorIdAsync(detalleDto.ListaElectoralId);
                    if (listaElectoral == null)
                        throw new KeyNotFoundException($"La lista electoral no existe.");
                    if (!listaElectoral.Activo)
                        throw new InvalidOperationException($"La lista electoral '{listaElectoral.NombreLista}' está suspendida.");               
                    // Verificar que la lista participe en esta elección
                    var listaParticipa = await _candidatoRepository.ExistePorEleccionListaTipoAsync(acta.EleccionId, 
                            detalleDto.ListaElectoralId, acta.TipoCandidato);
                    if (!listaParticipa)
                        throw new InvalidOperationException($"La lista electoral '{listaElectoral.NombreLista}' no tiene candidatos " +
                            $"registrados para {acta.TipoCandidato} en esta elección.");

                    // la lista principal es la que se guarda en el detalle
                    var listasPrincipales = await _candidatoRepository.ExisteListaPrincipalPorEleccionTipoAsync(acta.EleccionId,
                            detalleDto.ListaElectoralId, acta.TipoCandidato);
                    if (!listasPrincipales)
                        throw new InvalidOperationException($"La lista electoral '{listaElectoral.NombreLista}' no es la lista principal " + 
                                $"de una candidatura de {acta.TipoCandidato} en esta elección.");

                    detalleDto.CandidatoId = null;
                } 
                else
                {
                    if (detalleDto.CandidatoId == null || detalleDto.CandidatoId == Guid.Empty)
                        throw new ArgumentException("El candidato es obligatorio.");
                    
                    var candidato = await _candidatoRepository.ObtenerPorIdAsync(detalleDto.CandidatoId.Value);
                    if (candidato == null)
                        throw new KeyNotFoundException($"El candidato no existe.");
                    if (!candidato.Activo)
                        throw new InvalidOperationException($"El candidato '{candidato.NombreCandidato}' está inactivo.");

                    if (candidato.EleccionId != acta.EleccionId)
                        throw new InvalidOperationException($"El candidato '{candidato.NombreCandidato}' no pertenece a esta elección.");
                    // validar que el candidato corresponda, al tipo de candidatura del acta
                    if (candidato.TipoCandidato != acta.TipoCandidato)
                        throw new InvalidOperationException(
                            $"El candidato {candidato.NombreCandidato} no corresponde al tipo de candidatura {acta.TipoCandidato} del acta.");

                    // Obtener lista principal del candidato
                    var listasPrincipales = candidato.ListaCandidatos.Where(x => x.ListaPrincipal).ToList();
                    if (listasPrincipales.Count != 1)
                        throw new InvalidOperationException($"El candidato '{candidato.NombreCandidato}' debe tener una lista principal");
                    // La lista principal es la que se guarda en el detalle
                    detalleDto.ListaElectoralId = listasPrincipales[0].ListaElectoralId;
                }
            }
            // Calcular total
            acta.TotalVotos = acta.ActaDetalles.Sum(x => x.Votos) + acta.VotosBlancos + acta.VotosNulos;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _actaRepository.AgregarAsync(acta);

                #region Registrar bitacora
                // Bitácora
                var valoresNuevos = await ObtenerValoresAuditoria(acta, eleccion, mesaElec);

                await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, acta.IdActa.ToString(), 
                    $"Acta registrada para la mesa '{mesaElec.CodigoMesa}-{acta.TipoCandidato}'.", acta.EleccionId, 
                    _currentService.UsuarioId, null, valoresNuevos);
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
                throw new InvalidOperationException($"El acta no puede ser modificada en su estado actual {existente.Estado}.");


            #region Validar Acta
            // validar Acta
            if (acta.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");
            if (acta.EleccionId != existente.EleccionId)
                throw new ArgumentException("No se puede modificar la elección del acta.");
            // validarMesa
            if (acta.MesaElectoralId == Guid.Empty)
                throw new ArgumentException("La mesa electoral es obligatorio.");
            if (acta.MesaElectoralId != existente.MesaElectoralId)
                throw new ArgumentException("No se puede modificar la mesa electoral del acta.");
            // validar tipo de candidato
            if (acta.TipoCandidato != existente.TipoCandidato)
                throw new ArgumentException("No se puede modificar la dignidad del acta.");

            // Validar votos
            if (acta.VotosBlancos < 0)
                throw new ArgumentException("Los votos blancos no pueden ser negativos.");
            if (acta.VotosNulos < 0)
                throw new ArgumentException("Los votos nulos no pueden ser negativos.");
            // Validar detalles
            if (acta.ActaDetalles == null || !acta.ActaDetalles.Any())
                throw new ArgumentException("El acta debe tener al menos un candidato o lista.");
            if (acta.ActaDetalles.Count != existente.ActaDetalles.Count)
                throw new ArgumentException("No se pueden agregar ni eliminar candidatos o listas del acta.");
            #endregion

            // OBTENER DATOS PARA AUDITORÍA
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(existente.EleccionId);
            if (eleccion == null)
                throw new KeyNotFoundException("La elección del acta no existe.");
            var mesaElec = await _mesaElectoralRepository.ObtenerPorIdAsync(existente.MesaElectoralId);
            if (mesaElec == null)
                throw new KeyNotFoundException("La mesa electoral del acta no existe.");

            //Valores anteriores
            var valoresAnteriores = await ObtenerValoresAuditoria(existente, eleccion, mesaElec);
            // Actualizar cabecera
            existente.VotosBlancos = acta.VotosBlancos;
            existente.VotosNulos = acta.VotosNulos;

            bool esConcejal = existente.TipoCandidato == TipoCandidato.ConcejalUrbano || existente.TipoCandidato == TipoCandidato.ConcejalRural;

            // Actualizar detalles
            foreach (var detalleDto in acta.ActaDetalles)
            {
                if (detalleDto.Votos < 0)
                    throw new ArgumentException("Los votos no pueden ser negativos.");

                ActaDetalle? detalleExistente;

                // CONCEJALES → voto por lista / plancha
                if (esConcejal)
                {
                    if (detalleDto.ListaElectoralId == Guid.Empty)
                        throw new ArgumentException("La lista electoral es obligatoria.");
                    // Buscar el detalle existente por lista
                    detalleExistente = existente.ActaDetalles.FirstOrDefault(x =>x.ListaElectoralId == detalleDto.ListaElectoralId);
                    if (detalleExistente == null)
                        throw new KeyNotFoundException($"La lista electoral no pertenece al acta.");

                    detalleExistente.Votos = detalleDto.Votos;
                    // En concejales no existe candidato individual
                    detalleExistente.CandidatoId = null;
                } 
                else
                {
                    if (detalleDto.CandidatoId == null || detalleDto.CandidatoId == Guid.Empty)
                        throw new ArgumentException("El candidato es obligatorio.");                    
                    // Buscar el detalle existente por candidato
                    detalleExistente = existente.ActaDetalles.FirstOrDefault(x => x.CandidatoId == detalleDto.CandidatoId);
                    if (detalleExistente == null)
                        throw new KeyNotFoundException($"El candidato no pertenece al acta.");

                    detalleExistente.Votos = detalleDto.Votos;
                }
            }

            // Recalcular total
            existente.TotalVotos = existente.ActaDetalles.Sum(x => x.Votos) + existente.VotosBlancos + existente.VotosNulos;
            //existente.UsuarioModificacionId = usuarioId;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                //valores nuevos 
                var valoresNuevos = await ObtenerValoresAuditoria(existente, eleccion, mesaElec);
                //cambios
                var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);
                if (cambios.Nuevos.Any())
                {
                    existente.UsuarioModificacionId = _currentService.UsuarioId;
                    existente.FechaModificacion = Fecha.DevolverDatetime(DateTime.UtcNow.ToString("o"));
                    // Bitácora
                    await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdActa.ToString(),
                        $"Acta modificada para la mesa '{mesaElec.CodigoMesa}-{acta.TipoCandidato}'.", acta.EleccionId, 
                        _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return existente;
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
            var valoresAnteriores = await ObtenerValoresAuditoria(acta, eleccion, mesa);
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
            var valoresNuevos = await ObtenerValoresAuditoria(acta, eleccion, mesa);
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
        private async Task<Dictionary<string, object?>> ObtenerValoresAuditoria(ActaEleccion acta, Eleccion eleccion, MesaElectoral mesa)
        {
            var valores = BitacoraHelper.ObtenerValores(acta, CamposAuditablesActa.Campos);
            BitacoraHelper.AgregarRelacion(valores, "Eleccion", eleccion.NombreEleccion);
            BitacoraHelper.AgregarRelacion(valores, "Mesa", mesa.CodigoMesa);
            //Obtener zona
            var zona = await _mesaElectoralRepository.ObtenerZonaAsync(mesa.ZonaId);
            var parroquia = await _zonaRepository.ObtenerParroquiaPorIdAsync(zona!.ParroquiaId);
            var canton = await _zonaRepository.ObtenerCantonPorIdAsync(parroquia!.CantonId);
            var provincia = await _zonaRepository.ObtenerProvinciaPorIdAsync(canton!.ProvinciaId);
            // agregar relaciones
            BitacoraHelper.AgregarRelacion(valores, "Zona", zona!.NombreZona);
            BitacoraHelper.AgregarRelacion(valores, "Parroquia", parroquia!.NombreParroquia);
            BitacoraHelper.AgregarRelacion(valores, "Canton", canton!.NombreCanton);
            BitacoraHelper.AgregarRelacion(valores, "Provincia", provincia!.NombreProvincia);

            // Detalles del acta
            if (acta.TipoCandidato == TipoCandidato.ConcejalUrbano || acta.TipoCandidato == TipoCandidato.ConcejalRural)
            {
                valores["Detalles"] = acta.ActaDetalles
                    .Select(d => new
                    {
                        Lista = d.ListaElectoral?.NombreLista,
                        NumeroLista = d.ListaElectoral?.NumeroLista,
                        Votos = d.Votos
                    })
                    .ToList();
            }
            else
            {
                valores["Detalles"] = acta.ActaDetalles
                .Select(d => new { Candidato = d.Candidato?.NombreCandidato, 
                    Lista = d.ListaElectoral?.NombreLista, NumeroLista = d.ListaElectoral?.NumeroLista,
                    Votos = d.Votos })
                .ToList();
            }
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
