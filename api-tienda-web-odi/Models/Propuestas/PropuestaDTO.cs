using api_tienda_web_odi.Data.Propuestas;

namespace api_tienda_web_odi.Models.Propuestas
{
    public class PropuestaDTO
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }

        public Guid ProductoSolicitadoId { get; set; }
        public string ProductoSolicitadoTitulo { get; set; } = string.Empty;
        public string? ProductoSolicitadoFoto { get; set; }

        public TipoOferta TipoOferta { get; set; }

        public Guid? ProductoOfrecidoId { get; set; }
        public string? ProductoOfrecidoTitulo { get; set; }
        public string? ProductoOfrecidoFoto { get; set; }

        public decimal? Monto { get; set; }
        public DireccionMonto? DireccionMonto { get; set; }

        public Guid ProponenteId { get; set; }
        public Guid VendedorId { get; set; }

        public string Mensaje { get; set; } = string.Empty;
        public EstadoPropuesta Estado { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaResolucion { get; set; }

        // true si la propuesta ya fue aceptada y el proponente todavía no
        // ha calificado al vendedor por ella.
        public bool PuedeCalificar { get; set; }
    }
}
