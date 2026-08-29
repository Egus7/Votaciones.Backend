using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class RolRepository : IRolRepository
    {
        private readonly AppDbContext _context;
        public RolRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<AdmRol> ObtenerQuery()
        {
            return _context.Roles.AsNoTracking();
        }

        public async Task<AdmRol?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Roles.FirstOrDefaultAsync(x => x.IdRol == id);
        }

        public async Task<AdmRol?> ObtenerPorNombreAsync(string nombreRol)
        {
            return await _context.Roles.FirstOrDefaultAsync(x => x.NombreRol == nombreRol);
        }

        public async Task<bool> ExisteNombreAsync(string nombreRol, Guid? idExcluir = null)
        {
            return await _context.Roles.AnyAsync(x => x.NombreRol == nombreRol && 
                (!idExcluir.HasValue || x.IdRol != idExcluir.Value));
        }

        public async Task AgregarAsync(AdmRol rol)
        {
            await _context.Roles.AddAsync(rol);
        }

    }
}
