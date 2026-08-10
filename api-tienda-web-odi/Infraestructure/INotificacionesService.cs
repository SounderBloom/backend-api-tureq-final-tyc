using api_tienda_web_odi.Data.Notificacion;
using api_tienda_web_odi.Models.Notificaciones;

namespace api_tienda_web_odi.Infraestructure
{
    public interface INotificacionesService
    {
        // Usado internamente por otros servicios (chats, propuestas, calificaciones)
        // para generar una notificación para un usuario.
        Task CrearNotificacion(
            Guid usuarioId,
            string titulo,
            string contenido,
            TipoNotificacion tipo,
            Guid? referenciaId = null,
            string urlImagenIcono = "");

        Task<List<NotificacionDTO>> ObtenerNotificaciones(Guid usuarioId, int pagina = 0);
        Task<int> ContarNoLeidas(Guid usuarioId);
        Task<bool> MarcarLeida(Guid usuarioId, int notificacionId);
        Task<bool> MarcarTodasLeidas(Guid usuarioId);
    }
}
