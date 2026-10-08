using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class ZonaRepository : IZonaRepository
    {
        private readonly AppDbContext _context;

        public ZonaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Provincia>> ObtenerProvinciasAsync()
        {
            return await _context.Provincias.AsNoTracking().OrderBy(p => Convert.ToInt32(p.CodigoProvincia)).ToListAsync();
        }
        public async Task<Provincia?> ObtenerProvinciaPorIdAsync(Guid provinciaId)
        {
            return await _context.Provincias.AsNoTracking().Where(p => p.IdProvincia == provinciaId).FirstOrDefaultAsync();
        }

        public async Task<List<Canton>> ObtenerCantonesAsync(Guid provinciaId)
        {
            return await _context.Cantones.AsNoTracking()
                .Where(c => c.ProvinciaId == provinciaId).OrderBy(c => c.NombreCanton).ToListAsync();
        }

        public async Task<Canton?> ObtenerCantonPorIdAsync(Guid cantonId)
        {
            return await _context.Cantones.AsNoTracking().Include(c => c.Provincia)
                .Where(c => c.IdCanton == cantonId).FirstOrDefaultAsync();
        }

        public async Task<List<Parroquia>> ObtenerParroquiasAsync(Guid cantonId)
        {
            return await _context.Parroquias.AsNoTracking()
                .Where(p => p.CantonId == cantonId).OrderBy(p => p.NombreParroquia).ToListAsync();
        }
        public async Task<Parroquia?> ObtenerParroquiaPorIdAsync(Guid parroquiaId)
        {
            return await _context.Parroquias.AsNoTracking().Include(p => p.Canton)
                .Where(p => p.IdParroquia == parroquiaId).FirstOrDefaultAsync();
        }

        public async Task<List<Zona>> ObtenerZonasAsync(Guid parroquiaId)
        {
            return await _context.Zonas.AsNoTracking()
                .Where(z => z.ParroquiaId == parroquiaId).OrderBy(z => z.NombreZona).ToListAsync();
        }
        public async Task<Zona?> ObtenerZonaPorIdAsync(Guid zonaId)
        {
            return await _context.Zonas.AsNoTracking().Where(z => z.IdZona == zonaId).FirstOrDefaultAsync();
        }

    }
}
