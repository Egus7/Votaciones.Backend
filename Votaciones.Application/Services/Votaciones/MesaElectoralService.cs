using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Helpers;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Services.Votaciones
{
    public class MesaElectoralService : IMesaElectoralService
    {
        private readonly IMesaElectoralRepository _mesaElectoralRepository;
        private readonly IActaRepository _actaRepository;
        private readonly IEleccionRepository _eleccionRepository;
        private readonly IBitacoraService _bitacoraService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentService _currentService;
        //bitacora
        private string tabla = "MesaElectoral";

        public MesaElectoralService(IMesaElectoralRepository mesaElectoralRepository, IActaRepository actaRepository, 
            IEleccionRepository eleccionRepository, IBitacoraService bitacoraService, IMapper mapper, IUnitOfWork unitOfWork, 
            ICurrentService currentService)
        {
            _mesaElectoralRepository = mesaElectoralRepository;
            _actaRepository = actaRepository;
            _eleccionRepository = eleccionRepository;
            _bitacoraService = bitacoraService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _currentService = currentService;
        }

        public async Task<PaginacionDTO<MesaElectoralDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize, string? busqueda = null, TipoMesa? tipoMesa = null)
        {
            var query = _mesaElectoralRepository.ObtenerQuery().Where(x => x.EleccionId == eleccionId);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query = query.Where(x => x.CodigoMesa.Contains(busqueda) || x.Descripcion!.Contains(busqueda));
            }

            if (tipoMesa.HasValue)
            {
                query = query.Where(x => x.TipoMesa == tipoMesa);
            }

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.Zona!.NombreZona)
                .ThenBy(x => x.CodigoMesa)
                .ThenBy(x => x.TipoMesa)
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
        public async Task<List<MesaElectoralDTO>> ObtenerPorZonaAsync(Guid eleccionId, Guid zonaId)
        {
            var query = _mesaElectoralRepository.ObtenerQuery()
                .Where(x => x.EleccionId == eleccionId && x.ZonaId == zonaId);

            return await query.OrderBy(x => x.CodigoMesa)
                .ProjectTo<MesaElectoralDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
        public async Task<List<MesaElectoralDTO>> ObtenerDisponiblePorZonaAsync(Guid eleccionId, Guid zonaId, TipoCandidato tipoCandidato)
        {
            var query = _mesaElectoralRepository.ObtenerQuery()
                .Where(x => x.EleccionId == eleccionId && x.ZonaId == zonaId 
                    && !_actaRepository.ObtenerQuery().Any(a => a.MesaElectoralId == x.IdMesaElectoral 
                    && a.TipoCandidato == tipoCandidato));
            
            return await query.OrderBy(x => x.CodigoMesa)
                .ProjectTo<MesaElectoralDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<(string CodigoMesa, string Descripcion)> PrevisualizarAsync(Guid eleccionId, Guid zonaId, TipoMesa tipoMesa)
        {
            if (eleccionId == Guid.Empty)
                throw new ArgumentException("La elección es obligatoria.");
            if (zonaId == Guid.Empty)
                throw new ArgumentException("La zona es obligatoria.");

            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(eleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección ingresada no existe.");
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");

            var zona = await _mesaElectoralRepository.ObtenerZonaAsync(zonaId);
            if (zona == null)
                throw new ArgumentException("La zona ingresada no existe.");

            var numeroMesa = await _mesaElectoralRepository.ObtenerSiguienteNumeroMesaAsync(eleccionId, zonaId, tipoMesa);

            var codigoMesa = GenerarCodigoMesa(numeroMesa, tipoMesa);
            var descripcion = GenerarDescripcionMesa(numeroMesa, tipoMesa);

            return (codigoMesa, descripcion);
        }

        public async Task<MesaElectoral> CrearAsync(MesaElectoral mesaElectoral)
        {
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
            // Obtener el siguiente número según elección + zona + tipo
            var numeroMesa = await _mesaElectoralRepository.ObtenerSiguienteNumeroMesaAsync(mesaElectoral.EleccionId, mesaElectoral.ZonaId, mesaElectoral.TipoMesa);
            // Generar código
            var codigoMesa = GenerarCodigoMesa(numeroMesa, mesaElectoral.TipoMesa);
            // verificar que no exista el mismo codigo de mesa para una eleccion
            var mesaExistente = await _mesaElectoralRepository.ObtenerPorCodigoMesaByEleccionAsync(codigoMesa, mesaElectoral.EleccionId, mesaElectoral.ZonaId);
            if (mesaExistente != null)
            {
                throw new ArgumentException("El código de mesa generado ya existe para esta elección.");
            }
            // Generar descripción
            var descripcion = GenerarDescripcionMesa(numeroMesa, mesaElectoral.TipoMesa);

            mesaElectoral.IdMesaElectoral = Guid.NewGuid();
            mesaElectoral.CodigoMesa = codigoMesa;
            mesaElectoral.Descripcion = descripcion;
            mesaElectoral.Activa = true;

            await _mesaElectoralRepository.AgregarAsync(mesaElectoral);

            #region Registrar bitacora
            //valores nuevos
            var valoresNuevos = ObtenerValoresAuditoria(mesaElectoral, eleccion, zona);

            await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, mesaElectoral.IdMesaElectoral.ToString(),
                $"Mesa electoral creada '{mesaElectoral.CodigoMesa}' - Zona: {zona.NombreZona}.", 
                mesaElectoral.EleccionId, _currentService.UsuarioId, null, valoresNuevos);
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return mesaElectoral;
        }
        
        public async Task<List<MesaElectoral>> CrearLoteAsync(Guid eleccionId, Guid zonaId, int cantidadFemeninas, int cantidadMasculinas)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (eleccionId == Guid.Empty)
                    throw new ArgumentException("La elección es obligatoria.");

                if (zonaId == Guid.Empty)
                    throw new ArgumentException("La zona es obligatoria.");

                if (cantidadFemeninas < 0)
                    throw new ArgumentException("La cantidad de mesas femeninas no puede ser negativa.");
                if (cantidadMasculinas < 0)
                    throw new ArgumentException("La cantidad de mesas masculinas no puede ser negativa.");
                if (cantidadFemeninas == 0 && cantidadMasculinas == 0)
                    throw new ArgumentException("Debe ingresar al menos una mesa.");

                // Verificar elección
                var eleccion = await _eleccionRepository.ObtenerPorIdAsync(eleccionId);
                if (eleccion == null)
                    throw new ArgumentException("La elección ingresada no existe.");
                // Validar estado de la elección
                if (eleccion.Estado == EstadoEleccion.Cerrada)
                    throw new InvalidOperationException("La elección está cerrada.");

                // Verificar zona
                var zona = await _mesaElectoralRepository.ObtenerZonaAsync(zonaId);
                if (zona == null)
                    throw new ArgumentException("La zona ingresada no existe.");

                var mesas = new List<MesaElectoral>();
                #region Mesas femeninas

                if (cantidadFemeninas > 0)
                {
                    var numeroInicialFemenina = await _mesaElectoralRepository.ObtenerSiguienteNumeroMesaAsync(eleccionId, zonaId, TipoMesa.Femenina);

                    for (int i = 0; i < cantidadFemeninas; i++)
                    {
                        var numeroMesa = numeroInicialFemenina + i;

                        var mesa = new MesaElectoral
                        {
                            IdMesaElectoral = Guid.NewGuid(),
                            EleccionId = eleccionId,
                            ZonaId = zonaId,
                            TipoMesa = TipoMesa.Femenina,
                            CodigoMesa = GenerarCodigoMesa(numeroMesa, TipoMesa.Femenina),
                            Descripcion = GenerarDescripcionMesa(numeroMesa, TipoMesa.Femenina),
                            Activa = true
                        };
                        mesas.Add(mesa);
                    }
                }

                #endregion

                #region Mesas masculinas

                if (cantidadMasculinas > 0)
                {
                    var numeroInicialMasculina = await _mesaElectoralRepository.ObtenerSiguienteNumeroMesaAsync(eleccionId, zonaId, TipoMesa.Masculina);

                    for (int i = 0; i < cantidadMasculinas; i++)
                    {
                        var numeroMesa = numeroInicialMasculina + i;

                        var mesa = new MesaElectoral
                        {
                            IdMesaElectoral = Guid.NewGuid(),
                            EleccionId = eleccionId,
                            ZonaId = zonaId,
                            TipoMesa = TipoMesa.Masculina,
                            CodigoMesa = GenerarCodigoMesa(numeroMesa, TipoMesa.Masculina),
                            Descripcion = GenerarDescripcionMesa(numeroMesa, TipoMesa.Masculina),
                            Activa = true
                        };
                        mesas.Add(mesa);
                    }
                }

                #endregion

                // Agregar todas las mesas
                foreach (var mesa in mesas)
                {
                    await _mesaElectoralRepository.AgregarAsync(mesa);
                }

                // Guardar todas las mesas en una sola operación
                await _unitOfWork.SaveChangesAsync();

                #region Bitácora

                if (cantidadFemeninas > 0)
                {
                    var numeroInicialFemenina = mesas.Where(x => x.TipoMesa == TipoMesa.Femenina).Min(x => x.CodigoMesa);
                    var numeroFinalFemenina = mesas.Where(x => x.TipoMesa == TipoMesa.Femenina).Max(x => x.CodigoMesa);

                    var valoresNuevos = new
                    {
                        Zona = zona.NombreZona,
                        TipoMesa = TipoMesa.Femenina.ToString(),
                        Cantidad = cantidadFemeninas,
                        CodigoInicial = numeroInicialFemenina,
                        CodigoFinal = numeroFinalFemenina
                    };

                    string descripcionFem = $"Se crearon {cantidadFemeninas} mesas femeninas para la zona '{zona.NombreZona}'. " +
                        $"Rango de códigos: {numeroInicialFemenina} - {numeroFinalFemenina}.";
                    // registrar bitácora para mesas femeninas
                    await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, null, descripcionFem, eleccionId, _currentService.UsuarioId,
                            null, valoresNuevos);
                }

                if (cantidadMasculinas > 0)
                {
                    var numeroInicialMasculina = mesas.Where(x => x.TipoMesa == TipoMesa.Masculina).Min(x => x.CodigoMesa);
                    var numeroFinalMasculina = mesas.Where(x => x.TipoMesa == TipoMesa.Masculina).Max(x => x.CodigoMesa);

                    var valoresNuevos = new
                    {
                        Zona = zona.NombreZona,
                        TipoMesa = TipoMesa.Masculina.ToString(),
                        Cantidad = cantidadMasculinas,
                        CodigoInicial = numeroInicialMasculina,
                        CodigoFinal = numeroFinalMasculina
                    };

                    string descripcionMasc = $"Se crearon {cantidadMasculinas} mesas masculinas para la zona '{zona.NombreZona}'. " +
                        $"Rango de códigos: {numeroInicialMasculina} - {numeroFinalMasculina}.";
                    // registrar bitácora para mesas masculinas
                    await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, null, descripcionMasc, eleccionId,
                        _currentService.UsuarioId, null, valoresNuevos);
                }
                #endregion
                // gauardar 
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return mesas;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<MesaElectoral> ActualizarAsync(Guid id, MesaElectoral mesaElectoral)
        {
            // Validaciones básicas
            if (id == Guid.Empty)
                throw new ArgumentException("El id de la mesa es obligatorio.");

            if (mesaElectoral.ZonaId == Guid.Empty)
                throw new ArgumentException("La zona es obligatoria.");

            // Obtener mesa existente
            var mesaExistente = await _mesaElectoralRepository.ObtenerPorIdAsync(id);
            if (mesaExistente == null)
                throw new ArgumentException("La mesa electoral a actualizar no existe.");
            //verificar que la mesa este abierta
            if (!mesaExistente.Activa)
                throw new InvalidOperationException("La mesa electoral está cerrada y no puede ser modificada.");
            // Verificar elección
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(mesaExistente.EleccionId);
            if (eleccion == null)
                throw new ArgumentException("La elección de la mesa no existe.");
            // Validar estado de elección
            if (eleccion.Estado == EstadoEleccion.Cerrada)
                throw new InvalidOperationException("La elección está cerrada.");
            // Verificar zona
            var zona = await _mesaElectoralRepository.ObtenerZonaAsync(mesaElectoral.ZonaId);
            if (zona == null)
                throw new ArgumentException("La zona ingresada no existe.");

            #region Capturar valores anteriores
            var zonaExistente = await _mesaElectoralRepository.ObtenerZonaAsync(mesaExistente.ZonaId);
            var valoresAnteriores = ObtenerValoresAuditoria(mesaExistente, eleccion, zonaExistente!);

            #endregion

            // Determinar si cambió la zona o el tipo
            bool cambioEstructural = mesaExistente.ZonaId != mesaElectoral.ZonaId || mesaExistente.TipoMesa != mesaElectoral.TipoMesa;

            if (cambioEstructural)
            {
                // Obtener nuevo número para la combinación - Elección + Zona + TipoMesa
                var numeroMesa = await _mesaElectoralRepository.ObtenerSiguienteNumeroMesaAsync(mesaExistente.EleccionId, mesaElectoral.ZonaId, mesaElectoral.TipoMesa);
                // Generar nuevo código
                var codigoMesa = GenerarCodigoMesa(numeroMesa, mesaElectoral.TipoMesa);

                // Validar que no exista otra mesa con ese código
                var mesaConCodigoExistente = await _mesaElectoralRepository.ObtenerPorCodigoMesaByEleccionAsync(codigoMesa,
                        mesaExistente.EleccionId, mesaElectoral.ZonaId, id);

                if (mesaConCodigoExistente != null)
                    throw new ArgumentException("El código de mesa generado ya existe para esta elección y zona.");

                // Actualizar datos generados
                mesaExistente.CodigoMesa = codigoMesa;
                mesaExistente.Descripcion = GenerarDescripcionMesa(numeroMesa, mesaElectoral.TipoMesa);
            }

            // Actualizar datos modificables
            mesaExistente.ZonaId = mesaElectoral.ZonaId;
            mesaExistente.TipoMesa = mesaElectoral.TipoMesa;

            #region Registrar bitácora

            var valoresNuevos = ObtenerValoresAuditoria(mesaExistente, eleccion, zona);

            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);
            if (cambios.Nuevos.Any())
            {
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, mesaExistente.IdMesaElectoral.ToString(), 
                    $"Mesa electoral modificada '{mesaExistente.CodigoMesa}'.", mesaExistente.EleccionId, _currentService.UsuarioId,
                    cambios.Anteriores, cambios.Nuevos);
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
                    existente.EleccionId, _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return existente;
        }

        #region Metodos privados
        private string GenerarCodigoMesa(int numeroMesa, TipoMesa tipoMesa)
        {
            var sufijo = tipoMesa switch
            {
                TipoMesa.Femenina => "F",
                TipoMesa.Masculina => "M",
                _ => throw new ArgumentException("El tipo de mesa no es válido.")
            };

            return $"{numeroMesa:D2}{sufijo}";
        }
        private string GenerarDescripcionMesa(int numeroMesa, TipoMesa tipoMesa)
        {
            return $"Mesa {numeroMesa} {tipoMesa}";
        }

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
