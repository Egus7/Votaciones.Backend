using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ZonasController : ControllerBase
    {
        private readonly IZonaService _zonaService;

        public ZonasController(IZonaService zonaService)
        {
            _zonaService = zonaService;
        }

        [HttpGet("provincias")]
        public async Task<ActionResult<List<Provincia>>> ObtenerProvincias()
        {
            var resultado = await _zonaService.ObtenerProvinciasAsync();

            return Ok(resultado);
        }

        [HttpGet("cantones")]
        public async Task<ActionResult<List<Canton>>> ObtenerCantones([FromQuery] Guid provinciaId)
        {
            var resultado = await _zonaService.ObtenerCantonesAsync(provinciaId);

            return Ok(resultado);
        }

        [HttpGet("parroquias")]
        public async Task<ActionResult<List<Parroquia>>> ObtenerParroquias([FromQuery] Guid cantonId)
        {
            var resultado = await _zonaService.ObtenerParroquiasAsync(cantonId);

            return Ok(resultado);
        }

        [HttpGet("zonas")]
        public async Task<ActionResult<List<Zona>>> ObtenerZonas([FromQuery] Guid parroquiaId)
        {
            var resultado = await _zonaService.ObtenerZonasAsync(parroquiaId);

            return Ok(resultado);
        }

    }
}
