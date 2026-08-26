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

        public IQueryable<Candidato> ObtenerQuery()
        {
            return _context.Candidatos.AsNoTracking();
        }
        public async Task<Candidato?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Candidatos.FirstOrDefaultAsync(x => x.IdCandidato == id);
        }
        public async Task AgregarAsync(Candidato candidato)
        {
            await _context.Candidatos.AddAsync(candidato);
        }

    }
}
