namespace api_tienda_web_odi.Models.Calificaciones
{
    public class CalificacionDTO
    {
        public int Id { get; set; }
        public Guid CalificadorId { get; set; }
        public string CalificadorNombre { get; set; } = string.Empty;
        public int Estrellas { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}
