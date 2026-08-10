using api_tienda_web_odi.Models.Calificaciones;

namespace api_tienda_web_odi.Infraestructure
{
    public interface ICalificacionesService
    {
        Task<(bool Exito, string Mensaje)> CrearCalificacion(CrearCalificacionDTO dto, Guid calificadorId);
        Task<ResumenCalificacionesDTO> ObtenerDeUsuario(Guid usuarioId);
    }
}
