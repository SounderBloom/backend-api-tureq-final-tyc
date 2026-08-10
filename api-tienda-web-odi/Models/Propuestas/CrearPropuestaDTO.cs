using api_tienda_web_odi.Data.Propuestas;

namespace api_tienda_web_odi.Models.Propuestas
{
    public class CrearPropuestaDTO
    {
        public required Guid ChatId { get; set; }
        public TipoOferta TipoOferta { get; set; } = TipoOferta.Trueque;

        // Requerido si TipoOferta es Trueque o TruequeConDiferencia.
        public Guid? ProductoOfrecidoId { get; set; }

        // Requerido si TipoOferta es Compra o TruequeConDiferencia.
        public decimal? Monto { get; set; }

        // Requerido si TipoOferta es TruequeConDiferencia.
        public DireccionMonto? DireccionMonto { get; set; }

        public string Mensaje { get; set; } = string.Empty;
    }
}
