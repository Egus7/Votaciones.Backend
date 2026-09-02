using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class ActaRepository : IActaRepository
    {
        private readonly AppDbContext _context;

        public ActaRepository(AppDbContext context)
        {
            _context = context;
        }
        public IQueryable<ActaEleccion> ObtenerQuery()
        {
            return _context.ActasEleccion.AsNoTracking();
        }

        public async Task<ActaEleccion?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.ActasEleccion.Include(x => x.ActaDetalles)
                .ThenInclude(x => x.Candidato).FirstOrDefaultAsync(x => x.IdActa == id);
        }

        public async Task<ActaEleccion?> ObtenerPorMesaAsync(Guid mesaId, TipoCandidato tipoCandidato, Guid? idActaExcluir = null)
        {
            return await _context.ActasEleccion.AsNoTracking()
                .Where(x => x.MesaElectoralId == mesaId && x.TipoCandidato == tipoCandidato &&
                    (!idActaExcluir.HasValue || x.IdActa != idActaExcluir.Value)).FirstOrDefaultAsync();
        }

        public async Task AgregarAsync(ActaEleccion acta)
        {
            await _context.ActasEleccion.AddAsync(acta);
        }

    }
}
