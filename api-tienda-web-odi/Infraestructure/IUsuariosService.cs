using api_tienda_web_odi.Data.Auth;
using api_tienda_web_odi.Models.Usuarios;

namespace api_tienda_web_odi.Infraestructure
{
    public interface IUsuariosService
    {
        Task<PerfilUsuarioDTO?> ObtenerPerfil(Guid usuarioId);

        // Panel de administrador
        Task<List<UsuarioAdminDTO>> ObtenerTodos();
        Task<(bool Exito, string Mensaje)> CambiarRol(Guid usuarioId, Rol nuevoRol, Guid solicitanteId);
    }
}
