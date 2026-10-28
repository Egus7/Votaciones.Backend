using ClosedXML.Excel;
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
        private const int TotalEscanos = 5;
        private readonly IResultadoRepository _resultadoRepository;
        private readonly IEleccionRepository _eleccionRepository;
        private readonly ICandidatoRepository _candidatoRepository;

        public ResultadoService(IResultadoRepository resultadoRepository, IEleccionRepository eleccionRepository, ICandidatoRepository candidatoRepository)
        {
            _resultadoRepository = resultadoRepository;
            _eleccionRepository = eleccionRepository;
            _candidatoRepository = candidatoRepository;
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorEleccionAsync(Guid eleccionId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            //validar que sea valido para el ambito nacional
            ValidarAmbito(tipoCandidato, "Nacional");

            var mesasQuery = _resultadoRepository.ObtenerMesasQuery().Where(x => x.EleccionId == eleccionId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery().Where(x => x.EleccionId == eleccionId 
                && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorProvinciaAsync(Guid eleccionId, Guid provinciaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            //validar que sea valido para el ambito provincia
            ValidarAmbito(tipoCandidato, "Provincia");

            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.Parroquia!.Canton!.ProvinciaId == provinciaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId
                    && x.TipoCandidato == tipoCandidato && x.MesaElectoral!.Zona!.Parroquia!.Canton!.ProvinciaId == provinciaId);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorCantonAsync(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            //validar que sea valido para el ambito canton
            ValidarAmbito(tipoCandidato, "Canton");

            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.Parroquia!.CantonId == cantonId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.Zona!.Parroquia!.CantonId == cantonId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorParroquiaAsync(Guid eleccionId, Guid parroquiaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            //validar que sea valido para el ambito parroquia
            ValidarAmbito(tipoCandidato, "Parroquia");

            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.Zona!.ParroquiaId == parroquiaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.Zona!.ParroquiaId == parroquiaId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorZonaAsync(Guid eleccionId, Guid zonaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            //validar que sea valido para el ambito zona
            ValidarAmbito(tipoCandidato, "Zona");

            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.ZonaId == zonaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoral!.ZonaId == zonaId 
                    && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<ResultadoEleccionDTO> ObtenerPorMesaAsync(Guid eleccionId, Guid mesaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(x => x.EleccionId == eleccionId && x.IdMesaElectoral == mesaId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(x => x.EleccionId == eleccionId && x.MesaElectoralId == mesaId && x.TipoCandidato == tipoCandidato);

            return await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);
        }

        public async Task<AsignacionConcejalesDTO> ObtenerAsignacionConcejalesAsync(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato,
                TipoResultado tipoResultado)
        {
            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(eleccionId) 
                ?? throw new InvalidOperationException("La elección no existe.");

            if (tipoCandidato != TipoCandidato.ConcejalUrbano && tipoCandidato != TipoCandidato.ConcejalRural)
                throw new InvalidOperationException("La asignación de escaños está habilitada solo para Concejales.");

            var mesasQuery = _resultadoRepository.ObtenerMesasQuery()
                .Where(m => m.EleccionId == eleccionId && m.Zona!.Parroquia!.CantonId == cantonId);

            var actasQuery = _resultadoRepository.ObtenerActasQuery()
                .Where(a => a.EleccionId == eleccionId && a.TipoCandidato == tipoCandidato &&
                        a.MesaElectoral!.Zona!.Parroquia!.CantonId == cantonId);
            // Reutiliza los filtros, totales y agrupación existentes.
            var resultado = await CalcularResultadosAsync(mesasQuery, actasQuery, tipoCandidato, tipoResultado);

            // Generar cocientes según el método configurado en la elección.
            var cocientes = new List<(Guid ListaId, int NumeroLista, double Cociente)>();
            foreach (var lista in resultado.Resultados)
            {
                if (lista.ListaElectoralId == Guid.Empty || lista.Votos <= 0)
                    continue;

                for (int i = 1; i <= TotalEscanos; i++)
                {
                    int divisor = eleccion.MetodoEscanos == MetodoAsignacionEscanos.Webster 
                        ? (2 * i) - 1 : i;

                    cocientes.Add((lista.ListaElectoralId, lista.NumeroLista, (double)lista.Votos / divisor));
                }
            }

            var ganadores = cocientes.OrderByDescending(c => c.Cociente)
                .ThenBy(c => c.NumeroLista)
                .Take(TotalEscanos)
                .ToList();

            var escanosPorLista = ganadores
                .GroupBy(c => c.ListaId).ToDictionary(g => g.Key, g => g.Count());

            var candidatos = await _resultadoRepository.ObtenerCandidatosQuery().AsNoTracking()
                .Where(c => c.EleccionId == eleccionId && c.TipoCandidato == tipoCandidato && 
                    c.CantonId == cantonId && c.Activo)
                .SelectMany(c => c.ListaCandidatos.Where(lc => lc.ListaPrincipal).Select(lc => new
                {
                    c.IdCandidato, c.NombreCandidato, c.Orden, lc.ListaElectoralId
                }))
                .ToListAsync();
            
            var listasDTO = resultado.Resultados.Where(r => r.ListaElectoralId != Guid.Empty)
                .Select(lista =>
                {
                    var listaId = lista.ListaElectoralId!;
                    var escanos = escanosPorLista.GetValueOrDefault(listaId);
                    var candidatosLista = candidatos.Where(c => c.ListaElectoralId == listaId)
                        .GroupBy(c => c.IdCandidato).Select(g => g.First())
                        .OrderBy(c => c.Orden ?? int.MaxValue).ThenBy(c => c.NombreCandidato)
                        .ToList();

                    return new AsignacionListaDTO
                    {
                        ListaElectoralId = listaId,
                        NumeroLista = lista.NumeroLista,
                        NumerosLista = lista.NumerosLista,
                        NombreLista = lista.Lista ?? string.Empty,
                        Votos = lista.Votos,
                        EscanosAsignados = escanos,
                        Candidatos = candidatosLista
                         .Select((c, index) => new AsignacionCandidatoDTO
                         {
                             CandidatoId = c.IdCandidato,
                             NombreCandidato = c.NombreCandidato,
                             Orden = c.Orden ?? index + 1,
                             Electo = index < escanos
                         })
                         .ToList()
                    };
                }).ToList();

            return new AsignacionConcejalesDTO
            {
                EleccionId = eleccionId,
                NombreEleccion = eleccion.NombreEleccion,
                Dignidad = tipoCandidato.ToString(),
                EstadoResultado = tipoResultado.ToString(),
                TotalEscanos = TotalEscanos,
                TotalListas = listasDTO.Count,
                Listas = listasDTO
            };
        }


        public async Task<byte[]> ObtenerAsignacionConcejalesExcelAsync(Guid eleccionId, Guid cantonId, 
            TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var asignacion = await ObtenerAsignacionConcejalesAsync(eleccionId, cantonId, tipoCandidato, tipoResultado);

            var eleccion = await _eleccionRepository.ObtenerPorIdAsync(eleccionId)
                ?? throw new InvalidOperationException("La elección no existe.");

            using var workbook = new XLWorkbook();
            // Todo el reporte estará en una sola hoja.
            var hoja = workbook.Worksheets.Add("Asignacion de escanos");

            // Encabezado general.
            hoja.Cell(1, 1).Value = "ASIGNACIÓN DE ESCAÑOS — CONCEJALES";
            hoja.Range(1, 1, 1, 9).Merge();
            hoja.Cell(2, 1).Value = "Elección";
            hoja.Cell(2, 2).Value = asignacion.NombreEleccion;
            hoja.Cell(3, 1).Value = "Dignidad";
            hoja.Cell(3, 2).Value = asignacion.Dignidad;
            hoja.Cell(4, 1).Value = "Tipo de resultado";
            hoja.Cell(4, 2).Value = asignacion.EstadoResultado;
            hoja.Cell(5, 1).Value = "Método de asignación";
            hoja.Cell(5, 2).Value = eleccion.MetodoEscanos.ToString();
            hoja.Cell(6, 1).Value = "Total de escaños";
            hoja.Cell(6, 2).Value = asignacion.TotalEscanos;

            // Construir todos los cocientes.
            var cocientes = new List<(int NumeroLista, string NombreLista, Guid ListaId, int Votos, int Divisor, 
                decimal Cociente, int OrdenCandidato, string? NombreCandidato)>();

            foreach (var lista in asignacion.Listas.OrderBy(x => x.NumeroLista))
            {
                if (lista.Votos <= 0)
                    continue;
                // Cada cociente corresponde al siguiente candidato de la lista.
                var candidatosOrdenados = lista.Candidatos.OrderBy(c => c.Orden).ToList();

                for (int i = 1; i <= asignacion.TotalEscanos; i++)
                {
                    int divisor = eleccion.MetodoEscanos switch
                    {
                        MetodoAsignacionEscanos.Webster => (2 * i) - 1, MetodoAsignacionEscanos.DHondt => i,
                        _ => throw new InvalidOperationException("El método de asignación de escaños no es válido.")
                    };

                    var candidato = candidatosOrdenados.ElementAtOrDefault(i - 1);

                    cocientes.Add((lista.NumeroLista, lista.NombreLista, lista.ListaElectoralId, lista.Votos, divisor, 
                        (decimal)lista.Votos / divisor, i, candidato?.NombreCandidato));
                }
            }

            // El orden debe coincidir con el utilizado en la asignación.
            var cocientesOrdenados = cocientes.OrderByDescending(c => c.Cociente).ThenBy(c => c.NumeroLista).ToList();

            // Tabla detallada: una fila por cada cociente calculado.
            int filaEncabezado = 8;
            hoja.Cell(filaEncabezado, 1).Value = "N.º Lista";
            hoja.Cell(filaEncabezado, 2).Value = "Lista";
            hoja.Cell(filaEncabezado, 3).Value = "Votos";
            hoja.Cell(filaEncabezado, 4).Value = "Divisor";
            hoja.Cell(filaEncabezado, 5).Value = "Cociente";
            hoja.Cell(filaEncabezado, 6).Value = "Orden global";
            hoja.Cell(filaEncabezado, 7).Value = "Candidato correspondiente";
            hoja.Cell(filaEncabezado, 8).Value = "Asignación del escaño";
            hoja.Cell(filaEncabezado, 9).Value = "Estado del candidato";

            int fila = filaEncabezado + 1;

            for (int i = 0; i < cocientesOrdenados.Count; i++)
            {
                var item = cocientesOrdenados[i];
                // Los primeros cinco cocientes reciben los escaños.
                bool cocienteGanador = i < asignacion.TotalEscanos;

                hoja.Cell(fila, 1).Value = item.NumeroLista;
                hoja.Cell(fila, 2).Value = item.NombreLista;
                hoja.Cell(fila, 3).Value = item.Votos;
                hoja.Cell(fila, 4).Value = item.Divisor;
                hoja.Cell(fila, 5).Value = item.Cociente;
                hoja.Cell(fila, 6).Value = i + 1;
                hoja.Cell(fila, 7).Value = item.NombreCandidato ?? "Sin candidato disponible";
                hoja.Cell(fila, 8).Value = cocienteGanador ? $"Escaño {i + 1}" : "No asignado";
                hoja.Cell(fila, 9).Value = cocienteGanador ? item.NombreCandidato != null ? "Electo" : "Revisar candidato"
                    : "No electo";
                // Resaltar los cocientes que ganaron un escaño.
                if (cocienteGanador)
                {
                    hoja.Range(fila, 1, fila, 9).Style.Font.Bold = true;
                }
                fila++;
            }

            // Formato del título.
            hoja.Range(1, 1, 1, 9).Style.Font.Bold = true;
            hoja.Range(1, 1, 1, 9).Style.Font.FontSize = 14;
            hoja.Range(1, 1, 1, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            // Formato de los encabezados de la tabla.
            var encabezado = hoja.Range(filaEncabezado, 1, filaEncabezado, 9);
            encabezado.Style.Font.Bold = true;
            encabezado.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            // Formatos numéricos.
            if (fila > filaEncabezado + 1)
            {
                hoja.Range(filaEncabezado + 1, 3, fila - 1, 4).Style.NumberFormat.Format = "#,##0";
                hoja.Range(filaEncabezado + 1, 5, fila - 1, 5).Style.NumberFormat.Format = "#,##0.00";
            }
            // Bordes de la tabla.
            if (fila > filaEncabezado + 1)
            {
                var tabla = hoja.Range(filaEncabezado, 1, fila - 1, 9);
                tabla.Style.Border.OutsideBorder =XLBorderStyleValues.Thin;
                tabla.Style.Border.InsideBorder =XLBorderStyleValues.Thin;
            }
            
            hoja.SheetView.FreezeRows(filaEncabezado);
            hoja.Columns().AdjustToContents();

            // Evitar columnas excesivamente anchas.
            hoja.Column(2).Width = 25;
            hoja.Column(7).Width = 32;
            hoja.Column(8).Width = 22;
            hoja.Column(9).Width = 24;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }


        #region Metodos privados
        private static void ValidarAmbito(TipoCandidato tipoCandidato, string nivelGeografico)
        {
            if (!Enum.IsDefined(typeof(TipoCandidato), tipoCandidato))
                throw new InvalidOperationException("El tipo de candidatura ingresado no es válido.");

            bool permitido = nivelGeografico switch
            {
                "Nacional" => tipoCandidato == TipoCandidato.Presidente,
                "Provincia" => tipoCandidato is TipoCandidato.Presidente or TipoCandidato.Prefecto,
                "Canton" =>
                    tipoCandidato is TipoCandidato.Presidente or TipoCandidato.Prefecto 
                        or TipoCandidato.Alcalde or TipoCandidato.ConcejalUrbano or TipoCandidato.ConcejalRural,
                "Parroquia" or "Zona" =>
                    tipoCandidato is TipoCandidato.Presidente or TipoCandidato.Prefecto
                        or TipoCandidato.Alcalde or TipoCandidato.ConcejalUrbano or TipoCandidato.ConcejalRural,

                _ => false
            };

            if (!permitido)
            {
                throw new InvalidOperationException($"La dignidad {tipoCandidato} no permite consultar resultados a nivel {nivelGeografico}.");
            }
        }

        private async Task<ResultadoEleccionDTO> CalcularResultadosAsync(IQueryable<MesaElectoral> mesasQuery, 
            IQueryable<ActaEleccion> actasQuery, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {

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
                    actasResultado = actasQuery.Where(x => x.Estado == EstadoActa.Registrada || x.Estado == EstadoActa.EnRevision || 
                        x.Estado == EstadoActa.Validada);
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
            var resultados = await actasValidadas
                .SelectMany(x => x.ActaDetalles)
                .Where(x => x.CandidatoId != null)
                .GroupBy(x => new { x.CandidatoId, x.Candidato!.NombreCandidato })
                .Select(g => new ResultadoDetalleDTO
                {
                    CandidatoId = g.Key.CandidatoId,
                    Candidato = g.Key.NombreCandidato,
                    Votos = g.Sum(x => x.Votos)
                })
                .ToListAsync();

            await CompletarListasCandidatosAsync(resultados);

            return resultados.OrderByDescending(x => x.Votos).ToList();
        }

        private async Task<List<ResultadoDetalleDTO>> ObtenerResultadosPorListaAsync(IQueryable<ActaEleccion> actasValidadas)
        {
            var resultados = await actasValidadas
                .SelectMany(x => x.ActaDetalles)   
                .Where(x => x.ListaElectoralId != Guid.Empty)
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
                    NumerosLista = g.Key.NumeroLista.ToString(),
                    Votos = g.Sum(x => x.Votos)
                })
                .ToListAsync();

            await CompletarAlianzasPorListaAsync(resultados);

            return resultados.OrderByDescending(x => x.Votos).ToList();
        }

        private async Task CompletarListasCandidatosAsync(List<ResultadoDetalleDTO> resultados)
        {
            var candidatoIds = resultados
                .Where(x => x.CandidatoId.HasValue)
                .Select(x => x.CandidatoId!.Value)
                .Distinct()
                .ToList();

            if (candidatoIds.Count == 0)
                return;

            var relaciones = await _candidatoRepository
                .ObtenerQuery()
                .Where(x => candidatoIds.Contains(x.IdCandidato))
                .SelectMany(x => x.ListaCandidatos)
                .Select(x => new
                {
                    x.CandidatoId,
                    x.ListaElectoralId,
                    x.ListaPrincipal,
                    x.ListaElectoral!.NumeroLista,
                    x.ListaElectoral.NombreLista
                })
                .ToListAsync();

            foreach (var resultado in resultados)
            {
                if (!resultado.CandidatoId.HasValue)
                    continue;

                var listas = relaciones
                    .Where(x => x.CandidatoId == resultado.CandidatoId.Value)
                    .GroupBy(x => x.ListaElectoralId)
                    .Select(g => g.First())
                    .OrderByDescending(x => x.ListaPrincipal)
                    .ThenBy(x => x.NumeroLista)
                    .ToList();

                if (listas.Count == 0)
                    continue;

                // Conservar la lista principal como identificador del resultado.
                var listaPrincipal = listas.FirstOrDefault(x => x.ListaPrincipal) ?? listas.First();

                resultado.ListaElectoralId = listaPrincipal.ListaElectoralId;
                resultado.NumeroLista = listaPrincipal.NumeroLista;
                // Mostrar los números de todas las listas aliadas.
                resultado.NumerosLista = string.Join(" - ", listas.Select(x => x.NumeroLista));
                // Mostrar los nombres de todas las listas aliadas.
                resultado.Lista = string.Join(" / ", listas
                    .Select(x => x.NombreLista)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct());
            }
        }

        private async Task CompletarAlianzasPorListaAsync(List<ResultadoDetalleDTO> resultados)
        {
            var listaIds = resultados
                .Select(x => x.ListaElectoralId)
                .Distinct()
                .ToList();

            if (listaIds.Count == 0)
                return;

            var relaciones = await _candidatoRepository
                .ObtenerQuery()
                .SelectMany(x => x.ListaCandidatos)
                .Select(x => new
                {
                    x.CandidatoId,
                    x.ListaElectoralId,
                    x.ListaPrincipal,
                    x.ListaElectoral!.NumeroLista,
                    x.ListaElectoral.NombreLista
                })
                .ToListAsync();

            // Obtener las listas asociadas a cada candidato.
            var listasPorCandidato = relaciones
                .GroupBy(x => x.CandidatoId)
                .ToList();

            // Para cada lista, identificar las listas aliadas a través de los candidatos relacionados.
            var alianzasPorLista = listasPorCandidato
                .Where(g => g.Select(x => x.ListaElectoralId).Distinct().Count() > 1)
                .SelectMany(g =>
                {
                    var listas = g
                        .GroupBy(x => x.ListaElectoralId)
                        .Select(x => x.First())
                        .ToList();

                    return listas.Select(lista => new
                    {
                        ListaOrigenId = lista.ListaElectoralId,
                        ListasAliadas = listas
                    });
                })
                .GroupBy(x => x.ListaOrigenId)
                .ToDictionary(g => g.Key, g => g.SelectMany(x => x.ListasAliadas)
                .GroupBy(x => x.ListaElectoralId)
                .Select(x => x.First())
                .OrderByDescending(x => x.ListaPrincipal)
                .ThenBy(x => x.NumeroLista)
                .ToList());

            foreach (var resultado in resultados)
            {
                if (!alianzasPorLista.TryGetValue(resultado.ListaElectoralId, out var listasAlianza))
                {
                    continue;
                }

                resultado.NumerosLista = string.Join(" - ", listasAlianza.Select(x => x.NumeroLista));

                resultado.Lista = string.Join(" / ", listasAlianza
                    .Select(x => x.NombreLista)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct());
            }
        }

        #endregion

    }
}
