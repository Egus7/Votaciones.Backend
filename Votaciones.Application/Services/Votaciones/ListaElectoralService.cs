using Microsoft.EntityFrameworkCore;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.Helpers;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Domain.Interfaces;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Application.Helpers.Audit.CamposAuditablesBitacora;

namespace Votaciones.Application.Services.Votaciones
{
    public class ListaElectoralService : IListaElectoralService
    {
        private readonly IListaElectoralRepository _listaElectoralRepository;
        private readonly ICurrentService _currentService;
        private readonly IBitacoraService _bitacoraService;
        private readonly IUnitOfWork _unitOfWork;
        //tabla
        private string tabla = "ListaElectoral";

        public ListaElectoralService(IListaElectoralRepository listaElectoralRepository, ICurrentService currentService, 
            IBitacoraService bitacoraService, IUnitOfWork unitOfWork)
        {
            _listaElectoralRepository = listaElectoralRepository;
            _currentService = currentService;
            _bitacoraService = bitacoraService;
            _unitOfWork = unitOfWork;
        }

        public async Task<PaginacionDTO<ListaElectoral>> ObtenerPaginacionAsync(int pagina, int pageSize, string? buscar = null)
        {
            var query = _listaElectoralRepository.ObtenerQuery();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var busqueda = buscar.Trim();

                query = query.Where(x => x.NombreLista.Contains(buscar) ||
                    (x.Siglas != null && x.Siglas.Contains(buscar)));
            }

            var totalRegistros = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.NumeroLista)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginacionDTO<ListaElectoral>
            {
                Items = items,
                PageActual = pagina,
                PageSize = pageSize,
                TotalRegistros = totalRegistros,
                TotalPages = (int)Math.Ceiling(totalRegistros / (double)pageSize)
            };
        }
        public async Task<ListaElectoral?> ObtenerPorIdAsync(Guid id)
        {
            var listaElectoral = _listaElectoralRepository.ObtenerPorIdAsync(id);

            if (listaElectoral == null)
            {
                throw new ArgumentException("No existe la lista electoral.");
            }
            return await listaElectoral;
        }

        public async Task<ListaElectoral> CrearAsync(ListaElectoral listaElectoral)
        {
            if (string.IsNullOrWhiteSpace(listaElectoral.NombreLista))
                throw new ArgumentException("El nombre de la lista electoral es obligatorio.");
            if (listaElectoral.NumeroLista <= 0)
                throw new ArgumentException("El número de la lista debe ser mayor a 0.");
            // verificar que el numero de lista no exista
            var nroLista = await _listaElectoralRepository.ObtenerNumeroListaAsync(listaElectoral.NumeroLista);
            if (nroLista != null)
                throw new ArgumentException("El número de lista electoral ya existe");

            listaElectoral.IdListaElectoral = Guid.NewGuid();
            listaElectoral.Activo = true;

            await _listaElectoralRepository.AgregarAsync(listaElectoral);

            #region Registrar bitacora
            //valores nuevos
            var valoresNuevos = BitacoraHelper.ObtenerValores(listaElectoral, CamposAuditablesListaElectoral.Campos);

            await _bitacoraService.RegistrarBitacoraAsync("INSERT", tabla, listaElectoral.IdListaElectoral.ToString(),
                $"Lista electoral creada '{listaElectoral.NombreLista} - {listaElectoral.NumeroLista}'.",
                null, _currentService.UsuarioId, null, valoresNuevos);
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return listaElectoral;
        }

        public async Task<ListaElectoral> ActualizarAsync(Guid id, ListaElectoral listaElectoral)
        {
            var existente = await _listaElectoralRepository.ObtenerPorIdAsync(id);

            if (existente == null)
                throw new KeyNotFoundException("La lista electoral no existe.");

            if (string.IsNullOrWhiteSpace(listaElectoral.NombreLista))
                throw new ArgumentException("El nombre de la lista electoral es obligatorio.");
            if (listaElectoral.NumeroLista <= 0)
                throw new ArgumentException("El número de la lista debe ser mayor a 0.");
            // verificar que el numero de lista no exista
            var nroLista = await _listaElectoralRepository.ObtenerNumeroListaAsync(listaElectoral.NumeroLista, id);
            if (nroLista != null)
                throw new ArgumentException("El número de lista electoral ya existe");

            // Valores ANTES de modificar
            var valoresAnteriores = BitacoraHelper.ObtenerValores(existente, CamposAuditablesListaElectoral.Campos);
            // valores a modificar 
            existente.NombreLista = listaElectoral.NombreLista;
            existente.NumeroLista = listaElectoral.NumeroLista;
            existente.Siglas = listaElectoral.Siglas;
            existente.Jurisdiccion = listaElectoral.Jurisdiccion;

            // Valores DESPUÉS de modificar
            var valoresNuevos = BitacoraHelper.ObtenerValores(existente, CamposAuditablesListaElectoral.Campos);
            // Detectar solamente cambios
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAnteriores, valoresNuevos);

            #region Registrar bitacora
            // Solo generar bitácora si realmente hubo cambios
            if (cambios.Nuevos.Any())
            {
                // Bitacora
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdListaElectoral.ToString(),
                $"Lista electoral modificada '{existente.NombreLista} - {existente.NumeroLista}'.", null, _currentService.UsuarioId,
                cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return existente;
        }
        public async Task<ListaElectoral> CambiarEstadoAsync(Guid id)
        {
            var existente = await _listaElectoralRepository.ObtenerPorIdAsync(id);
            if (existente == null)
                throw new KeyNotFoundException("La lista electoral no existe.");

            // valores antiguos
            var valoresAntes = BitacoraHelper.ObtenerValores(existente, CamposAuditablesListaElectoral.Campos);
            // cambiarEstado
            existente.Activo = !existente.Activo;

            #region Registrar bitacora
            // valores nuevos
            var valoresNuevos = BitacoraHelper.ObtenerValores(existente, CamposAuditablesListaElectoral.Campos);
            // obtener cambios
            var cambios = BitacoraHelper.ObtenerSoloCambios(valoresAntes, valoresNuevos);
            // Si no hubo cambios, no actualiza ni genera auditoría.
            if (cambios.Anteriores.Count > 0 || cambios.Nuevos.Count > 0)
            {
                //bitacora
                string descripcion = $"Lista electoral {existente.NombreLista} - {existente.NumeroLista} modificado estado a '{(existente.Activo ? "Activo" : "Inactivo")}'";
                await _bitacoraService.RegistrarBitacoraAsync("UPDATE", tabla, existente.IdListaElectoral.ToString(), descripcion,
                    null, _currentService.UsuarioId, cambios.Anteriores, cambios.Nuevos);
            }
            #endregion

            await _unitOfWork.SaveChangesAsync();

            return existente;
        }

    }
}
