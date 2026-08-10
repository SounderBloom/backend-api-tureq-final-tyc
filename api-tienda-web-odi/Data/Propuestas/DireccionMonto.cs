namespace api_tienda_web_odi.Data.Propuestas
{
    // Solo aplica cuando TipoOferta == TruequeConDiferencia: indica quien
    // pone el dinero de la diferencia entre los dos productos.
    public enum DireccionMonto
    {
        // El proponente paga el monto extra al vendedor, ademas de su producto.
        ProponentePagaAlVendedor = 0,
        // El proponente pide que el vendedor le pague el monto extra a el.
        VendedorPagaAlProponente = 1
    }
}
