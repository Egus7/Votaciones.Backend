using Microsoft.EntityFrameworkCore;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Domain.Interfaces.IRepositories;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Application.Services.Votaciones
{
    public class ResultadoService : IResultadoService
    {
        private readonly IResultadoRepository _resultadoRepository;

        public ResultadoService(IResultadoRepository resultadoRepository)
        {
            _resultadoRepository = resultadoRepository;
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorEleccionAsync(Guid eleccionId, TipoCandidato tipoCandidato)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery().Where(x => x.EleccionId == eleccionId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery().Where(x => x.EleccionId == eleccionId 
                && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorCantonAsync(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.Parroquia!.CantonId == cantonId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.Zona!.Parroquia!.CantonId == cantonId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorParroquiaAsync(Guid eleccionId, Guid parroquiaId, TipoCandidato tipoCandidato)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.ParroquiaId == parroquiaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.Zona!.ParroquiaId == parroquiaId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorZonaAsync(Guid eleccionId, Guid zonaId, TipoCandidato tipoCandidato)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.ZonaId == zonaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.ZonaId == zonaId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato);
        }

        #region Metodos privados
        private async Task<ResultadoEleccionDTO> CalcularResultadosAsync(IQueryable<MesaElectoral> mesasQuery, 
            IQueryable<ActaEleccion> actasQuery, TipoCandidato tipoCandidato)
        {
            if (!Enum.IsDefined(typeof(TipoCandidato), tipoCandidato))
                throw new InvalidOperationException("El tipo de candidatura ingresada no es válida.");

            var resultado = new ResultadoEleccionDTO();

            // Total de mesas/actas existentes
            resultado.TotalMesas = await mesasQuery.CountAsync();
            // Estados de las actas
            resultado.MesasRegistradas = await actasQuery.CountAsync(x => x.Estado == EstadoActa.Registrada);
            resultado.MesasEnRevision = await actasQuery.CountAsync(x => x.Estado == EstadoActa.EnRevision);
            resultado.MesasConInconsistencia = await actasQuery.CountAsync(x => x.Estado == EstadoActa.ConInconsistencia);
            resultado.MesasValidadas = await actasQuery.CountAsync(x => x.Estado == EstadoActa.Validada);
            // Solamente actas validadas participan en el resultado electoral
            var actasValidadas = actasQuery.Where(x => x.Estado == EstadoActa.Validada);
            resultado.TotalVotosBlancos = await actasValidadas.SumAsync(x => x.VotosBlancos);
            resultado.TotalVotosNulos = await actasValidadas.SumAsync(x => x.VotosNulos);
            resultado.TotalVotos = await actasValidadas.SumAsync(x => x.TotalVotos);
            resultado.TotalVotosValidos = await actasValidadas.SelectMany(x => x.ActaDetalles).SumAsync(x => x.Votos);

            //resultados
            if (tipoCandidato == TipoCandidato.ConcejalUrbano || tipoCandidato == TipoCandidato.ConcejalRural)
            {
                resultado.Resultados = await ObtenerResultadosPorListaAsync(actasValidadas);
            }
            else
            {
                resultado.Resultados = await ObtenerResultadosPorCandidatoAsync(actasValidadas);
            }
            // Calcular porcentajes
            if (resultado.TotalVotosValidos > 0)
            {
                foreach (var item in resultado.Resultados)
                {
                    item.Porcentaje = Math.Round(item.Votos * 100m / resultado.TotalVotosValidos, 2);
                }
            }
            return resultado;
        }

        private async Task<List<ResultadoDetalleDTO>> ObtenerResultadosPorCandidatoAsync(IQueryable<ActaEleccion> actasValidadas)
        {
            return await actasValidadas
                .SelectMany(x => x.ActaDetalles)
                .Where(x => x.CandidatoId != null)
                .GroupBy(x => new
                {
                    x.CandidatoId,
                    x.Candidato!.NombreCandidato,
                    x.Candidato.ListaElectoralId,
                    x.Candidato.ListaElectoral!.NombreLista,
                    x.Candidato.ListaElectoral.NumeroLista
                })
                .Select(g => new ResultadoDetalleDTO
                {
                    CandidatoId = g.Key.CandidatoId,
                    Candidato = g.Key.NombreCandidato,
                    ListaElectoralId = g.Key.ListaElectoralId,
                    Lista = g.Key.NombreLista,
                    NumeroLista = g.Key.NumeroLista,
                    Votos = g.Sum(x => x.Votos)
                })
                .OrderByDescending(x => x.Votos)
                .ToListAsync();
        }

        private async Task<List<ResultadoDetalleDTO>> ObtenerResultadosPorListaAsync(IQueryable<ActaEleccion> actasValidadas)
        {
            return await actasValidadas
                .SelectMany(x => x.ActaDetalles)
                .Where(x => x.ListaElectoralId != null)
                .GroupBy(x => new
                {
                    x.ListaElectoralId,
                    x.ListaElectoral!.NombreLista,
                    x.ListaElectoral.NumeroLista
                })
                .Select(g => new ResultadoDetalleDTO
                {
                    CandidatoId = null,
                    Candidato = null,
                    ListaElectoralId = g.Key.ListaElectoralId,
                    Lista = g.Key.NombreLista,
                    NumeroLista = g.Key.NumeroLista,
                    Votos = g.Sum(x => x.Votos)
                })
                .OrderByDescending(x => x.Votos)
                .ToListAsync();
        }

        #endregion

    }
}
