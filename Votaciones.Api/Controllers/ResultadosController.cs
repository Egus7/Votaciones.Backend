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

    }
}
