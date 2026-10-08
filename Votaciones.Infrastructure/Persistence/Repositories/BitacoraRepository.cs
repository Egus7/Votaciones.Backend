using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class BitacoraRepository : IBitacoraRepository
    {
        private readonly AppDbContext _context;

        public BitacoraRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<AdmBitacora> ObtenerQuery()
        {
            return _context.Bitacora.AsNoTracking();
        }

        public async Task<List<AdmBitacora>> ObtenerPorRegistroAsync(string tabla, string idRegistro)
        {
            return await _context.Bitacora.AsNoTracking()
                .Where(x => x.Tabla == tabla && x.IdRegistro == idRegistro)
                .OrderByDescending(x => x.FechaRegistro).ToListAsync();
        }

        public async Task AgregarAsync(AdmBitacora bitacora)
        {
            await _context.Bitacora.AddAsync(bitacora);
        }

    }
}
