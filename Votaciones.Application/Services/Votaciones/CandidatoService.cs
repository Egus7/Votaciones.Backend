using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Helpers;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Services.Votaciones
{
    public class CandidatoService : ICandidatoService
    {
        private readonly IEleccionRepository _eleccionRepository;
        private readonly IListaElectoralRepository _listaElectoralRepository;
        private readonly ICandidatoRepository _candidatoRepository;
        private readonly IActaRepository _actaRepository;
        private readonly IZonaRepository _zonaRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentService _currentService;
        //bitacora
        private string tabla = "Candidato";

        public CandidatoService(IEleccionRepository eleccionRepository, IListaElectoralRepository listaElectoralRepository, 
            ICandidatoRepository candidatoRepository, IActaRepository actaRepository, IZonaRepository zonaRepository, IBitacoraService bitacoraService, 
            IMapper mapper, IUnitOfWork unitOfWork, ICurrentService currentService)
        {
            _eleccionRepository = eleccionRepository;
            _listaElectoralRepository = listaElectoralRepository;
            _candidatoRepository = candidatoRepository;
            _actaRepository = actaRepository;
            _zonaRepository = zonaRepository;
            _bitacoraService = bitacoraService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _currentService = currentService;
        }

        public async Task<PaginacionDTO<CandidatoDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize, string? busqueda = null,
            TipoCandidato? tipoCandidato = null, Guid? listaElectoralId = null)
        {
            var query = _candidatoRepository.ObtenerQuery().Where(x => x.EleccionId == eleccionId);

            // Buscar por nombre
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();
                query = query.Where(x => x.NombreCandidato.Contains(busqueda));
            }
            // Filtrar por tipo de candidato
            if (tipoCandidato.HasValue)
            {
                query = query.Where(x => x.TipoCandidato == tipoCandidato.Value);
            }
            // Filtrar por lista electoral
            if (listaElectoralId.HasValue)
            {
                query = query.Where(x => x.ListaCandidatos.Any(l => l.ListaElectoralId == listaElectoralId.Value));
            }

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.ListaCandidatos.Where(l => l.ListaPrincipal).Select(l => l.ListaElectoral!.NumeroLista).FirstOrDefault())
                .ThenBy(x => x.TipoCandidato)
                .ThenBy(x => x.Orden)
                .ThenBy(x => x.ProvinciaId)
                .ThenBy(x => x.CantonId)
                .ThenBy(x => x.ParroquiaId)
                .ThenBy(x => x.NombreCandidato)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<CandidatoDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new PaginacionDTO<CandidatoDTO>
            {
                Items = items,
                PageActual = pagina,
                PageSize = pageSize,
                TotalRegistros = totalRegistros,
                TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize)
            };
        }

        public async Task<CandidatoDTO?> ObtenerPorIdAsync(Guid id)
        {
            var query = _candidatoRepository.ObtenerQuery();

            return await query.Where(x => x.IdCandidato == id)
                .ProjectTo<CandidatoDTO>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        public async Task<Candidato> CrearAsync(Candidato candidato)
        {
            var eleccion = await ValidarDatosCandidato(candidato);
            var listasElectorales = await ValidarListasAsync(candidato);
            // EsConcejal
            bool esConcejal = candidato.TipoCandidato == TipoCandidato.ConcejalUrbano || candidato.TipoCandidato == TipoCandidato.ConcejalRural;

            candidato.IdCandidato = Guid.NewGuid();
            candidato.Activo = true;

            // VALIDAR Y PREPARAR LISTAS
            foreach (var lista in candidato.ListaCandidatos)
            {
                var listaElectoral = listasElectorales[lista.ListaElectoralId];
                //Alcalde
                if (!esConcejal)
                {
                    var existeCandidato = await _candidatoRepository.ExistePorEleccionListaTipoAsync(candidato.EleccionId,
                            lista.ListaElectoralId, candidato.TipoCandidato, candidato.ProvinciaId, candidato.CantonId, candidato.ParroquiaId);
                    // Si existe un candidato para la misma elección, lista y tipo, no se permite crear otro
                    if (existeCandidato)
                        throw new InvalidOperationException($"Ya existe un candidato de tipo '{candidato.TipoCandidato}' registrado para " +
                            $"la lista electoral '{listaElectoral.NombreLista}' en la zona seleccionada.");
                }
                //Concejales
                else
                {
                    var existeOrden = await _candidatoRepository.ExisteOrdenPorEleccionListaTipoAsync(candidato.EleccionId, 
                            lista.ListaElectoralId, candidato.TipoCandidato, candidato.Orden!.Value, candidato.ProvinciaId, 
                            candidato.CantonId, candidato.ParroquiaId);
                    // Si existe un candidato para la misma elección, lista, tipo y orden, no se permite crear otro
                    if (existeOrden)
                        throw new InvalidOperationException($"El orden '{candidato.Orden}' ya está asignado a otro candidato " +
                            $"para la lista electoral '{listaElectoral.NombreLista}' en la zona seleccionada.");
                }

                lista.IdListaCandidato = Guid.NewGuid();
                lista.CandidatoId = candidato.IdCandidato;
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _candidatoRepository.AgregarAsync(candidato);
                #region Registrar bitacora
                //valores nuevos
                var valoresNuevos = ObtenerValoresAuditoria(candidato, eleccion);

                await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, candidato.IdCandidato.ToString(),
                    $"Candidato creado '{candidato.NombreCandidato}'", candidato.EleccionId, _currentService.UsuarioId, null, valoresNuevos);
                #endregion

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return candidato;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<List<Candidato>> CrearVariosAsync(List<Candidato> candidatos)
        {
            if (candidatos == null || !candidatos.Any())
                throw new ArgumentException("Debe ingresar al menos un candidato.");
            // VALIDAR QUE SEA PARA CONCEJALES
            if (candidatos.Any(x => x.TipoCandidato != TipoCandidato.ConcejalUrbano && 
                x.TipoCandidato != TipoCandidato.ConcejalRural))
            {
                throw new ArgumentException("El registro múltiple solo está permitido para concejales.");
            }

            // TODOS DEBEN SER DE LA MISMA ELECCIÓN
            var eleccionId = candidatos.First().EleccionId;
            if (candidatos.Any(x => x.EleccionId != eleccionId))
                throw new ArgumentException("Todos los candidatos deben pertenecer a la misma elección.");

            var tipoCandidato = candidatos.First().TipoCandidato;
            if (candidatos.Any(x => x.TipoCandidato != tipoCandidato))
                throw new ArgumentException("Todos los candidatos deben ser del mismo tipo.");

            // VALIDAR CADA CANDIDATO
            Eleccion? eleccion = null;
            foreach (var candidato in candidatos)
            {
                eleccion = await ValidarDatosCandidato(candidato);
                await ValidarListasAsync(candidato);
            }

            // MISMO ÁMBITO TERRITORIAL
            var provinciaId = candidatos.First().ProvinciaId;
            var cantonId = candidatos.First().CantonId;
            var parroquiaId = candidatos.First().ParroquiaId;

            if (candidatos.Any(x => x.ProvinciaId != provinciaId || x.CantonId != cantonId || x.ParroquiaId != parroquiaId))
                throw new ArgumentException("Todos los candidatos deben pertenecer a la misma zona.");

            // MISMAS LISTAS
            var listasReferencia = candidatos.First().ListaCandidatos.Select(x => x.ListaElectoralId)
                .OrderBy(x => x).ToList();

            foreach (var candidato in candidatos)
            {
                var listasCandidato = candidato.ListaCandidatos
                    .Select(x => x.ListaElectoralId)
                    .OrderBy(x => x)
                    .ToList();

                if (!listasCandidato.SequenceEqual(listasReferencia))
                    throw new InvalidOperationException("Todos los candidatos deben pertenecer a las mismas listas electorales.");
            }

            // MISMA LISTA PRINCIPAL
            var listaPrincipalReferencia = candidatos.First()
                .ListaCandidatos.First(x => x.ListaPrincipal).ListaElectoralId;

            foreach (var candidato in candidatos)
            {
                var listaPrincipal = candidato.ListaCandidatos
                    .First(x => x.ListaPrincipal)
                    .ListaElectoralId;

                if (listaPrincipal != listaPrincipalReferencia)
                {
                    throw new InvalidOperationException("Todos los candidatos deben tener la misma lista principal.");
                }
            }

            // NO REPETIR ORDEN DENTRO DE LA CARGA
            if (candidatos.GroupBy(x => x.Orden).Any(g => g.Count() > 1))
                throw new InvalidOperationException("No puede existir más de un candidato con el mismo orden.");
            // VALIDAR ORDEN
            foreach (var candidato in candidatos)
            {
                foreach (var lista in candidato.ListaCandidatos)
                {
                    var existeOrden = await _candidatoRepository.ExisteOrdenPorEleccionListaTipoAsync(candidato.EleccionId, lista.ListaElectoralId, 
                        candidato.TipoCandidato, candidato.Orden!.Value, candidato.ProvinciaId, candidato.CantonId, candidato.ParroquiaId);

                    if (existeOrden)
                        throw new InvalidOperationException($"El orden '{candidato.Orden}' ya está asignado " +
                            $"para la lista electoral seleccionada.");
                }
            }

            // PREPARAR CANDIDATOS
            foreach (var candidato in candidatos)
            {
                candidato.IdCandidato = Guid.NewGuid();
                candidato.Activo = true;

                foreach (var lista in candidato.ListaCandidatos)
                {
                    lista.IdListaCandidato = Guid.NewGuid();
                    lista.CandidatoId = candidato.IdCandidato;
                }
            }
            // UNA SOLA TRANSACCIÓN
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var candidato in candidatos)
                {
                    await _candidatoRepository.AgregarAsync(candidato);
                }

                #region Registrar bitacora
                
                // UNA SOLA BITÁCORA
                var cantidad = candidatos.Count;
                var listaPrincipal = candidatos.First().ListaCandidatos.First(x => x.ListaPrincipal);
                var descripcion = $"Se registraron {cantidad} candidatos de tipo '{tipoCandidato}' para la lista electoral " +
                        $"'{listaPrincipal.ListaElectoral?.NombreLista}'.";
                //valores nuevos
                var valoresNuevos = await ObtenerValoresAuditoriaVariosAsync(candidatos, eleccion!);

                await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, null, descripcion, eleccionId, _currentService.UsuarioId, 
                    null, valoresNuevos);
                #endregion

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
                return candidatos;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<Candidato> ActualizarAsync(Guid id, Candidato candidato)
        {
            var existente = await _candidatoRepository.ObtenerPorIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException("El candidato no existe.");

            var eleccion = await ValidarDatosCandidato(candidato);
            // No permitir cambiar la elección
            if (existente.EleccionId != candidato.EleccionId)
                throw new InvalidOperationException("No se puede cambiar la elección del candidato.");
            // validar listas electorales
            var listasElectorales = await ValidarListasAsync(candidato);
            // Validaciones específicas para concejales
            bool esConcejal = candidato.TipoCandidato == TipoCandidato.ConcejalUrbano || candidato.TipoCandidato == TipoCandidato.ConcejalRural;
            bool tieneActas = false;

            if (esConcejal)
            {
                var listaPrincipal = existente.ListaCandidatos.FirstOrDefault(x => x.ListaPrincipal);
                // Para concejales el acta registra la lista, no el candidato
                tieneActas = await _actaRepository.ExistePorListaYAmbitoAsync(existente.EleccionId, listaPrincipal!.ListaElectoralId,
                    existente.TipoCandidato, existente.ProvinciaId, existente.CantonId, existente.ParroquiaId);
            }
            else
            {
                // Para los demás tipos el acta registra directamente el candidato
                tieneActas = await _actaRepository.ExistePorCandidatoAsync(existente.EleccionId, existente.IdCandidato);
            }
            // Si el candidato tiene actas registradas, no se permite modificar algunos campos
            if (tieneActas)
            {
                // No puede modificar el tipo de candidato
                if (existente.TipoCandidato != candidato.TipoCandidato)
                    throw new InvalidOperationException(
                        "No se puede modificar el tipo de candidato porque el candidato tiene actas registradas.");

                // No puede modificar la ubicación
                if (existente.ProvinciaId != candidato.ProvinciaId || existente.CantonId != candidato.CantonId ||
                    existente.ParroquiaId != candidato.ParroquiaId)
                {
                    throw new InvalidOperationException(
                        "No se puede modificar la zona del candidato porque ya tiene actas registradas.");
                }
                // no se puede cambiar la lista principal si el candidato tiene actas registradas
                if (existente.ListaCandidatos.Any(x => x.ListaPrincipal) && candidato.ListaCandidatos.Any(x => x.ListaPrincipal))
                {
                    var listaPrincipalExistente = existente.ListaCandidatos.First(x => x.ListaPrincipal);
                    var listaPrincipalNueva = candidato.ListaCandidatos.First(x => x.ListaPrincipal);
                    if (listaPrincipalExistente.ListaElectoralId != listaPrincipalNueva.ListaElectoralId)
                    {
                        throw new InvalidOperationException(
                            "No se puede cambiar la lista principal del candidato porque ya tiene actas registradas.");
                    }
                }
            }
            // No permitir eliminar una lista que ya fue utilizada en un acta
            foreach (var listaActual in existente.ListaCandidatos)
            {
                var sigueSeleccionada = candidato.ListaCandidatos.Any(x => x.ListaElectoralId == listaActual.ListaElectoralId);

                if (!sigueSeleccionada)
                {
                    var utilizada = await _actaRepository.ExistePorListaYAmbitoAsync(existente.EleccionId, listaActual.ListaElectoralId,
                        existente.TipoCandidato, existente.ProvinciaId, existente.CantonId, existente.ParroquiaId);

                    if (utilizada)
                    {
                        throw new InvalidOperationException($"No se puede eliminar la lista electoral '{listaActual.ListaElectoral?.NombreLista}' " +
                            "porque ya fue utilizada en un acta.");
                    }
                }
            }
            // VALIDAR LISTAS Y GUARDARLAS PARA NO VOLVER A CONSULTAR
            // recorrer las listas del candidato y validar que existan y estén activas
            foreach (var lista in candidato.ListaCandidatos)
            {
                var listaElectoral = listasElectorales[lista.ListaElectoralId];

                // validar que no exista otro candidato del mismo tipo para la misma elección y lista
                if (!esConcejal)
                {
                    var existeCandidato = await _candidatoRepository.ExistePorEleccionListaTipoAsync(candidato.EleccionId,
                            lista.ListaElectoralId, candidato.TipoCandidato, candidato.ProvinciaId, candidato.CantonId, candidato.ParroquiaId, id);
                    if (existeCandidato)
                        throw new InvalidOperationException($"Ya existe un candidato de tipo '{candidato.TipoCandidato}' registrado para " +
                            $"la lista electoral '{listaElectoral.NombreLista}' en la zona seleccionada.");

                }
                // validar que no exista otro candidato del mismo tipo y orden para la misma elección y lista
                else
                {
                    var existeOrden = await _candidatoRepository.ExisteOrdenPorEleccionListaTipoAsync(candidato.EleccionId, lista.ListaElectoralId,
                            candidato.TipoCandidato, candidato.Orden!.Value, candidato.ProvinciaId, candidato.CantonId, candidato.ParroquiaId, id);
                    if (existeOrden) 
                    {
                        throw new InvalidOperationException($"El orden '{candidato.Orden}' ya está asignado a otro candidato " +
                            $"para la lista electoral '{listaElectoral.NombreLista}' en la zona seleccionada.");
                    }
                }
            }
            // INICIAR TRANSACCIÓN
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                //valores antiguos
                var valoresAnteriores = ObtenerValoresAuditoria(existente, eleccion);
                // actualizar campos
                existente.NombreCandidato = candidato.NombreCandidato.Trim();
                existente.TipoCandidato = candidato.TipoCandidato;
                existente.Orden = candidato.Orden;
                existente.ProvinciaId = candidato.ProvinciaId;
                existente.CantonId = candidato.CantonId;
                existente.ParroquiaId = candidato.ParroquiaId;
                existente.Activo = candidato.Activo;

                // Eliminar las relaciones
                if (existente.ListaCandidatos != null && existente.ListaCandidatos.Any())
                {
                    _candidatoRepository.EliminarListaCandidatos(existente.ListaCandidatos);
                }
                // Volver a crear las relaciones
                foreach (var lista in candidato.ListaCandidatos)
                {
                    await _candidatoRepository.AgregarListaCandidatoAsync(new ListaCandidato
                    {
                        IdListaCandidato = Guid.NewGuid(),
                        CandidatoId = existente.IdCandidato,
                        ListaElectoralId = lista.ListaElectoralId,
                        ListaPrincipal = lista.ListaPrincipal,
                        ListaElectoral = listasElectorales[lista.ListaElectoralId]  // Asignar la lista electoral previamente obtenida
                    });

                }
                // Guardar cambios
                await _unitOfWork.SaveChangesAsync();
                //valores nuevos
                var valoresNuevos = ObtenerValoresAuditoria(existente, eleccion);
                //obtener solo cambios  
                var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);
                
                #region Registrar bitacora
                //Registrar bitacora solo si hay cambios
                if (cambios.Nuevos.Any())
                {
                    await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdCandidato.ToString(),
                        $"Candidato modificado '{existente.NombreCandidato}'", existente.EleccionId, _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
                }
                #endregion
                // Guardar cambios
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
                    existente.EleccionId, _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return existente;
        }

        #region Metodos privados
        private async Task<Eleccion> ValidarDatosCandidato(Candidato candidato)
        {
            if (string.IsNullOrWhiteSpace(candidato.NombreCandidato))
                throw new ArgumentException("El nombre del candidato es obligatorio.");

            if (candidato.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");

            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(candidato.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe.");

            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");

            if (!Enum.IsDefined(typeof(TipoCandidato), candidato.TipoCandidato))
            {
                throw new ArgumentException("El tipo de candidato es inválido.");
            }

            // Debe existir al menos una lista
            if (candidato.ListaCandidatos == null || !candidato.ListaCandidatos.Any())
            {
                throw new ArgumentException("Debe seleccionar al menos una lista electoral.");
            }

            // Debe existir exactamente una lista principal
            if (!candidato.ListaCandidatos.Any(x => x.ListaPrincipal))
                throw new ArgumentException("Debe seleccionar una lista principal.");

            if (candidato.ListaCandidatos.Count(x => x.ListaPrincipal) > 1)
                throw new InvalidOperationException("Solo puede haber una lista principal para el candidato.");

            // No permitir la misma lista dos veces
            if (candidato.ListaCandidatos.GroupBy(x => x.ListaElectoralId).Any(g => g.Count() > 1))
            {
                throw new InvalidOperationException("No se puede asociar la misma lista electoral más de una vez.");
            }

            // Validación específica de concejales
            bool esConcejal = candidato.TipoCandidato == TipoCandidato.ConcejalUrbano || candidato.TipoCandidato == TipoCandidato.ConcejalRural;
            if (esConcejal)
            {
                if (!candidato.Orden.HasValue || candidato.Orden <= 0)
                {
                    throw new ArgumentException("El orden del candidato es obligatorio.");
                }
            }
            return eleccion;
        }
        private async Task<Dictionary<Guid, ListaElectoral>> ValidarListasAsync(Candidato candidato)
        {
            var listas = new Dictionary<Guid, ListaElectoral>();
            foreach (var lista in candidato.ListaCandidatos)
            {
                if (lista.ListaElectoralId == Guid.Empty)
                    throw new ArgumentException("La lista electoral es obligatoria.");
                var listaElectoral = await _listaElectoralRepository.ObtenerPorIdAsync(lista.ListaElectoralId);

                if (listaElectoral == null)
                    throw new ArgumentException("La lista electoral ingresada no existe.");

                if (!listaElectoral.Activo)
                {
                    throw new InvalidOperationException($"La lista electoral '{listaElectoral.NombreLista}' está suspendida.");
                }

                listas[lista.ListaElectoralId] = listaElectoral;
            }

            return listas;
        }

        private Dictionary<string, object?> ObtenerValoresAuditoria(Candidato candidato, Eleccion eleccion)
        {
            var valores = BitacoraHelper.ObtenerValores(candidato, CamposAuditablesCandidato.Campos);

            BitacoraHelper.AgregarRelacion(valores, "Eleccion", eleccion.NombreEleccion);
            // agregar relaciones de provincia, canton y parroquia si existen
            if (candidato.ProvinciaId.HasValue)
            {
                var provincia = _zonaRepository.ObtenerProvinciaPorIdAsync(candidato.ProvinciaId!.Value);
                BitacoraHelper.AgregarRelacion(valores, "Provincia", provincia.Result!.NombreProvincia!);
            }
            if (candidato.CantonId.HasValue)
            {
                var canton = _zonaRepository.ObtenerCantonPorIdAsync(candidato.CantonId.Value);
                BitacoraHelper.AgregarRelacion(valores, "Canton", canton.Result!.NombreCanton!);
            }
            if (candidato.ParroquiaId.HasValue)
            {
                var parroquia = _zonaRepository.ObtenerParroquiaPorIdAsync(candidato.ParroquiaId.Value);
                BitacoraHelper.AgregarRelacion(valores, "Parroquia", parroquia.Result!.NombreParroquia!);
            }
            // detalles de listas candidatos
            valores["Listas Candidatos"] = candidato.ListaCandidatos.Select(lc => new
            {
                Lista = lc.ListaElectoral?.NombreLista,
                NumeroLista = lc.ListaElectoral?.NumeroLista,
                EsPrincipal = lc.ListaPrincipal
            }).ToList();

            return valores;
        }
        //valoresAuditoria para bitacora de varios candidatos
        private async Task<Dictionary<string, object?>> ObtenerValoresAuditoriaVariosAsync(List<Candidato> candidatos, Eleccion eleccion)
        {
            var valores = new Dictionary<string, object?>
            {
                ["CantidadCandidatos"] = candidatos.Count,
                ["TipoCandidato"] = candidatos.First().TipoCandidato.ToString()
            };

            // Elección
            BitacoraHelper.AgregarRelacion(valores, "Eleccion", eleccion.NombreEleccion);
            // Provincia
            var provinciaId = candidatos.First().ProvinciaId;
            if (provinciaId.HasValue)
            {
                var provincia = await _zonaRepository.ObtenerProvinciaPorIdAsync(provinciaId.Value);

                if (provincia != null)
                    BitacoraHelper.AgregarRelacion(valores, "Provincia", provincia.NombreProvincia);
            }
            // Cantón
            var cantonId = candidatos.First().CantonId;
            if (cantonId.HasValue)
            {
                var canton = await _zonaRepository.ObtenerCantonPorIdAsync(cantonId.Value);

                if (canton != null)
                    BitacoraHelper.AgregarRelacion(valores, "Canton", canton.NombreCanton);
            }
            // Parroquia
            var parroquiaId = candidatos.First().ParroquiaId;
            if (parroquiaId.HasValue)
            {
                var parroquia = await _zonaRepository.ObtenerParroquiaPorIdAsync(parroquiaId.Value);
                if (parroquia != null)
                    BitacoraHelper.AgregarRelacion(valores, "Parroquia", parroquia.NombreParroquia);
            }

            // Candidatos y sus listas
            valores["Candidatos"] = candidatos.OrderBy(x => x.Orden)
                .Select(x => new 
                {
                    x.NombreCandidato,
                    x.Orden,
                    Listas = x.ListaCandidatos
                        .Select(lc => new
                        {
                            NumeroLista = lc.ListaElectoral?.NumeroLista,
                            Lista = lc.ListaElectoral?.NombreLista,
                            EsPrincipal = lc.ListaPrincipal
                        })
                        .ToList()
                })
                .ToList();

            return valores;
        }
        #endregion

    }
}
