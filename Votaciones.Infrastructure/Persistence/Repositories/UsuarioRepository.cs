using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _context;
        public UsuarioRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<AdmUsuario> ObtenerQuery()
        {
            return _context.Usuarios.AsNoTracking();
        }

        public async Task<AdmUsuario?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.IdUsuario == id);
        }

        public async Task<AdmUsuario?> ObtenerPorNombreUsuarioOEmailAsync(string identificador)
        {
            return await _context.Usuarios.AsNoTracking().Include(x => x.Rol)
                .FirstOrDefaultAsync(x => x.NombreUsuario == identificador || x.EmailUsuario == identificador);
        }

        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, Guid? idExcluir = null)
        {
            return await _context.Usuarios.AnyAsync(x => x.NombreUsuario == nombreUsuario && 
                (!idExcluir.HasValue || x.IdUsuario != idExcluir.Value));
        }

        public async Task<bool> ExisteEmailUsuarioAsync(string emailUsuario, Guid? idExcluir = null)
        {
            return await _context.Usuarios.AnyAsync(x => x.EmailUsuario == emailUsuario &&
                (!idExcluir.HasValue || x.IdUsuario != idExcluir.Value));
        }
        public async Task AgregarAsync(AdmUsuario usuario)
        {
            await _context.Usuarios.AddAsync(usuario);
        }

    }
}
