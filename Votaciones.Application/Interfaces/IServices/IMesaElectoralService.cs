using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Interfaces.IServices
{
    public interface IMesaElectoralService
    {
        Task<PaginacionDTO<MesaElectoralDTO>> ObtenerPaginacionAsync(Guid eleccionId, int pagina, int pageSize, string? busqueda = null, TipoMesa? tipoMesa = null);
        Task<MesaElectoralDTO?> ObtenerPorIdAsync(Guid id);
        Task<List<MesaElectoralDTO>> ObtenerPorZonaAsync(Guid eleccionId, Guid zonaId);
        Task<List<MesaElectoralDTO>> ObtenerDisponiblePorZonaAsync(Guid eleccionId, Guid zonaId, TipoCandidato tipoCandidato);
        Task<(string CodigoMesa, string Descripcion)> PrevisualizarAsync(Guid eleccionId, Guid zonaId, TipoMesa tipoMesa);
        Task<MesaElectoral> CrearAsync(MesaElectoral mesaElectoral);
        Task<List<MesaElectoral>> CrearLoteAsync(Guid eleccionId, Guid zonaId, int cantidadFemeninas, int cantidadMasculinas);
        Task<MesaElectoral> ActualizarAsync(Guid id, MesaElectoral mesaElectoral);
        Task<MesaElectoral> CambiarEstadoAsync(Guid id);
    }
}
