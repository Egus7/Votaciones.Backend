using Votaciones.Domain.Models;

namespace Votaciones.Domain.Interfaces.IRepositories
{
    public interface IZonaRepository
    {
        Task<List<Provincia>> ObtenerProvinciasAsync();
        Task<Provincia?> ObtenerProvinciaPorIdAsync(Guid provinciaId);
        Task<List<Canton>> ObtenerCantonesAsync(Guid provinciaId);
        Task<Canton?> ObtenerCantonPorIdAsync(Guid cantonId);
        Task<List<Parroquia>> ObtenerParroquiasAsync(Guid cantonId);
        Task<Parroquia?> ObtenerParroquiaPorIdAsync(Guid parroquiaId);
        Task<List<Zona>> ObtenerZonasAsync(Guid parroquiaId);
        Task<Zona?> ObtenerZonaPorIdAsync(Guid zonaId);
    }
}
