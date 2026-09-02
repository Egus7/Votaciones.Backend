using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class ListaElectoralRepository : IListaElectoralRepository
    {
        private readonly AppDbContext _context;

        public ListaElectoralRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<ListaElectoral> ObtenerQuery()
        {
            return _context.ListasElectorales.AsNoTracking();
        }

        public async Task<ListaElectoral?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.ListasElectorales.FirstOrDefaultAsync(x => x.IdListaElectoral == id);
        }
        public async Task<ListaElectoral?> ObtenerNumeroListaAsync(int nroLista, Guid? idListaExcluir = null)
        {
            return await _context.ListasElectorales.AsNoTracking().Where(z => z.NumeroLista == nroLista &&
            (!idListaExcluir.HasValue || z.IdListaElectoral != idListaExcluir.Value)).FirstOrDefaultAsync();
        }
        public async Task AgregarAsync(ListaElectoral listaElectoral)
        {
            await _context.ListasElectorales.AddAsync(listaElectoral);
        }

    }
}
