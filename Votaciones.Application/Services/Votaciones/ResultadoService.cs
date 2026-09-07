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

        public async Task<ResultadoEleccionDTO> ObtenerPorEleccionAsync(Guid eleccionId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery().Where(x => x.EleccionId == eleccionId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery().Where(x => x.EleccionId == eleccionId 
                && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorCantonAsync(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.Parroquia!.CantonId == cantonId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.Zona!.Parroquia!.CantonId == cantonId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorParroquiaAsync(Guid eleccionId, Guid parroquiaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.ParroquiaId == parroquiaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.Zona!.ParroquiaId == parroquiaId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorZonaAsync(Guid eleccionId, Guid zonaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.ZonaId == zonaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.ZonaId == zonaId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        #region Metodos privados
        private async Task<ResultadoEleccionDTO> CalcularResultadosAsync(IQueryable<MesaElectoral> mesasQuery, 
            IQueryable<ActaEleccion> actasQuery, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
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
            // Actas que participan en el resultado
            IQueryable<ActaEleccion> actasResultado;
            switch (tipoResultado)
            {
                case TipoResultado.Oficial:
                    actasResultado = actasQuery.Where(x => x.Estado == EstadoActa.Validada);
                    break;
                case TipoResultado.Preliminar:
                    actasResultado = actasQuery.Where(x => x.Estado == EstadoActa.Registrada || x.Estado == EstadoActa.Validada);
                    break;
                default:
                    throw new InvalidOperationException("El tipo de resultado ingresado no es válido.");
            }
            // Totales
            resultado.TotalVotosBlancos = await actasResultado.SumAsync(x => x.VotosBlancos);
            resultado.TotalVotosNulos = await actasResultado.SumAsync(x => x.VotosNulos);
            resultado.TotalVotos = await actasResultado.SumAsync(x => x.TotalVotos);
            resultado.TotalVotosValidos = await actasResultado.SelectMany(x => x.ActaDetalles).SumAsync(x => x.Votos);

            //resultados
            if (tipoCandidato == TipoCandidato.ConcejalUrbano || tipoCandidato == TipoCandidato.ConcejalRural)
            {
                resultado.Resultados = await ObtenerResultadosPorListaAsync(actasResultado);
            }
            else
            {
                resultado.Resultados = await ObtenerResultadosPorCandidatoAsync(actasResultado);
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
                    x.ListaElectoralId,
                    x.ListaElectoral!.NombreLista,
                    x.ListaElectoral.NumeroLista
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
