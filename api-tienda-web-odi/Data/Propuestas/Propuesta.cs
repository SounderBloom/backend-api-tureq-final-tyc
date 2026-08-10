using api_tienda_web_odi.Data.Auth;
using api_tienda_web_odi.Data.Chats;
using api_tienda_web_odi.Data.Producto;

namespace api_tienda_web_odi.Data.Propuestas
{
    // Oferta hecha dentro de un chat: puede ser un trueque puro (producto
    // por producto), una compra en efectivo (sin producto ofrecido), o un
    // trueque con una diferencia en efectivo a favor de una de las partes.
    // El "proponente" es quien la envia; el "vendedor" (dueño del producto
    // solicitado) puede aceptarla o rechazarla.
    public class Propuesta
    {
        public Guid Id { get; set; }

        public Guid ChatId { get; set; }
        public Chat? Chat { get; set; }

        // Producto que el vendedor publicó y que el proponente quiere obtener.
        public Guid ProductoSolicitadoId { get; set; }
        public Producto.Producto? ProductoSolicitado { get; set; }

        public TipoOferta TipoOferta { get; set; } = TipoOferta.Trueque;

        // Producto que el proponente ofrece a cambio. Solo aplica para
        // Trueque y TruequeConDiferencia; null en una Compra pura.
        public Guid? ProductoOfrecidoId { get; set; }
        public Producto.Producto? ProductoOfrecido { get; set; }

        // Monto en efectivo: el precio completo en una Compra, o la
        // diferencia a favor de una parte en un TruequeConDiferencia.
        // Null en un Trueque puro.
        public decimal? Monto { get; set; }

        // Solo aplica en TruequeConDiferencia.
        public DireccionMonto? DireccionMonto { get; set; }

        public Guid ProponenteId { get; set; }
        public Usuario? Proponente { get; set; }

        public Guid VendedorId { get; set; }
        public Usuario? Vendedor { get; set; }

        public string Mensaje { get; set; } = string.Empty;

        public EstadoPropuesta Estado { get; set; } = EstadoPropuesta.Pendiente;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaResolucion { get; set; }
    }
}
