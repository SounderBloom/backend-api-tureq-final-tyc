namespace api_tienda_web_odi.Models.Calificaciones
{
    public class ResumenCalificacionesDTO
    {
        public double Promedio { get; set; }
        public int Total { get; set; }
        public List<CalificacionDTO> Recientes { get; set; } = [];
    }
}
