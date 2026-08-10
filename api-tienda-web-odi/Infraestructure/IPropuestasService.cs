using api_tienda_web_odi.Models.Propuestas;

namespace api_tienda_web_odi.Infraestructure
{
    public interface IPropuestasService
    {
        Task<(bool Exito, string Mensaje, PropuestaDTO? Propuesta)> CrearPropuesta(CrearPropuestaDTO dto, Guid proponenteId);
        Task<List<PropuestaDTO>> ObtenerPorChat(Guid chatId, Guid usuarioId);
        Task<(bool Exito, string Mensaje)> Responder(Guid propuestaId, Guid vendedorId, bool aceptar);
        Task<List<PropuestaDTO>> ObtenerPendientesDeCalificar(Guid usuarioId);
    }
}
