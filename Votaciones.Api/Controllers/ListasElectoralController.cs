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
    public class ListasElectoralController : ControllerBase
    {
        private readonly IListaElectoralService _listaElectoralService;

        public ListasElectoralController(IListaElectoralService listaElectoralService)
        {
            _listaElectoralService = listaElectoralService;
        }

        // GET: api/<ListasElectoralController>/paginacion
        [HttpGet("paginacion")]
        [Authorize(Policy = RolPermisos.ListasView)]
        public async Task<ActionResult<PaginacionDTO<ListaElectoral>>> GetPaginacion([FromQuery] int pagina = 1, [FromQuery] int pageSize = 7, 
                [FromQuery] string? buscar = null, [FromQuery] Jurisdiccion? jurisdiccion = null)
        {
            var resultado = await _listaElectoralService.ObtenerPaginacionAsync(pagina, pageSize, buscar, jurisdiccion);

            return Ok(resultado);
        }

        // GET api/<ListasElectoralController>/5
        [HttpGet("{id}")]
        [Authorize(Policy = RolPermisos.ListasView)]
        public async Task<ActionResult<ListaElectoral>> ObtenerPorId(Guid id)
        {
            var listaElectoral = await _listaElectoralService.ObtenerPorIdAsync(id);

            return Ok(listaElectoral);
        }

        // POST api/<ListasElectoralController>
        [HttpPost]
        [Authorize(Policy = RolPermisos.ListasCreate)]
        public async Task<ActionResult<ListaElectoral>> CrearListaElectoral(ListaElectoral listaElectoral)
        {
            var resultado = await _listaElectoralService.CrearAsync(listaElectoral);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.IdListaElectoral }, resultado);
        }

        // PUT api/<ListasElectoralController>/5
        [HttpPut("{id}")]
        [Authorize(Policy = RolPermisos.ListasEdit)]
        public async Task<IActionResult> ActualizarListaElectoral(Guid id, ListaElectoral listaElectoral)
        {
            var resultado = await _listaElectoralService.ActualizarAsync(id, listaElectoral);

            return Ok("Lista electoral actualizada correctamente");
        }

        // PUT: api/ListasElectoral/cambiarEstado/{id}
        [HttpPut("cambiarEstado/{id:guid}")]
        [Authorize(Policy = RolPermisos.ListasEdit)]
        public async Task<IActionResult> CambiarEstado(Guid id)
        {
            var resultado = await _listaElectoralService.CambiarEstadoAsync(id);

            return Ok(resultado.Activo ? 
                "Lista electoral activada correctamente." : "Lista electoral suspendida correctamente.");
        }

    }
}
