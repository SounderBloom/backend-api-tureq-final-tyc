using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Propuestas;
using api_tienda_web_odi.Wrapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace api_tienda_web_odi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PropuestasController : ControllerBase
    {
        private readonly IPropuestasService _propuestasService;

        public PropuestasController(IPropuestasService propuestasService)
        {
            _propuestasService = propuestasService;
        }

        private Guid UsuarioId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

        [HttpPost("Crear")]
        public async Task<IActionResult> Crear([FromBody] CrearPropuestaDTO dto)
        {
            var (exito, mensaje, propuesta) = await _propuestasService.CrearPropuesta(dto, UsuarioId);

            if (!exito)
            {
                return BadRequest(new ResponseWrapper<PropuestaDTO?>
                {
                    Data = null,
                    Message = mensaje,
                    Code = HttpStatusCode.BadRequest
                });
            }

            return Ok(new ResponseWrapper<PropuestaDTO?>
            {
                Data = propuesta,
                Message = mensaje,
                Code = HttpStatusCode.OK
            });
        }

        [HttpGet("ObtenerPorChat/{chatId}")]
        public async Task<IActionResult> ObtenerPorChat(Guid chatId)
        {
            var propuestas = await _propuestasService.ObtenerPorChat(chatId, UsuarioId);
            return Ok(new ResponseWrapper<List<PropuestaDTO>>
            {
                Data = propuestas,
                Message = "Propuestas obtenidas exitosamente.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpPost("Responder/{propuestaId}")]
        public async Task<IActionResult> Responder(Guid propuestaId, [FromBody] ResponderPropuestaDTO dto)
        {
            var (exito, mensaje) = await _propuestasService.Responder(propuestaId, UsuarioId, dto.Aceptar);

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

        [HttpGet("PendientesDeCalificar")]
        public async Task<IActionResult> PendientesDeCalificar()
        {
            var propuestas = await _propuestasService.ObtenerPendientesDeCalificar(UsuarioId);
            return Ok(new ResponseWrapper<List<PropuestaDTO>>
            {
                Data = propuestas,
                Message = "Propuestas pendientes de calificar obtenidas exitosamente.",
                Code = HttpStatusCode.OK
            });
        }
    }
}
