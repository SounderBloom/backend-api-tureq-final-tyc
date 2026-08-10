using api_tienda_web_odi.Data.Notificacion;

namespace api_tienda_web_odi.Models.Notificaciones
{
    public class NotificacionDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public bool Leida { get; set; }
        public TipoNotificacion Tipo { get; set; }
        public Guid? ReferenciaId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string UrlImagenIcono { get; set; } = string.Empty;
    }
}
