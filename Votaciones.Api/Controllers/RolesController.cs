using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Security;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly IRolService _rolService;
        public RolesController(IRolService rolService)
        {
            _rolService = rolService;
        }

        // GET: api/<RolesController>/paginacion
        [HttpGet("paginacion")]
        [Authorize(Policy = RolPermisos.RolesView)]
        public async Task<ActionResult<PaginacionDTO<RolDTO>>> ObtenerPaginacion([FromQuery] int pagina = 1, [FromQuery] int pageSize = 7, [FromQuery] string? buscar = null)
        {
            var resultado = await _rolService.ObtenerPaginacionAsync(pagina, pageSize, buscar);

            return Ok(resultado);
        }

        // GET api/<RolesController>/5
        [HttpGet("{id:guid}")]
        [Authorize(Policy = RolPermisos.RolesView)]
        public async Task<ActionResult<RolDTO>> ObtenerPorId(Guid id)
        {
            var rol = await _rolService.ObtenerPorIdAsync(id);

            if (rol == null)
                return NotFound("Rol no existe");

            return Ok(rol);
        }

        // POST api/<RolesController>
        [HttpPost]
        [Authorize(Policy = RolPermisos.RolesCreate)]
        public async Task<ActionResult<RolDTO>> CrearRol(RolDTO dto)
        {
            var resultado = await _rolService.CrearAsync(dto);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = dto.IdRol }, resultado);
        }

        // PUT api/<RolesController>/5
        [HttpPut("{id:guid}")]
        [Authorize(Policy = RolPermisos.RolesEdit)]
        public async Task<IActionResult> ActualizarRol(Guid id, RolDTO dto)
        {
            var resultado = await _rolService.ActualizarAsync(id, dto);

            return Ok("Rol actualizado correctamente");
        }

        [HttpPut("cambiarEstado/{id:guid}")]
        [Authorize(Policy = RolPermisos.RolesEdit)]
        public async Task<IActionResult> CambiarEstado(Guid id)
        {
            var resultado = await _rolService.CambiarEstadoAsync(id);

            return Ok(resultado.Activo ? "Rol activado correctamente." : "Rol desactivado correctamente.");
        }

    }
}
