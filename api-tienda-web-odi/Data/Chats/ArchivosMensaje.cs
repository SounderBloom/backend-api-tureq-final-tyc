namespace api_tienda_web_odi.Data.Chats
{
    public class ArchivosMensaje
    {
        public int Id { get; set; }
        public string NombreArchivo { get; set; } = string.Empty;
        public int MensajeId { get; set; }

        // Misma nota: no inicializar con "= new()", MensajeChat.Id es
        // IDENTITY y crearía un mensaje fantasma en cada archivo adjunto.
        public MensajeChat? Mensaje { get; set; }
    }
}
