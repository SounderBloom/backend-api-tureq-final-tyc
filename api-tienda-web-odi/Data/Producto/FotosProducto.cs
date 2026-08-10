namespace api_tienda_web_odi.Data.Producto
{
    public class FotosProducto
    {
        public int Id { get; set; }
        public Guid ProductoId { get; set; }

        // OJO: nunca inicializar esto con "= new Producto()". Un valor por
        // defecto así hace que EF Core, al no ver el navegador seteado
        // explícitamente, trate a esa instancia vacía (Id = Guid.Empty,
        // Titulo/Descripcion = null) como una entidad NUEVA que hay que
        // insertar, porque Producto.Id está configurado como
        // ValueGeneratedOnAdd(). Eso rompía "Publicar artículo" con un error
        // de SQL Server 515 (NULL en columna NOT NULL) cada vez que se subía
        // al menos una foto.
        public Producto? Producto { get; set; }
        public string FotoRuta { get; set; } = string.Empty;
        public int Orden { get; set; }
    }
}
