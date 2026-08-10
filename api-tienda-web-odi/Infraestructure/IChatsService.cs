using api_tienda_web_odi.Models.Chats;

namespace api_tienda_web_odi.Infraestructure
{
    public interface IChatsService
    {
        Task<bool> BorrarChat(Guid UsuarioId, Guid ChatId);

        // Devuelve el Id del chat (el existente si ya había uno entre este
        // usuario y este producto, o uno nuevo si no) o null si el producto
        // no existe.
        Task<Guid?> CrearChat(Guid InteresadoId, Guid ProductoId);
        Task<bool> EnviarMensaje(CrearMensajeDTO Mensaje, Guid EmisorId);
        Task<List<ChatDTO>> ObtenerChats(Guid UsuarioId, int iteracion = 0);

        // Historial completo de mensajes de un chat. Devuelve null si el
        // chat no existe o el usuario no participa en él (no es ni el
        // comprador ni el vendedor).
        Task<List<MensajeDTO>?> ObtenerMensajes(Guid chatId, Guid usuarioId);

        // Usado internamente (p. ej. por Propuestas) para dejar un mensaje
        // de sistema en el chat sin pasar por un emisor real.
        Task EnviarMensajeSistema(Guid chatId, string contenido);
    }
}
