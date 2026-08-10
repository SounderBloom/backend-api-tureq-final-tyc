using api_tienda_web_odi.Data.Auth;
using api_tienda_web_odi.Data.Propuestas;

namespace api_tienda_web_odi.Data.Calificaciones
{
    // Calificación con estrellas que el proponente de una propuesta ACEPTADA
    // deja al vendedor. Una calificación por propuesta (índice único).
    public class Calificacion
    {
        public int Id { get; set; }

        public Guid PropuestaId { get; set; }
        public Propuesta? Propuesta { get; set; }

        public Guid CalificadorId { get; set; }
        public Usuario? Calificador { get; set; }

        public Guid CalificadoId { get; set; }
        public Usuario? Calificado { get; set; }

        public int Estrellas { get; set; }
        public string Comentario { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
