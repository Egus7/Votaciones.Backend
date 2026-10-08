using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Infrastructure.Persistence.Repositories
{
    public class MesaElectoralRepository : IMesaElectoralRepository
    {
        private readonly AppDbContext _context;
        public MesaElectoralRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<MesaElectoral> ObtenerQuery()
        {
            return _context.MesasElectorales.AsNoTracking();
        }

        public async Task<MesaElectoral?> ObtenerPorIdAsync(Guid id)
        {
            return await _context.MesasElectorales.Include(x => x.Zona)
                .FirstOrDefaultAsync(x => x.IdMesaElectoral == id);
        }
        public async Task<int> ObtenerSiguienteNumeroMesaAsync(Guid eleccionId, Guid zonaId, TipoMesa tipoMesa)
        {
            var sufijo = tipoMesa switch
            {
                TipoMesa.Femenina => "F",
                TipoMesa.Masculina => "M",
                _ => throw new ArgumentException("El tipo de mesa no es válido.")
            };

            var codigos = await _context.MesasElectorales.Where(x => x.EleccionId == eleccionId && 
                    x.ZonaId == zonaId && x.CodigoMesa.EndsWith(sufijo))
                .Select(x => x.CodigoMesa).ToListAsync();

            if (codigos.Count == 0)
                return 1;

            var ultimoNumero = codigos.Select(x => int.Parse(x[..^1])).Max();
            return ultimoNumero + 1;
        }

        public async Task<MesaElectoral?> ObtenerPorCodigoMesaByEleccionAsync(string codigoMesa, Guid eleccionId, Guid zonaId, Guid? idMesaExcluir = null)
        {
            return await _context.MesasElectorales.AsNoTracking() // Recomendado para consultas de lectura/validación
                .Where(m => m.CodigoMesa == codigoMesa && m.EleccionId == eleccionId && m.ZonaId == zonaId
                    && (!idMesaExcluir.HasValue || m.IdMesaElectoral != idMesaExcluir.Value)).FirstOrDefaultAsync();
        }
        public async Task<Zona?> ObtenerZonaAsync(Guid zonaId)
        {
            return await _context.Zonas.AsNoTracking().Include(z => z.Parroquia)
                .Where(z => z.IdZona == zonaId).FirstOrDefaultAsync();
        }
        public async Task AgregarAsync(MesaElectoral mesaElectoral)
        {
            await _context.MesasElectorales.AddAsync(mesaElectoral);
        }

    }
}
