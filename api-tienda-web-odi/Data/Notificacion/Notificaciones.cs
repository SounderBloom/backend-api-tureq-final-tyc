using api_tienda_web_odi.Data.Auth;

namespace api_tienda_web_odi.Data.Notificacion
{
    public class Notificaciones
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public Guid UsuarioNotificadoId { get; set; }
        public Usuario UsuarioNotificado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public string UrlImagenIcono { get; set; } = string.Empty;
        public bool Leida { get; set; } = false;

        // Tipo de evento que generó la notificación (mensaje, propuesta, respuesta, calificación...)
        public TipoNotificacion Tipo { get; set; } = TipoNotificacion.Sistema;

        // Id de referencia opcional (ChatId, PropuestaId, etc.) para poder
        // navegar directamente desde la notificación en el frontend.
        public Guid? ReferenciaId { get; set; }
    }
}
