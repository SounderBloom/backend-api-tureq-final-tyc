namespace api_tienda_web_odi.Models.Usuarios
{
    public class PerfilUsuarioDTO
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ApellidoPaterno { get; set; } = string.Empty;
        public string ApellidoMaterno { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Biografia { get; set; } = string.Empty;
        public string FotoPerfilUrl { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string Rol { get; set; } = string.Empty;

        public int TruequesRealizados { get; set; }
        public int ArticulosActivos { get; set; }
        public double PromedioCalificacion { get; set; }
        public int TotalCalificaciones { get; set; }
    }
}
