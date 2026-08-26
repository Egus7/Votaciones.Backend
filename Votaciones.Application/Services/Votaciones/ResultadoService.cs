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

        public async Task<ResultadoEleccionDTO> ObtenerPorEleccionAsync(Guid eleccionId)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery().Where(x => x.EleccionId == eleccionId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery().Where(x => x.EleccionId == eleccionId);

            return await CalcularResultadosAsync(mesasQuery, actasQuery);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorCantonAsync(Guid eleccionId, Guid cantonId)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.Parroquia!.CantonId == cantonId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.Zona!.Parroquia!.CantonId == cantonId);

            return await CalcularResultadosAsync(mesasQuery, actasQuery);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorParroquiaAsync(Guid eleccionId, Guid parroquiaId)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.ParroquiaId == parroquiaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.Zona!.ParroquiaId == parroquiaId);

            return await CalcularResultadosAsync(mesasQuery, actasQuery);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorZonaAsync(Guid eleccionId, Guid zonaId)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.ZonaId == zonaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.ZonaId == zonaId);

            return await CalcularResultadosAsync(mesasQuery, actasQuery);

        }

        #region Metodos privados
        private async Task<ResultadoEleccionDTO> CalcularResultadosAsync(IQueryable<MesaElectoral> mesasQuery, 
            IQueryable<ActaEleccion> actasQuery)
        {
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

            // Agrupar votos por candidato
            resultado.Resultados = await actasValidadas.SelectMany(x => x.ActaDetalles)
                .GroupBy(x => new
                {
                    x.CandidatoId, x.Candidato!.NombreCandidato, x.Candidato.Lista, x.Candidato.NumeroLista
                })
                .Select(g => new ResultadoCandidatoDTO
                {
                    CandidatoId = g.Key.CandidatoId,
                    Candidato = g.Key.NombreCandidato,
                    Lista = g.Key.Lista ?? string.Empty,
                    NumeroLista = g.Key.NumeroLista,
                    Votos = g.Sum(x => x.Votos)
                })
                .OrderByDescending(x => x.Votos).ToListAsync();

            // Calcular porcentajes
            if (resultado.TotalVotosValidos > 0)
            {
                foreach (var candidato in resultado.Resultados)
                {
                    candidato.Porcentaje = Math.Round(candidato.Votos * 100m / resultado.TotalVotosValidos, 2);
                }
            }
            return resultado;
        }

        #endregion


    }
}
