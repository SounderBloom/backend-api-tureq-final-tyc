namespace api_tienda_web_odi.Data.Propuestas
{
    public enum TipoOferta
    {
        // Solo intercambio de productos, sin dinero de por medio.
        Trueque = 0,
        // Solo compra en efectivo, sin producto ofrecido.
        Compra = 1,
        // Intercambio de productos + una diferencia en efectivo (ver DireccionMonto).
        TruequeConDiferencia = 2,
        // Pedir un producto publicado como Donar (TipoTransaccion.Donar): sin
        // producto ofrecido ni monto, solo la solicitud para que el vendedor
        // decida a quién se lo dona.
        SolicitudDonacion = 3
    }
}
