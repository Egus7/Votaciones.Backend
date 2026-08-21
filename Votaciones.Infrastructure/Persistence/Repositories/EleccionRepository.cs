using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class EleccionRepository : IEleccionRepository
    {
        private readonly AppDbContext _dbContext;

        public EleccionRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Eleccion?> ObtenerPorIdAsync(Guid id)
        {
            return await _dbContext.Elecciones.FirstOrDefaultAsync(x => x.IdEleccion == id);
        }

        public async Task<IEnumerable<Eleccion>> ObtenerTodosAsync()
        {
            return await _dbContext.Elecciones.AsNoTracking()
                .OrderByDescending(x => x.FechaEleccion)
                .ToListAsync();
        }

        public async Task AgregarAsync(Eleccion eleccion)
        {
            await _dbContext.Elecciones.AddAsync(eleccion);
        }

    }
}
