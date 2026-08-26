using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class ResultadoRepository : IResultadoRepository
    {
        private readonly AppDbContext _context;
        public ResultadoRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<ActaEleccion> ObtenerActasQuery()
        {
            return _context.ActasEleccion.AsNoTracking();
        }

        public IQueryable<MesaElectoral> ObtenerMesasQuery()
        {
            return _context.MesasElectorales.AsNoTracking();
        }

    }
}
