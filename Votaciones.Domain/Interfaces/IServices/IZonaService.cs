using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IServices
{
    public interface IZonaService
    {
        Task<List<Provincia>> ObtenerProvinciasAsync();
        Task<List<Canton>> ObtenerCantonesAsync(Guid provinciaId);
        Task<List<Parroquia>> ObtenerParroquiasAsync(Guid cantonId);
        Task<List<Zona>> ObtenerZonasAsync(Guid parroquiaId);
    }
}
