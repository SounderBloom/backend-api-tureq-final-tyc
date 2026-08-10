using api_tienda_web_odi.Data.Auth;
using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Usuarios;
using api_tienda_web_odi.Wrapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace api_tienda_web_odi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuariosService _usuariosService;

        public UsuariosController(IUsuariosService usuariosService)
        {
            _usuariosService = usuariosService;
        }

        private Guid UsuarioId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

        [HttpGet("MiPerfil")]
        [Authorize]
        public async Task<IActionResult> MiPerfil()
        {
            var perfil = await _usuariosService.ObtenerPerfil(UsuarioId);

            if (perfil == null)
            {
                return NotFound(new ResponseWrapper<PerfilUsuarioDTO?>
                {
                    Data = null,
                    Message = "No se encontro el usuario.",
                    Code = HttpStatusCode.NotFound
                });
            }

            return Ok(new ResponseWrapper<PerfilUsuarioDTO>
            {
                Data = perfil,
                Message = "Perfil obtenido exitosamente.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpGet("ObtenerTodos")]
        [Authorize(nameof(Rol.Administrador))]
        public async Task<IActionResult> ObtenerTodos()
        {
            var usuarios = await _usuariosService.ObtenerTodos();
            return Ok(new ResponseWrapper<List<UsuarioAdminDTO>>
            {
                Data = usuarios,
                Message = "Usuarios obtenidos exitosamente.",
                Code = HttpStatusCode.OK
            });
        }

        [HttpPost("CambiarRol/{id}")]
        [Authorize(nameof(Rol.Administrador))]
        public async Task<IActionResult> CambiarRol([FromRoute] Guid id, [FromBody] CambiarRolDTO dto)
        {
            if (!Enum.IsDefined(typeof(Rol), dto.Rol))
            {
                return BadRequest(new ResponseWrapper<bool>
                {
                    Data = false,
                    Message = "El rol indicado no es valido.",
                    Code = HttpStatusCode.BadRequest
                });
            }

            var (exito, mensaje) = await _usuariosService.CambiarRol(id, (Rol)dto.Rol, UsuarioId);

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
    }
}
