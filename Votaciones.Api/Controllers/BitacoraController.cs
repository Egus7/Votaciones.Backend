using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.SeguridadDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Security;

namespace Votaciones.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BitacoraController : ControllerBase
    {
        private readonly IBitacoraService _bitacoraService;

        public BitacoraController(IBitacoraService bitacoraService)
        {
            _bitacoraService = bitacoraService;
        }

        [HttpGet("paginacion")]
        [Authorize(Policy = RolPermisos.BitacoraView)]
        public async Task<ActionResult<PaginacionDTO<BitacoraDTO>>> GetPaginacion([FromQuery] int pagina = 1, [FromQuery] int pageSize = 7,
            [FromQuery] DateTime? fechaDesde = null, [FromQuery] DateTime? fechaHasta = null, [FromQuery] Guid? usuarioId = null, 
            [FromQuery] string? accion = null, [FromQuery] string? tabla = null)
        {
            var resultado = await _bitacoraService.ObtenerPaginacionAsync(pagina, pageSize, fechaDesde, fechaHasta, usuarioId, accion, tabla);

            return Ok(resultado);
        }

        [HttpGet("id")]
        [Authorize(Policy = RolPermisos.BitacoraView)]
        public async Task<ActionResult<BitacoraDTO>> GetPorId(Guid bitacoraId)
        {
            var resultado = await _bitacoraService.ObtenerPorIdAsync(bitacoraId);
            
            return Ok(resultado);
        }

        [HttpGet("registro")]
        [Authorize(Policy = RolPermisos.BitacoraView)]
        public async Task<ActionResult<List<BitacoraDTO>>> GetPorRegistro(string tabla, string idRegistro)
        {
            var resultado = await _bitacoraService.ObtenerPorRegistroAsync(tabla, idRegistro);
            
            return Ok(resultado);
        }

    }
}
