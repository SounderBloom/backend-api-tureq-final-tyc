namespace api_tienda_web_odi.Models.Calificaciones
{
    public class CrearCalificacionDTO
    {
        public required Guid PropuestaId { get; set; }
        public required int Estrellas { get; set; }
        public string Comentario { get; set; } = string.Empty;
    }
}
