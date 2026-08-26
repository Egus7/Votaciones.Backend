using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Domain.Interfaces.IServices;
using Votaciones.Domain.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CandidatosController : ControllerBase
    {
        private readonly ICandidatoService _candidatoService;

        public CandidatosController(ICandidatoService candidatoService)
        {
            _candidatoService = candidatoService;
        }

        // GET: api/CandidatosController
        [HttpGet("paginacion")]
        public async Task<ActionResult<PaginacionDTO<CandidatoDTO>>> GetPaginacion(Guid eleccionId, [FromQuery] int pagina = 1, [FromQuery] int pageSize = 7)
        {
            var resultado = await _candidatoService.ObtenerPaginacionAsync(eleccionId, pagina, pageSize);

            return Ok(resultado);
        }

        // GET api/CandidatosController/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CandidatoDTO>> ObtenerPorId(Guid id)
        {
            var candidato = await _candidatoService.ObtenerPorIdAsync(id);

            if (candidato == null)
                return NotFound("Candidato no existe");

            return Ok(candidato);
        }

        // POST api/CandidatosController
        [HttpPost]
        public async Task<ActionResult<Candidato>> CrearCandidato(Candidato candidato)
        {
            var resultado = await _candidatoService.CrearAsync(candidato);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.IdCandidato }, resultado);
        }

        // PUT api/CandidatosController/5
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarCandidato(Guid id, Candidato candidato)
        {
            var resultado = await _candidatoService.ActualizarAsync(id, candidato);

            return Ok("Candidato actualizado correctamente");
        }

        // PUT: api/Candidatos/cambiarEstado/{id}
        [HttpPut("cambiarEstado/{id:guid}")]
        public async Task<IActionResult> CambiarEstado(Guid id)
        {
            var resultado = await _candidatoService.CambiarEstadoAsync(id);

            return Ok(new 
            { 
                message = resultado.Activo  ? "Candidato activado correctamente." : "Candidato desactivado correctamente."
            });
        }

    }
}
