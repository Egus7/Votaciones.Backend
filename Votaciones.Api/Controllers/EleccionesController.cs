using Microsoft.AspNetCore.Mvc;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EleccionesController : ControllerBase
    {
        private readonly IEleccionService _eleccionService;

        public EleccionesController(IEleccionService eleccionService)
        {
            _eleccionService = eleccionService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Eleccion>>> ObtenerTodos()
        {
            var elecciones = await _eleccionService.ObtenerTodosAsync();

            return Ok(elecciones);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Eleccion>> ObtenerPorId(Guid id)
        {
            var eleccion = await _eleccionService.ObtenerPorIdAsync(id);

            if (eleccion == null)
                return NotFound("Eleccion no existe");

            return Ok(eleccion);
        }

        [HttpPost]
        public async Task<ActionResult<Eleccion>> CrearEleccion(Eleccion eleccion)
        {
            var resultado = await _eleccionService.CrearAsync(eleccion);
            
            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.IdEleccion }, resultado);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> ActualizarEleccion(Guid id, Eleccion eleccion)
        {
            var resultado = await _eleccionService.ActualizarAsync(id, eleccion);

            return Ok("Elección actualizada correctamente");
        }

    }
}
