using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Votaciones.Application.DTOs.PaginacionDTO;
using Votaciones.Application.DTOs.VotacionesDTO;
using Votaciones.Application.Interfaces.IServices;
using Votaciones.Application.Security;
using Votaciones.Domain.Models;
using static Votaciones.Domain.Enums.EnumsEleccion;

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
        public  async Task<ActionResult<PaginacionDTO<MesaElectoralDTO>>> GetPaginacion(Guid eleccionId, [FromQuery] int pagina = 1, [FromQuery] int pageSize = 7, 
                [FromQuery] string? busqueda = null, [FromQuery] TipoMesa? tipoMesa = null)
        {
            var resultado = await _mesaElelectoralService.ObtenerPaginacionAsync(eleccionId, pagina, pageSize, busqueda, tipoMesa);

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

        [HttpGet("zona")]
        [Authorize(Policy = RolPermisos.MesasView)]
        public async Task<List<MesaElectoralDTO>> ObtenerPorZona(Guid eleccionId, Guid zonaId)
        {
            var mesaElectoral = await _mesaElelectoralService.ObtenerPorZonaAsync(eleccionId, zonaId);
            return mesaElectoral;
        }

        [HttpGet("disponible-por-zona")]
        [Authorize(Policy = RolPermisos.MesasView)]
        public async Task<List<MesaElectoralDTO>> ObtenerDisponiblePorZona(Guid eleccionId, Guid zonaId, TipoCandidato tipoCandidato)
        {
            var mesaElectoral = await _mesaElelectoralService.ObtenerDisponiblePorZonaAsync(eleccionId, 
                zonaId, tipoCandidato);

            return mesaElectoral;
        }

        [HttpGet("codigo-mesa")]
        [Authorize(Policy = RolPermisos.MesasView)]
        public async Task<ActionResult> CodigoMesa([FromQuery] Guid eleccionId, [FromQuery] Guid zonaId, [FromQuery] TipoMesa tipoMesa)
        {
            var resultado = await _mesaElelectoralService.PrevisualizarAsync(eleccionId, zonaId, tipoMesa);

            return Ok(new { codigoMesa = resultado.CodigoMesa, descripcion = resultado.Descripcion });
        }

        // POST api/<MesasElectoralController>
        [HttpPost]
        [Authorize(Policy = RolPermisos.MesasCreate)]
        public async Task<ActionResult<MesaElectoral>> CrearMesaElectoral(MesaElectoral mesaElectoral)
        {
            var resultado = await _mesaElelectoralService.CrearAsync(mesaElectoral);

            return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.IdMesaElectoral }, resultado);
        }

        [HttpPost("lote")]
        [Authorize(Policy = RolPermisos.MesasCreate)]
        public async Task<ActionResult<MesaElectoral>> CrearMesaElectoralLote([FromQuery] Guid eleccionId, [FromQuery] Guid zonaId, 
                [FromQuery] int cantidadFemeninas, [FromQuery] int cantidadMasculinas)
        {
            var resultado = await _mesaElelectoralService.CrearLoteAsync(eleccionId, zonaId, cantidadFemeninas, cantidadMasculinas);

            return Ok(resultado);
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

            return Ok(resultado.Activa ? 
                "Mesa electoral activada correctamente." : "Mesa electoral desactivada correctamente.");
        }

    }
}
