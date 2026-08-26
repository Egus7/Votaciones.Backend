using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Interfaces.IServices;

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultadosController : ControllerBase
    {
        private readonly IResultadoService _resultadoService;
        public ResultadosController(IResultadoService resultadoService)
        {
            _resultadoService = resultadoService;
        }

        // GET: api/Resultados/eleccion
        [HttpGet("eleccion")]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorEleccion(Guid eleccionId)
        {
            var resultado = await _resultadoService.ObtenerPorEleccionAsync(eleccionId);

            if (resultado == null)
                return NotFound("No se encontraron resultados para la elección.");

            return Ok(resultado);
        }

        // GET: api/Resultados/eleccion/canton
        [HttpGet("eleccion/canton")]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorCanton(Guid eleccionId, Guid cantonId)
        {
            var resultado = await _resultadoService.ObtenerPorCantonAsync(eleccionId, cantonId);

            return Ok(resultado);
        }

        // GET: api/Resultados/eleccion/parroquia
        [HttpGet("eleccion/parroquia")]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorParroquia(Guid eleccionId, Guid parroquiaId)
        {
            var resultado = await _resultadoService.ObtenerPorParroquiaAsync(eleccionId, parroquiaId);

            return Ok(resultado);
        }

        // GET: api/Resultados/eleccion/zona
        [HttpGet("eleccion/zona")]
        public async Task<ActionResult<ResultadoEleccionDTO>> ObtenerPorZona(Guid eleccionId, Guid zonaId)
        {
            var resultado = await _resultadoService.ObtenerPorZonaAsync(eleccionId, zonaId);

            return Ok(resultado);
        }


    }
}
