using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Chats;
using api_tienda_web_odi.Wrapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace api_tienda_web_odi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatsController : ControllerBase
    {
        private readonly IChatsService _chatsService;
        public ChatsController(IChatsService chatsService)
        {
            _chatsService = chatsService;
        }

        [HttpGet("ObtenerListaChats")]
        [Authorize]
        public async Task<IActionResult> ObtenerChats(int pagina = 0)
        {
            var UserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var chats = await _chatsService.ObtenerChats(UserId, pagina);
            return Ok(new ResponseWrapper<List<ChatDTO>>
            {
                Data = chats,
                Message = "Chats obtenidos exitosamente.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpPost("Crear/{ProductoId}")]
        [Authorize]
        public async Task<IActionResult> CrearChat([FromRoute] Guid ProductoId)
        {
            var UserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var chatId = await _chatsService.CrearChat(UserId, ProductoId);
            if (chatId == null)
            {
                return BadRequest(new ResponseWrapper<Guid?>
                {
                    Data = null,
                    Message = "No se pudo crear el chat.",
                    Code = HttpStatusCode.BadRequest
                });
            }
            return Ok(new ResponseWrapper<Guid>
            {
                Data = chatId.Value,
                Message = "Chat listo.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpGet("ObtenerMensajes/{ChatId}")]
        [Authorize]
        public async Task<IActionResult> ObtenerMensajes([FromRoute] Guid ChatId)
        {
            var UserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var mensajes = await _chatsService.ObtenerMensajes(ChatId, UserId);

            if (mensajes == null)
            {
                return BadRequest(new ResponseWrapper<List<MensajeDTO>?>
                {
                    Data = null,
                    Message = "No se pudo obtener la conversación.",
                    Code = HttpStatusCode.BadRequest
                });
            }

            return Ok(new ResponseWrapper<List<MensajeDTO>>
            {
                Data = mensajes,
                Message = "Mensajes obtenidos exitosamente.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpPost("EnviarMensaje")]
        [Authorize]
        // OJO: el parámetro NO se puede llamar "Mensaje" (como antes). El DTO
        // también tiene una propiedad "Mensaje" (el texto), y ASP.NET Core, al
        // ver un campo de formulario literalmente llamado "Mensaje", lo
        // interpretaba como el PREFIJO para bindear el resto de propiedades
        // (esperando "Mensaje.ChatId", "Mensaje.EsSistema", etc.). Como el
        // frontend manda los campos sin prefijo, todo caía a sus valores por
        // defecto -> ChatId siempre llegaba como Guid.Empty y el mensaje
        // nunca se podía enviar.
        public async Task<IActionResult> EnviarMensaje([FromForm] CrearMensajeDTO dto)
        {
            var EmisorId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var enviado = await _chatsService.EnviarMensaje(dto, EmisorId);
            if (!enviado)
            {
                return BadRequest(new ResponseWrapper<bool>
                {
                    Data = false,
                    Message = "No se pudo enviar el mensaje.",
                    Code = HttpStatusCode.BadRequest
                });
            }
            return Ok(new ResponseWrapper<bool>
            {
                Data = true,
                Message = "Mensaje enviado exitosamente.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpDelete("Borrar/{ChatId}")]
        [Authorize]
        public async Task<IActionResult> BorrarChat([FromRoute] Guid ChatId)
        {
            var UserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
            var borrado = await _chatsService.BorrarChat(UserId, ChatId);
            if (!borrado)
            {
                return BadRequest(new ResponseWrapper<bool>
                {
                    Data = false,
                    Message = "No se pudo borrar el chat.",
                    Code = HttpStatusCode.BadRequest
                });
            }
            return Ok(new ResponseWrapper<bool>
            {
                Data = true,
                Message = "Chat borrado exitosamente.",
                Code = HttpStatusCode.OK
            });
        }
    }
}
