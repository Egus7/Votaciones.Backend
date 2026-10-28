using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Security;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ResultadosController : ControllerBase
    {
        private readonly IResultadoService _resultadoService;
        public ResultadosController(IResultadoService resultadoService)
        {
            _resultadoService = resultadoService;
        }

        // GET: api/Resultados/eleccion
        [HttpGet("eleccion")]
        [Authorize(Policy = RolPermisos.ResultadosView)]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorEleccion(Guid eleccionId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var resultado = await _resultadoService.ObtenerPorEleccionAsync(eleccionId, tipoCandidato, tipoResultado);

            if (resultado == null)
                return NotFound("No se encontraron resultados para la elección.");

            return Ok(resultado);
        }

        // GET: api/Resultados/eleccion/provincia
        [HttpGet("eleccion/provincia")]
        [Authorize(Policy = RolPermisos.ResultadosView)]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorProvincia(Guid eleccionId, Guid provinciaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var resultado = await _resultadoService.ObtenerPorProvinciaAsync(eleccionId, provinciaId, tipoCandidato, tipoResultado);
            return Ok(resultado);
        }

        // GET: api/Resultados/eleccion/canton
        [HttpGet("eleccion/canton")]
        [Authorize(Policy = RolPermisos.ResultadosView)]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorCanton(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var resultado = await _resultadoService.ObtenerPorCantonAsync(eleccionId, cantonId, tipoCandidato, tipoResultado);

            return Ok(resultado);
        }

        // GET: api/Resultados/eleccion/parroquia
        [HttpGet("eleccion/parroquia")]
        [Authorize(Policy = RolPermisos.ResultadosView)]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorParroquia(Guid eleccionId, Guid parroquiaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var resultado = await _resultadoService.ObtenerPorParroquiaAsync(eleccionId, parroquiaId, tipoCandidato, tipoResultado);

            return Ok(resultado);
        }

        // GET: api/Resultados/eleccion/zona
        [HttpGet("eleccion/zona")]
        [Authorize(Policy = RolPermisos.ResultadosView)]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorZona(Guid eleccionId, Guid zonaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var resultado = await _resultadoService.ObtenerPorZonaAsync(eleccionId, zonaId, tipoCandidato, tipoResultado);

            return Ok(resultado);
        }

        // GET: api/Resultados/eleccion/mesa
        [HttpGet("eleccion/mesa")]
        [Authorize(Policy = RolPermisos.ResultadosView)]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorMesa(Guid eleccionId, Guid mesaId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var resultado = await _resultadoService.ObtenerPorMesaAsync(eleccionId, mesaId, tipoCandidato, tipoResultado);

            return Ok(resultado);
        }

        // GET: api/Resultados/asignacionEscanos
        [HttpGet("asignacionEscanos")]
        [Authorize(Policy = RolPermisos.ResultadosView)]
        public async Task<ActionResult<AsignacionConcejalesDTO>> ObtenerConcejalesEscanos(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var resultado = await _resultadoService.ObtenerAsignacionConcejalesAsync(eleccionId, cantonId, tipoCandidato, tipoResultado);

            return Ok(resultado);
        }


        [HttpGet("asignacion-concejales/excel")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType<Stream>(StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportarAsignacionConcejalesExcel(Guid eleccionId, Guid cantonId, TipoCandidato tipoCandidato, TipoResultado tipoResultado)
        {
            var archivo = await _resultadoService.ObtenerAsignacionConcejalesExcelAsync(eleccionId, cantonId, tipoCandidato, tipoResultado);

            var nombreArchivo = $"Asignacion_Concejales_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(archivo, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }

    }
}
