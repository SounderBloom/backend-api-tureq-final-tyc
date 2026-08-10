using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Notificaciones;
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
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionesService _notificacionesService;

        public NotificacionesController(INotificacionesService notificacionesService)
        {
            _notificacionesService = notificacionesService;
        }

        private Guid UsuarioId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

        [HttpGet("Obtener")]
        public async Task<IActionResult> Obtener(int pagina = 0)
        {
            var notificaciones = await _notificacionesService.ObtenerNotificaciones(UsuarioId, pagina);
            return Ok(new ResponseWrapper<List<NotificacionDTO>>
            {
                Data = notificaciones,
                Message = "Notificaciones obtenidas exitosamente.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpGet("ContarNoLeidas")]
        public async Task<IActionResult> ContarNoLeidas()
        {
            var total = await _notificacionesService.ContarNoLeidas(UsuarioId);
            return Ok(new ResponseWrapper<int>
            {
                Data = total,
                Message = "Conteo obtenido exitosamente.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpPost("MarcarLeida/{id}")]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var resultado = await _notificacionesService.MarcarLeida(UsuarioId, id);
            if (!resultado)
            {
                return BadRequest(new ResponseWrapper<bool>
                {
                    Data = false,
                    Message = "No se pudo marcar la notificacion como leida.",
                    Code = HttpStatusCode.BadRequest
                });
            }
            return Ok(new ResponseWrapper<bool>
            {
                Data = true,
                Message = "Notificacion marcada como leida.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpPost("MarcarTodasLeidas")]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            await _notificacionesService.MarcarTodasLeidas(UsuarioId);
            return Ok(new ResponseWrapper<bool>
            {
                Data = true,
                Message = "Notificaciones marcadas como leidas.",
                Code = HttpStatusCode.OK
            });
        }
    }
}
