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
            return await _context.ActasEleccion.Include(x => x.ActaDetalles).ThenInclude(x => x.Candidato)
                .Include(x => x.ActaDetalles).ThenInclude(x => x.ListaElectoral)
                .FirstOrDefaultAsync(x => x.IdActa == id);
        }

        public async Task<ActaEleccion?> ObtenerPorMesaAsync(Guid mesaId, TipoCandidato tipoCandidato, Guid? idActaExcluir = null)
        {
            return await _context.ActasEleccion.AsNoTracking()
                .Where(x => x.MesaElectoralId == mesaId && x.TipoCandidato == tipoCandidato &&
                    (!idActaExcluir.HasValue || x.IdActa != idActaExcluir.Value)).FirstOrDefaultAsync();
        }

        public async Task<bool> ExistePorCandidatoAsync(Guid eleccionId, Guid candidatoId)
        {
            return await _context.ActasEleccion.Where(x => x.EleccionId == eleccionId)
                .AnyAsync(a => a.ActaDetalles.Any(d => d.CandidatoId == candidatoId));
        }
        public async Task<bool> ExistePorListaYAmbitoAsync(Guid eleccionId, Guid listaElectoralId, TipoCandidato tipoCandidato, 
            Guid? provinciaId, Guid? cantonId, Guid? parroquiaId)
        {
            var query = _context.ActasEleccion.Where(a => a.TipoCandidato == tipoCandidato && a.MesaElectoral != null && 
                a.MesaElectoral.EleccionId == eleccionId && a.ActaDetalles.Any(d => d.ListaElectoralId == listaElectoralId));

            // Provincia
            if (provinciaId.HasValue)
            {
                // solo obtiene por mesa electoral
                query = query.Where(a => a.MesaElectoral!.Zona!.Parroquia!.Canton!.Provincia!.IdProvincia == provinciaId);
            }
            // Cantón
            if (cantonId.HasValue)
            {
                query = query.Where(a => a.MesaElectoral!.Zona!.Parroquia!.Canton!.IdCanton == cantonId);
            }
            // Parroquia
            if (parroquiaId.HasValue)
            {
                query = query.Where(a => a.MesaElectoral!.Zona!.Parroquia!.IdParroquia == parroquiaId);
            }

            return await query.AnyAsync();
        }
        public async Task AgregarAsync(ActaEleccion acta)
        {
            await _context.ActasEleccion.AddAsync(acta);
        }

    }
}
