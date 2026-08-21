using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class CandidatoRepository : ICandidatoRepository
    {
        private readonly AppDbContext _context;
        public CandidatoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Candidato>> ObtenerTodosAsync()
        {
            return await _context.Candidatos.AsNoTracking()
                .OrderBy(x => x.NumeroLista).ToListAsync();
        }
        public async Task<Candidato?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Candidatos.FirstOrDefaultAsync(x => x.IdCandidato == id);
        }
        public async Task<IEnumerable<Candidato>> ObtenerPorEleccionAsync(Guid eleccionId)
        {
            return await _context.Candidatos.AsNoTracking()
                .Where(x => x.EleccionId == eleccionId)
                .OrderBy(x => x.NumeroLista).ToListAsync();
        }
        public async Task AgregarAsync(Candidato candidato)
        {
            await _context.Candidatos.AddAsync(candidato);
        }

    }
}
