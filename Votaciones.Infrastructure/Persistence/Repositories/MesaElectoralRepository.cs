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

        public async Task<(IEnumerable<MesaElectoral> Items, int TotalRegistros)> ObtenerPaginacionAsync(int pagina, int pageSize)
        {
            var query = _context.MesasElectorales.AsNoTracking();

            // Conteo de total registros
            var totalRegistros = await query.CountAsync();

            // Consulta de paginacion 
            var items = await query
                .OrderBy(m => m.CodigoMesa)
                .Skip((pagina -1) * pageSize)
                .Take(pageSize)
                .AsQueryable()
                .ToListAsync();

            return (items, totalRegistros);
        }
        public async Task<MesaElectoral?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.MesasElectorales.FirstOrDefaultAsync(x => x.IdMesaElectoral == id);
        }
        public async Task<MesaElectoral?> ObtenerPorCodigoMesaByEleccionAsync(string codigoMesa, Guid eleccionId, Guid? idMesaExcluir = null)
        {
            return await _context.MesasElectorales.AsNoTracking() // Recomendado para consultas de lectura/validación
                .Where(m => m.CodigoMesa == codigoMesa && m.EleccionId == eleccionId 
                    && (!idMesaExcluir.HasValue || m.IdMesaElectoral != idMesaExcluir.Value)).FirstOrDefaultAsync();
        }
        public async Task<IEnumerable<MesaElectoral>> ObtenerPorEleccionAsync(Guid eleccionId)
        {
            return await _context.MesasElectorales.AsNoTracking().Where(m => m.EleccionId == eleccionId)
                .OrderBy(m => m.CodigoMesa).ToListAsync();
        }
        public async Task AgregarAsync(MesaElectoral mesaElectoral)
        {
            await _context.MesasElectorales.AddAsync(mesaElectoral);
        }

    }
}
