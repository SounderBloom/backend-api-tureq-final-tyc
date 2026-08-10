using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Calificaciones;
using api_tienda_web_odi.Wrapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace api_tienda_web_odi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalificacionesController : ControllerBase
    {
        private readonly ICalificacionesService _calificacionesService;

        public CalificacionesController(ICalificacionesService calificacionesService)
        {
            _calificacionesService = calificacionesService;
        }

        [HttpPost("Crear")]
        [Authorize]
        public async Task<IActionResult> Crear([FromBody] CrearCalificacionDTO dto)
        {
            var usuarioId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var (exito, mensaje) = await _calificacionesService.CrearCalificacion(dto, usuarioId);

            if (!exito)
            {
                return BadRequest(new ResponseWrapper<bool>
                {
                    Data = false,
                    Message = mensaje,
                    Code = HttpStatusCode.BadRequest
                });
            }

            return Ok(new ResponseWrapper<bool>
            {
                Data = true,
                Message = mensaje,
                Code = HttpStatusCode.OK
            });
        }

        [HttpGet("ObtenerDeUsuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerDeUsuario(Guid usuarioId)
        {
            var resumen = await _calificacionesService.ObtenerDeUsuario(usuarioId);
            return Ok(new ResponseWrapper<ResumenCalificacionesDTO>
            {
                Data = resumen,
                Message = "Calificaciones obtenidas exitosamente.",
                Code = HttpStatusCode.OK
            });
        }
    }
}
