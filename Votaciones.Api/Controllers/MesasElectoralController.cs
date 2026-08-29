using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Security;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MesasElectoralController : ControllerBase
    {
        private IMesaElectoralService _mesaElelectoralService;

        public MesasElectoralController(IMesaElectoralService mesaElelectoralService)
        {
            _mesaElelectoralService = mesaElelectoralService;
        }

        // GET: api/<MesasElectoralController>/paginacion
        [HttpGet("paginacion")]
        [Authorize(Policy = RolPermisos.MesasView)]
        public  async Task<ActionResult<PaginacionDTO<MesaElectoralDTO>>> GetPaginacion(Guid eleccionId, [FromQuery] int pagina = 1, [FromQuery] int pageSize = 7)
        {
            var resultado = await _mesaElelectoralService.ObtenerPaginacionAsync(eleccionId, pagina, pageSize);

            return Ok(resultado);
        }

        // GET api/<MesasElectoralController>/5
        [HttpGet("{id}")]
        [Authorize(Policy = RolPermisos.MesasView)]
        public async Task<ActionResult<MesaElectoralDTO>> ObtenerPorId(Guid id)
        {
            var mesaElectoral = await _mesaElelectoralService.ObtenerPorIdAsync(id);

            if (mesaElectoral == null)
                return NotFound("Mesa electoral no existe");

            return Ok(mesaElectoral);
        }

        // POST api/<MesasElectoralController>
        [HttpPost]
        [Authorize(Policy = RolPermisos.MesasCreate)]
        public async Task<ActionResult<MesaElectoral>> CrearMesaElectoral(MesaElectoral mesaElectoral)
        {
            var resultado = await _mesaElelectoralService.CrearAsync(mesaElectoral);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.IdMesaElectoral }, resultado);
        }

        // PUT api/<MesasElectoralController>/5
        [HttpPut("{id}")]
        [Authorize(Policy = RolPermisos.MesasEdit)]
        public async Task<IActionResult> ActualizarMesaElectoral(Guid id, MesaElectoral mesaElectoral)
        {
            var resultado = await _mesaElelectoralService.ActualizarAsync(id, mesaElectoral);

            return Ok("Mesa electoral actualizada correctamente");
        }

        // PUT: api/MesasElectoral/cambiarEstado/{id}
        [HttpPut("cambiarEstado/{id:guid}")]
        [Authorize(Policy = RolPermisos.MesasEdit)]
        public async Task<IActionResult> CambiarEstado(Guid id)
        {
            var resultado = await _mesaElelectoralService.CambiarEstadoAsync(id);

            return Ok(new
            {
                message = resultado.Activa ? "Mesa electoral activada correctamente." : "Mesa electoral desactivada correctamente."
            });
        }

    }
}
