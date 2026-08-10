using api_tienda_web_odi.Data;
using api_tienda_web_odi.Data.Producto;
using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Productos;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace api_tienda_web_odi.Service
{
    public class ProductosService: IProductosService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        public ProductosService(
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<(bool Exito, string Mensaje)> CrearProducto(CrearProductoDTO producto, Guid vendedorId)
        {
            await using var trsc = await _context.Database.BeginTransactionAsync();

            try
            {
                var productoBD = _context.Producto.Add(new Producto
                {
                    Titulo = producto.Titulo,
                    Descripcion = producto.Descripcion,
                    Precio = producto.Precio,
                    Disponible = true,
                    CategoriaId = producto.CategoriaId,
                    FechaPublicacion = DateTime.Now,
                    TipoTransaccion = producto.TipoTransaccion,
                    Latitud = producto.Latitud,
                    Longitud = producto.Longitud,
                    VendedorId = vendedorId
                });

                // Guarda primero el producto
                await _context.SaveChangesAsync();

                foreach (var foto in producto.Fotos)
                {
                    string nombreArchivo =
                        $"{productoBD.Entity.Id}_{foto.Orden}{Path.GetExtension(foto.Foto.FileName)}";

                    string carpetaFisica = Path.Combine(
                        _environment.WebRootPath,
                        "Uploads",
                        "Productos");

                    // La carpeta wwwroot/Uploads/Productos no siempre existe en
                    // una instalación nueva (las carpetas vacías no se
                    // versionan en git). Sin esto, FileStream lanza
                    // DirectoryNotFoundException y la creación del producto
                    // falla en silencio con "Error al crear el producto".
                    Directory.CreateDirectory(carpetaFisica);

                    string rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);

                    string rutaPublica =
                        $"/Uploads/Productos/{nombreArchivo}";

                    // Guarda el archivo en disco
                    await using (FileStream stream = new(rutaFisica, FileMode.Create))
                    {
                        await foto.Foto.CopyToAsync(stream);
                    }

                    _context.FotosProducto.Add(new FotosProducto
                    {
                        ProductoId = productoBD.Entity.Id,
                        Orden = foto.Orden,
                        FotoRuta = rutaPublica
                    });
                }

                await _context.SaveChangesAsync();
                await trsc.CommitAsync();

                return (true, "Producto creado exitosamente.");
            }
            catch (Exception ex)
            {
                // Se incluye el mensaje real de la excepción en la respuesta
                // (además de loguearlo) porque en hosting compartido (p. ej.
                // Somee) no siempre hay acceso a la consola/logs del
                // servidor como sí lo hay en Docker local. Causas típicas en
                // ese tipo de hosting: falta de permisos de escritura en
                // wwwroot/Uploads, o migraciones no aplicadas en la base de
                // datos remota (p. ej. sin categorías cargadas, lo que
                // rompe la llave foránea CategoriaId).
                Console.Error.WriteLine($"Error al crear producto: {ex}");
                await trsc.RollbackAsync();

                var detalle = ex.InnerException?.Message ?? ex.Message;
                return (false, $"Error al crear el producto: {detalle}");
            }
        }

        public async Task<bool> EliminarProducto(Guid productoId, Guid userId)
        {
            var trsc = _context.Database.BeginTransaction();
            try
            {
                var productoBd = await _context.Producto.FirstOrDefaultAsync(p => p.Id == productoId && p.VendedorId == userId);
                if (productoBd == null)
                {
                    return false;
                }

                _context.Producto.Remove(productoBd);
                var result = await _context.SaveChangesAsync();
                if (result <= 0)
                {
                    await trsc.RollbackAsync();
                    return false;
                }
                await trsc.CommitAsync();
                return true;
            }
            catch (Exception ex) {
                await trsc.RollbackAsync();
                _ = ex;
                return false;
            }
        }

        public async Task<List<ProductoDTO>> ObtenerProductosDeUsuario(Guid UsuarioId)
        {
            var productos = await _context.Producto
                .Where(p => p.VendedorId == UsuarioId)
                .Include(p => p.FotosProducto)
                .Include(p => p.Categoria)
                .OrderByDescending(p => p.FechaPublicacion)
                .Select(p => new ProductoDTO
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    TipoTransaccion = p.TipoTransaccion,
                    Latitud = p.Latitud,
                    Longitud = p.Longitud,
                    VendedorId = p.VendedorId,
                    CategoriaId = p.CategoriaId,
                    NombreCategoria = p.Categoria != null ? p.Categoria.Nombre : null,
                    FechaPublicacion = p.FechaPublicacion,
                    Disponible = p.Disponible,
                    Fotos = p.FotosProducto
                        .OrderBy(f => f.Orden)
                        .Select(f => f.FotoRuta)
                        .ToList()
                })
                .ToListAsync();
            return productos;
        }

        public async Task<ProductoDTO?> ObtenerPorId(Guid productoId)
        {
            var producto = await _context.Producto
                .Include(p => p.FotosProducto)
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == productoId);

            if (producto == null)
                return null;

            return new ProductoDTO
            {
                Id = producto.Id,
                Titulo = producto.Titulo,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                TipoTransaccion = producto.TipoTransaccion,
                Latitud = producto.Latitud,
                Longitud = producto.Longitud,
                VendedorId = producto.VendedorId,
                CategoriaId = producto.CategoriaId,
                NombreCategoria = producto.Categoria != null ? producto.Categoria.Nombre : null,
                FechaPublicacion = producto.FechaPublicacion,
                Disponible = producto.Disponible,
                Fotos = producto.FotosProducto
                    .OrderBy(f => f.Orden)
                    .Select(f => f.FotoRuta)
                    .ToList()
            };
        }

        public List<TipoTransaccionDTO> ObtenerTiposTransaccion() =>
            Enum.GetValues<TipoTransaccion>()
                .Select(t => new TipoTransaccionDTO
                {
                    Id = (int)t,
                    TipoTransaccion = t.ToString().Replace("OVenta", " o Venta")
                })
            .ToList();

        public async Task<PaginatedProductosDTO> BuscarProductos(
            double latitud,
            double longitud,
            double radio,
            int pagina = 1,
            int cantidadPorPagina = 10,
            int? categoriaId = null,
            TipoTransaccion? tipoTransaccion = null)
        {
            // Validar que pagina sea mayor a 0
            if (pagina < 1)
                pagina = 1;

            if (cantidadPorPagina < 1)
                cantidadPorPagina = 10;

            // Obtener todos los productos disponibles con filtros básicos
            var query = _context.Producto
                .Where(p => p.Disponible)
                .Include(p => p.FotosProducto)
                .Include(p => p.Categoria)
                .AsQueryable();

            // Aplicar filtro de categoría si se proporciona
            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            // Aplicar filtro de tipo de transacción si se proporciona
            if (tipoTransaccion.HasValue)
            {
                query = query.Where(p => p.TipoTransaccion == tipoTransaccion.Value);
            }

            // Traer a memoria y filtrar por distancia (fórmula de Haversine simplificada)
            var productosEnMemoria = await query
                .OrderByDescending(p => p.FechaPublicacion)
                .ToListAsync();

            // Calcular distancia usando fórmula de Haversine
            var productosConDistancia = productosEnMemoria
                .Select(p => new
                {
                    Producto = p,
                    Distancia = CalcularDistancia(latitud, longitud, p.Latitud, p.Longitud)
                })
                .Where(x => x.Distancia <= radio)
                .ToList();

            // Contar total registros
            int totalRegistros = productosConDistancia.Count;
            int totalPaginas = (int)Math.Ceiling((double)totalRegistros / cantidadPorPagina);

            // Aplicar paginación
            var productos = productosConDistancia
                .Skip((pagina - 1) * cantidadPorPagina)
                .Take(cantidadPorPagina)
                .Select(x => new ProductoDTO
                {
                    Id = x.Producto.Id,
                    Titulo = x.Producto.Titulo,
                    Descripcion = x.Producto.Descripcion,
                    Precio = x.Producto.Precio,
                    TipoTransaccion = x.Producto.TipoTransaccion,
                    Latitud = x.Producto.Latitud,
                    Longitud = x.Producto.Longitud,
                    VendedorId = x.Producto.VendedorId,
                    CategoriaId = x.Producto.CategoriaId,
                    NombreCategoria = x.Producto.Categoria != null ? x.Producto.Categoria.Nombre : null,
                    FechaPublicacion = x.Producto.FechaPublicacion,
                    Disponible = x.Producto.Disponible,
                    Fotos = x.Producto.FotosProducto
                        .OrderBy(f => f.Orden)
                        .Select(f => f.FotoRuta)
                        .ToList()
                })
                .ToList();

            return new PaginatedProductosDTO
            {
                Productos = productos,
                PaginaActual = pagina,
                CantidadPorPagina = cantidadPorPagina,
                TotalRegistros = totalRegistros,
                TotalPaginas = totalPaginas
            };
        }

        /// <summary>
        /// Calcula la distancia entre dos puntos geográficos usando la fórmula de Haversine
        /// Retorna la distancia en kilómetros
        /// </summary>
        private double CalcularDistancia(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Radio de la Tierra en km

            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distancia = R * c;

            return distancia;
        }


    }
}
