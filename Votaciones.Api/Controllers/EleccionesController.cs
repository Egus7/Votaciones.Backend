using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Security;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EleccionesController : ControllerBase
    {
        private readonly IEleccionService _eleccionService;

        public EleccionesController(IEleccionService eleccionService)
        {
            _eleccionService = eleccionService;
        }

        [HttpGet("paginacion")]
        [Authorize(Policy = RolPermisos.EleccionesView)]
        public async Task<ActionResult<PaginacionDTO<Eleccion>>> ObtenerPaginacion([FromQuery] int pagina = 1, [FromQuery] int pageSize = 7,
            [FromQuery] string? busqueda = null, [FromQuery] DateTime? fechaInicio = null, [FromQuery] DateTime? fechaFin = null, 
            [FromQuery] TipoEleccion? tipoEleccion = null)
        {
            var resultados = await _eleccionService.ObtenerPaginacionAsync(pagina, pageSize, busqueda, fechaInicio, fechaFin, tipoEleccion);
            
            return Ok(resultados);
        }

        [HttpGet]
        [Authorize(Policy = RolPermisos.EleccionesView)]
        public async Task<ActionResult<IEnumerable<Eleccion>>> ObtenerTodos()
        {
            var elecciones = await _eleccionService.ObtenerTodosAsync();

            return Ok(elecciones);
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = RolPermisos.EleccionesView)]
        public async Task<ActionResult<Eleccion>> ObtenerPorId(Guid id)
        {
            var eleccion = await _eleccionService.ObtenerPorIdAsync(id);

            if (eleccion == null)
                return NotFound("Eleccion no existe");

            return Ok(eleccion);
        }

        [HttpPost]
        [Authorize(Policy = RolPermisos.EleccionesCreate)]
        public async Task<ActionResult<Eleccion>> CrearEleccion(Eleccion eleccion)
        {
            var resultado = await _eleccionService.CrearAsync(eleccion);
            
            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.IdEleccion }, resultado);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = RolPermisos.EleccionesEdit)]
        public async Task<IActionResult> ActualizarEleccion(Guid id, Eleccion eleccion)
        {
            var resultado = await _eleccionService.ActualizarAsync(id, eleccion);

            return Ok("Elección actualizada correctamente");
        }

    }
}
