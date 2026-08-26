using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActasController : ControllerBase
    {
        private readonly IActaService _actaService;

        public ActasController(IActaService actaService)
        {
            _actaService = actaService;
        }

        [HttpGet("paginacion")]
        public async Task<ActionResult<PaginacionDTO<ActaDTO>>> GetPaginacion(Guid eleccionId, [FromQuery] int pagina = 1, [FromQuery] int pageSize = 7)
        {
            var resultado = await _actaService.ObtenerPaginacionAsync(eleccionId, pagina, pageSize);

            return Ok(resultado);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ActaDTO>> ObtenerPorId(Guid id)
        {
            var acta = await _actaService.ObtenerPorIdAsync(id);

            if (acta == null)
                return NotFound(new { message = "El acta no existe." });

            return Ok(acta);
        }

        [HttpGet("mesa/{mesaId:guid}")]
        public async Task<ActionResult<ActaDTO>> ObtenerPorMesa(Guid mesaId)
        {
            var acta = await _actaService.ObtenerPorMesaAsync(mesaId);

            if (acta == null)
                return NotFound(new { message = "La mesa no tiene un acta registrada." });

            return Ok(acta);
        }

        [HttpPost]
        public async Task<ActionResult<ActaEleccion>> CrearActa(ActaEleccion acta)
        {
            //var usuarioId = UsuarioId; // del usuario autenticado
            var resultado = await _actaService.CrearAsync(acta);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.IdActa }, resultado);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> ActualizarActa(Guid id, ActaEleccion acta)
        {
            //var usuarioId = UsuarioId;
            var resultado = await _actaService.ActualizarAsync(id, acta);

            return Ok(new { message = "Acta actualizada correctamente." });
        }

        [HttpPut("cambiarEstado/{id:guid}")]
        public async Task<IActionResult> CambiarEstado(Guid id, EstadoActa nuevoEstado)
        {
            var resultado = await _actaService.CambiarEstadoAsync(id, nuevoEstado);

            return Ok(new { message = "Estado del acta actualizado correctamente." });
        }

    }
}
