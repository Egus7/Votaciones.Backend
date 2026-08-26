using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class MesaElectoralRepository : IMesaElectoralRepository
    {
        private readonly AppDbContext _context;
        public MesaElectoralRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<MesaElectoral> ObtenerQuery()
        {
            return _context.MesasElectorales.AsNoTracking();
        }

        public async Task<MesaElectoral?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.MesasElectorales.FirstOrDefaultAsync(x => x.IdMesaElectoral == id);
        }
        public async Task<MesaElectoral?> ObtenerPorCodigoMesaByEleccionAsync(string codigoMesa, Guid eleccionId, Guid zonaId, Guid? idMesaExcluir = null)
        {
            return await _context.MesasElectorales.AsNoTracking() // Recomendado para consultas de lectura/validación
                .Where(m => m.CodigoMesa == codigoMesa && m.EleccionId == eleccionId && m.ZonaId == zonaId
                    && (!idMesaExcluir.HasValue || m.IdMesaElectoral != idMesaExcluir.Value)).FirstOrDefaultAsync();
        }
        public async Task<Zona?> ObtenerZonaAsync(Guid zonaId)
        {
            return await _context.Zonas.AsNoTracking().Where(z => z.IdZona == zonaId).FirstOrDefaultAsync();
        }
        public async Task AgregarAsync(MesaElectoral mesaElectoral)
        {
            await _context.MesasElectorales.AddAsync(mesaElectoral);
        }

    }
}
