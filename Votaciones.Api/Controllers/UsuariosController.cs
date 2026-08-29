using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Security;
using Votaciones.Domain.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // GET: api/<UsuariosController>
        [HttpGet("paginacion")]
        [Authorize(Policy = RolPermisos.UsuariosView)]
        public async Task<ActionResult<PaginacionDTO<UsuarioDTO>>> ObtenerPaginacion([FromQuery] int pagina = 1, [FromQuery] int pageSize = 7, [FromQuery] string? buscar = null)
        {
            var resultado = await _usuarioService.ObtenerPaginacionAsync(pagina, pageSize, buscar);

            return Ok(resultado);
        }

        // GET api/<UsuariosController>/5
        [HttpGet("{id:guid}")]
        [Authorize(Policy = RolPermisos.UsuariosView)]
        public async Task<ActionResult<UsuarioDTO>> ObtenerPorId(Guid id)
        {
            var usuario = await _usuarioService.ObtenerPorIdAsync(id);

            if (usuario == null)
                return NotFound("Rol no existe");

            return Ok(usuario);
        }

        // POST api/<UsuariosController>
        [HttpPost]
        [Authorize(Policy = RolPermisos.UsuariosCreate)]
        public async Task<ActionResult<AdmUsuario>> CrearUsuario(AdmUsuario usuario)
        {
            var resultado = await _usuarioService.CrearAsync(usuario);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = usuario.IdUsuario }, resultado);
        }

        // PUT api/<UsuariosController>/5
        [HttpPut("{id:guid}")]
        [Authorize(Policy = RolPermisos.UsuariosEdit)]
        public async Task<IActionResult> ActualizarUsuario(Guid id, AdmUsuario usuario)
        {
            var resultado = await _usuarioService.ActualizarAsync(id, usuario);

            return Ok("Usuario actualizado correctamente");
        }

        [HttpPut("cambiarEstado/{id:guid}")]
        [Authorize(Policy = RolPermisos.UsuariosEdit)]
        public async Task<IActionResult> CambiarEstado(Guid id)
        {
            var resultado = await _usuarioService.CambiarEstadoAsync(id);

            return Ok(new { message = resultado.Estado
                ? "Usuario activado correctamente." : "Usuario desactivado correctamente." });
        }
    }
}
