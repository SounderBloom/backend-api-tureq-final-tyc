using api_tienda_web_odi.Data;
using api_tienda_web_odi.Data.Chats;
using api_tienda_web_odi.Data.Notificacion;
using api_tienda_web_odi.Data.Producto;
using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Chats;
using Microsoft.EntityFrameworkCore;

namespace api_tienda_web_odi.Service
{
    public class ChatsService : IChatsService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly INotificacionesService _notificacionesService;

        public ChatsService(
            AppDbContext context, 
            IWebHostEnvironment environment,
            INotificacionesService notificacionesService)
        {
            _context = context;
            _environment = environment;
            _notificacionesService = notificacionesService;
        }
        public async Task<bool> BorrarChat(Guid UsuarioId, Guid ChatId)
        {
            var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var chat = await _context.Chat.FirstOrDefaultAsync(x => x.Id == ChatId);
                if (chat == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                if (chat.CompradorId == UsuarioId)
                {
                    if (chat.VisibleParaVendedor)
                    {
                        chat.VisibleParaComprador = false;

                        var result = await _context.SaveChangesAsync();
                        if (result > 0)
                        {
                            await transaction.CommitAsync();
                            return true;
                        }
                        return false;
                    } 
                    else
                    {
                        _context.Chat.Remove(chat);
                        var result = await _context.SaveChangesAsync();
                        if (result > 0)
                        {
                            await transaction.CommitAsync();
                            var carpetaChat = Path.Combine(
                                _environment.ContentRootPath,
                                "PrivateUserFiles",
                                "ChatsUsuarios",
                                chat.Id.ToString()
                            );
                            if (Directory.Exists(carpetaChat))
                                Directory.Delete(carpetaChat, true);
                            return true;
                        }
                        return false;
                    }
                }
                else
                {
                    var producto = await _context.Producto.FirstOrDefaultAsync(x => x.Id == chat.ProductoId);
                    if (producto == null)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    if (producto.VendedorId == UsuarioId)
                    {
                        if (chat.VisibleParaComprador)
                        {
                            chat.VisibleParaVendedor = false;
                            var result = await _context.SaveChangesAsync();
                            if (result > 0)
                            {
                                await transaction.CommitAsync();
                                return true;
                            }
                            return false;
                        }
                        else
                        {
                            _context.Chat.Remove(chat);
                            var result = await _context.SaveChangesAsync();
                            if (result > 0)
                            {
                                await transaction.CommitAsync();
                                var carpetaChat = Path.Combine(
                                    _environment.ContentRootPath,
                                    "PrivateUserFiles",
                                    "ChatsUsuarios",
                                    chat.Id.ToString()
                                );
                                if (Directory.Exists(carpetaChat))
                                    Directory.Delete(carpetaChat, true);
                                return true;
                            }
                            return false;
                        }
                    }
                    await transaction.RollbackAsync();
                    return false;
                }
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<List<ChatDTO>> ObtenerChats(Guid UsuarioId, int iteracion = 0)
        {
            int cantidadPorPagina = 10;
            int saltar = iteracion * cantidadPorPagina;

            var chats = await (
                from c in _context.Chat
                join p in _context.Producto on c.ProductoId equals p.Id
                join m in _context.MensajeChat on c.Id equals m.ChatId into mensajes
                join comprador in _context.Usuario on c.CompradorId equals comprador.Id
                join vendedor in _context.Usuario on p.VendedorId equals vendedor.Id
                where 
                    (c.CompradorId == UsuarioId && c.VisibleParaComprador) || 
                    (p.VendedorId == UsuarioId && c.VisibleParaVendedor)
                select new ChatDTO
                {
                    Id = c.Id,
                    ImagenProductoSnapshot = c.ImagenProductoSnapshot,
                    NombreProductoSnapshot = c.NombreProductoSnapshot,
                    TipoTransaccionProductoSnapshot = c.TipoTransaccionProductoSnapshot,
                    ProductoId = p.Id,
                    EsVendedor = p.VendedorId == UsuarioId,
                    UrlFotoUsuario = UsuarioId == c.CompradorId ? vendedor.FotoPerfilUrl : comprador.FotoPerfilUrl,
                    UltimoMensaje = mensajes.OrderByDescending(x => x.FechaEnvio).Select(x => new MensajeDTO
                    {
                        Id = x.Id,
                        Contenido = x.Contenido,
                        FechaEnvio = x.FechaEnvio,
                        Emisor = x.Emisor,
                        Estado = x.Estado
                    }).FirstOrDefault(),
                    UltimoMovimiento = mensajes.Any() ? mensajes.Max(x => x.FechaEnvio) : c.FechaCreacion
                })
                .OrderByDescending(x => x.UltimoMovimiento)
                .Skip(saltar)
                .Take(cantidadPorPagina)
                .ToListAsync();

            return chats;
        }

        public async Task<List<MensajeDTO>?> ObtenerMensajes(Guid chatId, Guid usuarioId)
        {
            var chat = await _context.Chat
                .Include(c => c.Producto)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
                return null;

            var esComprador = chat.CompradorId == usuarioId;
            var esVendedor = chat.Producto != null && chat.Producto.VendedorId == usuarioId;

            if (!esComprador && !esVendedor)
                return null;

            return await _context.MensajeChat
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.FechaEnvio)
                .Select(m => new MensajeDTO
                {
                    Id = m.Id,
                    Contenido = m.Contenido,
                    FechaEnvio = m.FechaEnvio,
                    Emisor = m.Emisor,
                    Estado = m.Estado,
                    TieneArchivos = m.Archivos.Any()
                })
                .ToListAsync();
        }

        public async Task<Guid?> CrearChat(Guid InteresadoId, Guid ProductoId)
        {
            // Reutilizar el chat si ya existe uno entre este usuario y este
            // producto, en vez de crear uno nuevo cada vez que se entra al
            // detalle del producto o se pide una oferta.
            var chatExistente = await _context.Chat
                .FirstOrDefaultAsync(c => c.ProductoId == ProductoId && c.CompradorId == InteresadoId);

            if (chatExistente != null)
            {
                // Si el usuario lo había ocultado de su lista, se vuelve a mostrar.
                if (!chatExistente.VisibleParaComprador)
                {
                    chatExistente.VisibleParaComprador = true;
                    await _context.SaveChangesAsync();
                }
                return chatExistente.Id;
            }

            var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Producto
                    .Include(p => p.FotosProducto)
                    .FirstOrDefaultAsync(x => x.Id == ProductoId);

                if (producto == null)
                {
                    await transaction.RollbackAsync();
                    return null;
                }

                var primeraFoto = producto.FotosProducto
                    .OrderBy(f => f.Orden)
                    .Select(f => f.FotoRuta)
                    .FirstOrDefault() ?? "";

                var chat = new Chat
                {
                    CompradorId = InteresadoId,
                    ProductoId = ProductoId,
                    ImagenProductoSnapshot = primeraFoto,
                    NombreProductoSnapshot = producto.Titulo,
                    TipoTransaccionProductoSnapshot = producto.TipoTransaccion,
                    PrecioProductoSnapshot = producto.Precio
                };
                _context.Chat.Add(chat);

                var result = await _context.SaveChangesAsync();
                if (result <= 0) {
                    await transaction.RollbackAsync();
                    return null;
                }
                await transaction.CommitAsync();
                return chat.Id;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al crear chat: {ex}");
                await transaction.RollbackAsync();
                return null;
            }
        }

        public async Task<bool> EnviarMensaje(CrearMensajeDTO Mensaje, Guid EmisorId)
        {
            var transaction = await _context.Database.BeginTransactionAsync();
            var carpetaArchivo = string.Empty;
            try
            {
                EmisorMensaje ValorEmisorMensaje = EmisorMensaje.Sistema;
                Guid? destinatarioId = null;

                var chatOrigen = await _context.Chat
                    .Include(c => c.Producto)
                    .FirstOrDefaultAsync(x => x.Id == Mensaje.ChatId);

                if (!Mensaje.EsSistema)
                {
                    if (chatOrigen == null || chatOrigen.Producto == null)
                    {
                        Console.Error.WriteLine(
                            $"EnviarMensaje: chat {Mensaje.ChatId} no existe o no tiene producto asociado " +
                            $"(chatOrigen null: {chatOrigen == null}, producto null: {chatOrigen?.Producto == null}).");
                        await transaction.RollbackAsync();
                        return false;
                    }
                    if (chatOrigen.CompradorId == EmisorId)
                    {
                        ValorEmisorMensaje = EmisorMensaje.Comprador;
                        destinatarioId = chatOrigen.Producto.VendedorId;
                    }
                    else if (chatOrigen.Producto.VendedorId == EmisorId)
                    {
                        ValorEmisorMensaje = EmisorMensaje.Vendedor;
                        destinatarioId = chatOrigen.CompradorId;
                    }
                    else
                    {
                        Console.Error.WriteLine(
                            $"EnviarMensaje: el usuario {EmisorId} no es ni el comprador ({chatOrigen.CompradorId}) " +
                            $"ni el vendedor ({chatOrigen.Producto.VendedorId}) de este chat.");
                        await transaction.RollbackAsync();
                        return false;
                    }
                }

                var MensajeBD = await _context.MensajeChat.AddAsync(new MensajeChat
                {
                    ChatId = Mensaje.ChatId,
                    Estado = 0,
                    Contenido = Mensaje.Mensaje,
                    Emisor = ValorEmisorMensaje
                });

                var result = await _context.SaveChangesAsync();
                if (result <= 0)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                if (Mensaje.Archivos != null && Mensaje.Archivos.Any())
                {
                    var contador = 0;
                    foreach (var archivo in Mensaje.Archivos)
                    {
                        contador++;
                        string nombreArchivo =
                            $"{contador}{Path.GetExtension(archivo.FileName)}";

                        carpetaArchivo = Path.Combine(
                            _environment.ContentRootPath,
                            "PrivateUserFiles",
                            "ChatsUsuarios",
                            Mensaje.ChatId.ToString(),
                            MensajeBD.Entity.Id.ToString()
                        );

                        Directory.CreateDirectory(carpetaArchivo);

                        string rutaFisica = Path.Combine(
                            carpetaArchivo,
                            nombreArchivo
                        );

                        await using (FileStream stream = new(rutaFisica, FileMode.Create))
                        {
                            await archivo.CopyToAsync(stream);
                        }

                        _context.ArchivosMensaje.Add(new ArchivosMensaje
                        {
                            MensajeId = MensajeBD.Entity.Id,
                            NombreArchivo = nombreArchivo
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (destinatarioId.HasValue && chatOrigen != null)
                {
                    await _notificacionesService.CrearNotificacion(
                        destinatarioId.Value,
                        "Mensaje nuevo",
                        "Nuevo mensaje sobre " + chatOrigen.NombreProductoSnapshot,
                        TipoNotificacion.MensajeNuevo,
                        chatOrigen.Id
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar mensaje: {ex}");
                await transaction.RollbackAsync();
                if (!string.IsNullOrWhiteSpace(carpetaArchivo)) Directory.Delete(carpetaArchivo, true);
                return false;
            }
        }

        public async Task EnviarMensajeSistema(Guid chatId, string contenido)
        {
            _context.MensajeChat.Add(new MensajeChat
            {
                ChatId = chatId,
                Estado = 0,
                Contenido = contenido,
                Emisor = EmisorMensaje.Sistema
            });

            await _context.SaveChangesAsync();
        }
    }
}
