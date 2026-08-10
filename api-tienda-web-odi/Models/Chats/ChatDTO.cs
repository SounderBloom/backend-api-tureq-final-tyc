using api_tienda_web_odi.Data.Producto;

namespace api_tienda_web_odi.Models.Chats
{
    public class ChatDTO
    {
        public Guid Id { get; set; }
        public Guid? ProductoId { get; set; }
        public string NombreProductoSnapshot { get; set; } = string.Empty;
        public string ImagenProductoSnapshot { get; set; } = string.Empty;
        public TipoTransaccion TipoTransaccionProductoSnapshot { get; set; }
        public string UrlFotoUsuario { get; set; } = string.Empty;
        public MensajeDTO? UltimoMensaje { get; set; } = new();
        public DateTime UltimoMovimiento { get; set; }

        // Indica si el usuario que consulta es el vendedor del producto de
        // este chat (true) o el comprador/interesado (false). Ayuda al
        // frontend a decidir de que lado mostrar los mensajes y si puede
        // responder propuestas de trueque.
        public bool EsVendedor { get; set; }
    }
}
