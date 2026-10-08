using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;

namespace Votaciones.Application.Services.Votaciones
{
    public class ZonaService : IZonaService
    {
        private readonly IZonaRepository _zonaRepository;

        public ZonaService(IZonaRepository zonaRepository)
        {
            _zonaRepository = zonaRepository;
        }

        public async Task<List<Provincia>> ObtenerProvinciasAsync()
        {
            return await _zonaRepository.ObtenerProvinciasAsync();
        }

        public async Task<List<Canton>> ObtenerCantonesAsync(Guid provinciaId)
        {
            if (provinciaId == Guid.Empty)
                throw new ArgumentException("La provincia es obligatoria.");

            return await _zonaRepository.ObtenerCantonesAsync(provinciaId);
        }

        public async Task<List<Parroquia>> ObtenerParroquiasAsync(Guid cantonId)
        {
            if (cantonId == Guid.Empty)
                throw new ArgumentException("El cantón es obligatorio.");
            return await _zonaRepository.ObtenerParroquiasAsync(cantonId);
        }

        public async Task<List<Zona>> ObtenerZonasAsync(Guid parroquiaId)
        {
            if (parroquiaId == Guid.Empty)
                throw new ArgumentException("La parroquia es obligatoria.");
            return await _zonaRepository.ObtenerZonasAsync(parroquiaId);
        }

    }
}
