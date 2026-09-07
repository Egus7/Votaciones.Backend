using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Helpers;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
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
        private readonly IBitacoraService _bitacoraService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentService _currentService;
        //bitacora
        private string tabla = "Candidato";

        public CandidatoService(IEleccionRepository eleccionRepository, IListaElectoralRepository listaElectoralRepository, 
            ICandidatoRepository candidatoRepository, IBitacoraService bitacoraService, IMapper mapper, IUnitOfWork unitOfWork, 
            ICurrentService currentService)
        {
            _eleccionRepository = eleccionRepository;
            _listaElectoralRepository = listaElectoralRepository;
            _candidatoRepository = candidatoRepository;
            _bitacoraService = bitacoraService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _currentService = currentService;
        }

        public async Task<PaginacionDTO<CandidatoDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize)
        {
            var query = _candidatoRepository.ObtenerQuery().Where(x => x.EleccionId == eleccionId);

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.NombreCandidato)
                .ThenBy(x => x.TipoCandidato)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<CandidatoDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();

            // armado para la Api
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
            if (string.IsNullOrWhiteSpace(candidato.NombreCandidato))
                throw new ArgumentException("El nombre del candidato es obligatorio.");
            // verifcar eleccionId no sea Guid.Empty
            if (candidato.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");
            // verificar que la Elección exista
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(candidato.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe");
            // validar estadoEleccion
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");

            // verificar que se seleccione al menos una lista electoral para candidato
            if (candidato.ListaCandidatos == null || !candidato.ListaCandidatos.Any())
                throw new ArgumentException("Debe seleccionar al menos una lista electoral para el concejal.");
            // Debe existir exactamente una lista principal
            if (!candidato.ListaCandidatos.Any(x => x.ListaPrincipal))
                throw new ArgumentException("Debe seleccionar una lista principal.");
            // Validar que no haya más de una lista principal
            if (candidato.ListaCandidatos.Count(x => x.ListaPrincipal) > 1)
                throw new InvalidOperationException("Solo puede haber una lista principal para el candidato.");
            // Evitar que la misma lista se envíe dos veces
            if (candidato.ListaCandidatos.GroupBy(x => x.ListaElectoralId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("No se puede asociar la misma lista electoral más de una vez.");
            // Validaciones específicas para concejales
            bool esConcejal = candidato.TipoCandidato == TipoCandidato.ConcejalUrbano || candidato.TipoCandidato == TipoCandidato.ConcejalRural;
            //validacion para concejales, que no se repita el orden en la misma lista y elección
            if (esConcejal)
            {
                if (candidato.Orden == null || candidato.Orden <= 0)
                    throw new ArgumentException("El orden del candidato para concejal es obligatorio.");
            }

            candidato.IdCandidato = Guid.NewGuid();
            candidato.Activo = true;

            // VALIDAR Y PREPARAR LISTAS
            foreach (var lista in candidato.ListaCandidatos)
            {
                if (lista.ListaElectoralId == Guid.Empty)
                    throw new ArgumentException("La lista electoral es obligatoria.");

                var listaElectoral = await _listaElectoralRepository.ObtenerPorIdAsync(lista.ListaElectoralId);
                if (listaElectoral == null)
                    throw new ArgumentException("La lista electoral ingresada no existe.");
                if (!listaElectoral.Activo)
                    throw new InvalidOperationException($"La lista electoral '{listaElectoral.NombreLista}' está suspendida.");

                //Alcalde
                if (!esConcejal)
                {
                    var existeCandidato = await _candidatoRepository.ExistePorEleccionListaTipoAsync(candidato.EleccionId,
                            lista.ListaElectoralId, candidato.TipoCandidato);
                    // Si existe un candidato para la misma elección, lista y tipo, no se permite crear otro
                    if (existeCandidato)
                        throw new InvalidOperationException($"Ya existe un candidato de tipo '{candidato.TipoCandidato}' registrado para " +
                            $"la lista electoral '{listaElectoral.NombreLista}'.");
                }
                //Concejales
                else
                {
                    var existeOrden = await _candidatoRepository.ExisteOrdenPorEleccionListaTipoAsync(candidato.EleccionId, 
                            lista.ListaElectoralId, candidato.TipoCandidato, candidato.Orden!.Value);
                    // Si existe un candidato para la misma elección, lista, tipo y orden, no se permite crear otro
                    if (existeOrden)
                        throw new InvalidOperationException($"El orden '{candidato.Orden}' ya está asignado a otro candidato " +
                            $"para la lista electoral '{listaElectoral.NombreLista}'.");
                }

                lista.IdListaCandidato = Guid.NewGuid();
                lista.CandidatoId = candidato.IdCandidato;
            }
            // si el candidato (alcalde o concejal) tiene una sola lista, esa lista debe ser la principal
            if (candidato.ListaCandidatos.Count == 1)
            {
                candidato.ListaCandidatos[0].ListaPrincipal = true;
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

        public async Task<Candidato> ActualizarAsync(Guid id, Candidato candidato)
        {
            var existente = await _candidatoRepository.ObtenerPorIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException("El candidato no existe.");

            if (string.IsNullOrWhiteSpace(candidato.NombreCandidato))
                throw new ArgumentException("El nombre del candidato es obligatorio.");
            // verifcar que la Elección exista
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(candidato.EleccionId);
            if (eleccion == null)
                throw new KeyNotFoundException("La elección del candidato no existe.");
            // validarEstadoEleccion
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");
            // No permitir cambiar el candidato a otra elección
            if (existente.EleccionId != candidato.EleccionId)
                throw new InvalidOperationException("No se puede cambiar la elección del candidato.");

            // Debe existir al menos una lista
            if (candidato.ListaCandidatos == null || !candidato.ListaCandidatos.Any())
                throw new ArgumentException("Debe seleccionar al menos una lista electoral.");
            // Debe existir exactamente una lista principal
            if (!candidato.ListaCandidatos.Any(x => x.ListaPrincipal))
                throw new ArgumentException("Debe seleccionar una lista principal.");
            if (candidato.ListaCandidatos.Count(x => x.ListaPrincipal) > 1)
                throw new InvalidOperationException("Solo puede haber una lista principal para el candidato.");
            // No permitir la misma lista dos veces
            if (candidato.ListaCandidatos.GroupBy(x => x.ListaElectoralId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("No se puede asociar la misma lista electoral más de una vez.");
            
            // Validaciones específicas para concejales
            bool esConcejal = candidato.TipoCandidato == TipoCandidato.ConcejalUrbano || candidato.TipoCandidato == TipoCandidato.ConcejalRural;
            if (esConcejal)
            {
                if (candidato.Orden == null || candidato.Orden <= 0)
                    throw new ArgumentException("El orden del candidato para concejal es obligatorio.");
            }
            // VALIDAR LISTAS Y GUARDARLAS PARA NO VOLVER A CONSULTAR
            var listasElectoralesAuditoria = new Dictionary<Guid, ListaElectoral>();
            // recorrer las listas del candidato y validar que existan y estén activas
            foreach (var lista in candidato.ListaCandidatos)
            {
                if (lista.ListaElectoralId == Guid.Empty)
                    throw new ArgumentException("La lista electoral es obligatoria.");
                var listaElectoral = await _listaElectoralRepository.ObtenerPorIdAsync(lista.ListaElectoralId);
                if (listaElectoral == null)
                    throw new ArgumentException("La lista electoral ingresada no existe.");
                if (!listaElectoral.Activo)
                    throw new InvalidOperationException($"La lista electoral '{listaElectoral.NombreLista}' está suspendida.");

                // Guardamos la lista para reutilizarla posteriormente
                listasElectoralesAuditoria[lista.ListaElectoralId] = listaElectoral;

                // validar que no exista otro candidato del mismo tipo para la misma elección y lista
                if (!esConcejal)
                {
                    var existeCandidato = await _candidatoRepository.ExistePorEleccionListaTipoAsync(candidato.EleccionId,
                            lista.ListaElectoralId, candidato.TipoCandidato, id);
                    if (existeCandidato)
                        throw new InvalidOperationException($"Ya existe un candidato de tipo '{candidato.TipoCandidato}' registrado para " +
                            $"la lista electoral '{listaElectoral.NombreLista}'.");
  
                }
                // validar que no exista otro candidato del mismo tipo y orden para la misma elección y lista
                else
                {
                    var existeOrden = await _candidatoRepository.ExisteOrdenPorEleccionListaTipoAsync(candidato.EleccionId, lista.ListaElectoralId,
                            candidato.TipoCandidato, candidato.Orden!.Value, id);
                    if (existeOrden) 
                    {
                        throw new InvalidOperationException($"El orden '{candidato.Orden}' ya está asignado a otro candidato " +
                            $"para la lista electoral '{listaElectoral.NombreLista}'.");
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
                        ListaElectoral = listasElectoralesAuditoria[lista.ListaElectoralId]  // Asignar la lista electoral previamente obtenida
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
        private Dictionary<string, object?> ObtenerValoresAuditoria(Candidato candidato, Eleccion eleccion)
        {
            var valores = BitacoraHelper.ObtenerValores(candidato, CamposAuditablesCandidato.Campos);

            BitacoraHelper.AgregarRelacion(valores, "Eleccion", eleccion.NombreEleccion);
            // detalles de listas candidatos
            valores["Listas Candidatos"] = candidato.ListaCandidatos.Select(lc => new
            {
                Lista = lc.ListaElectoral?.NombreLista,
                NumeroLista = lc.ListaElectoral?.NumeroLista,
                EsPrincipal = lc.ListaPrincipal
            }).ToList();

            return valores;
        }

        #endregion

    }
}
