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
        private readonly ICandidatoRepository _candidatoRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentService _currentService;
        //bitacora
        private string tabla = "Candidato";

        public CandidatoService(IEleccionRepository eleccionRepository, ICandidatoRepository candidatoRepository, 
            IBitacoraService bitacoraService, IMapper mapper, IUnitOfWork unitOfWork, ICurrentService currentService)
        {
            _eleccionRepository = eleccionRepository;
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
                .OrderBy(x => x.NumeroLista)
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

            if (candidato.EleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");
            // verificar que la Elección exista
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(candidato.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe");
            // validar estadoEleccion
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");
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
                $"Candidato creado '{candidato.NombreCandidato}'", candidato.EleccionId, _currentService.UsuarioId, null, valoresNuevos);
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
            // validarEstadoEleccion
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");
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
                    $"Candidato modificado '{existente.NombreCandidato}'", existente.EleccionId, _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
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

            return valores;
        }

        #endregion

    }
}
