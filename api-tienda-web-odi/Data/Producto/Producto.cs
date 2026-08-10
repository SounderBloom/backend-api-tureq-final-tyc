using api_tienda_web_odi.Data.Auth;
using api_tienda_web_odi.Data.Chats;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api_tienda_web_odi.Data.Producto
{
    public class Producto
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; }

        [Precision(18, 2)]
        public decimal Precio { get; set; }
        public string Descripcion { get; set; }
        public bool Disponible { get; set; }
        public DateTime FechaPublicacion { get; set; }
        public TipoTransaccion TipoTransaccion { get; set; }
        public Guid VendedorId { get; set; }
        public Usuario Vendedor { get; set; }
        public int CategoriaId { get; set; }

        // Ver la nota en FotosProducto.Producto: no inicializar con
        // "= new Categoria()", porque Categoria.Id es IDENTITY y EF Core
        // trataría esa instancia vacía (Id = 0) como una categoría nueva
        // que insertar en cada alta de producto.
        public Categoria? Categoria { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public List<FotosProducto> FotosProducto { get; set; } = new();

        [InverseProperty("Producto")]
        public List<Chat> Chats { get; set; } = new();
    }
}
