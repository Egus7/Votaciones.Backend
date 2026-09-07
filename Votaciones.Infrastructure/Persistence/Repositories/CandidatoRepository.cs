using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using Votaciones.Infrastructure.Data;
using static Votaciones.Domain.Enums.EnumsEleccion;

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
            var candidato = await _context.Candidatos.Include(x => x.ListaCandidatos)
                .ThenInclude(x => x.ListaElectoral).FirstOrDefaultAsync(x => x.IdCandidato == id);

            if (candidato != null)
            {
                candidato.ListaCandidatos = candidato.ListaCandidatos
                    .OrderByDescending(x => x.ListaPrincipal)
                    .ThenBy(x => x.ListaElectoral!.NumeroLista).ToList();
            }

            return candidato;
        }

        public async Task<bool> ExistePorEleccionListaTipoAsync(Guid eleccionId, Guid listaId, TipoCandidato tipoCandidato, Guid? excluirCandidatoId = null)
        {
            return await _context.Candidatos.AnyAsync(x => x.EleccionId == eleccionId && x.TipoCandidato == tipoCandidato &&
                    x.Activo && (!excluirCandidatoId.HasValue || x.IdCandidato != excluirCandidatoId.Value) && 
                    x.ListaCandidatos.Any(lc => lc.ListaElectoralId == listaId));
        }
        public async Task<bool> ExisteOrdenPorEleccionListaTipoAsync(Guid eleccionId, Guid listaId, TipoCandidato tipoCandidato, int orden, Guid? excluirCandidatoId = null)
        {
            return await _context.Candidatos.AnyAsync(x => x.EleccionId == eleccionId && x.TipoCandidato == tipoCandidato &&
                    x.Orden == orden && x.Activo && (!excluirCandidatoId.HasValue || x.IdCandidato != excluirCandidatoId.Value) &&
                    x.ListaCandidatos.Any(lc => lc.ListaElectoralId == listaId));
        }
        public async Task<bool> ExisteListaPrincipalPorEleccionTipoAsync(Guid eleccionId, Guid listaElectoralId, TipoCandidato tipoCandidato)
        {
            return await _context.ListaCandidatos.AnyAsync(x => x.ListaElectoralId == listaElectoralId &&
                    x.ListaPrincipal && x.Candidato!.EleccionId == eleccionId && 
                    x.Candidato.TipoCandidato == tipoCandidato && x.Candidato.Activo);
        }
        public async Task AgregarAsync(Candidato candidato)
        {
            await _context.Candidatos.AddAsync(candidato);
        }

        public async Task AgregarListaCandidatoAsync(ListaCandidato listaCandidato)
        {
            await _context.ListaCandidatos.AddAsync(listaCandidato);
        }

        public void EliminarListaCandidatos(IEnumerable<ListaCandidato> listaCandidatos)
        {
            _context.ListaCandidatos.RemoveRange(listaCandidatos);
        }

    }
}
