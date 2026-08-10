using api_tienda_web_odi.Data;
using api_tienda_web_odi.Data.Calificaciones;
using api_tienda_web_odi.Data.Notificacion;
using api_tienda_web_odi.Data.Propuestas;
using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Propuestas;
using Microsoft.EntityFrameworkCore;

namespace api_tienda_web_odi.Service
{
    public class PropuestasService : IPropuestasService
    {
        private readonly AppDbContext _context;
        private readonly IChatsService _chatsService;
        private readonly INotificacionesService _notificacionesService;

        public PropuestasService(
            AppDbContext context,
            IChatsService chatsService,
            INotificacionesService notificacionesService)
        {
            _context = context;
            _chatsService = chatsService;
            _notificacionesService = notificacionesService;
        }

        public async Task<(bool Exito, string Mensaje, PropuestaDTO? Propuesta)> CrearPropuesta(CrearPropuestaDTO dto, Guid proponenteId)
        {
            var chat = await _context.Chat
                .Include(c => c.Producto)
                .FirstOrDefaultAsync(c => c.Id == dto.ChatId);

            if (chat == null || chat.Producto == null || chat.ProductoId == null)
                return (false, "El chat indicado no existe.", null);

            // Solo el interesado (comprador) del chat puede proponer una oferta.
            if (chat.CompradorId != proponenteId)
                return (false, "No puedes hacer una oferta en este chat.", null);

            var vendedorId = chat.Producto.VendedorId;
            var esProductoDeDonacion = chat.Producto.TipoTransaccion == api_tienda_web_odi.Data.Producto.TipoTransaccion.Donar;

            // Un artículo publicado como "Donar" solo admite solicitar la
            // donación (sin producto ofrecido ni monto); los demás tipos de
            // oferta no aplican para un artículo así, y al revés, la
            // solicitud de donación no aplica para artículos que no son de
            // donación.
            if (esProductoDeDonacion && dto.TipoOferta != TipoOferta.SolicitudDonacion)
                return (false, "Este artículo es una donación: solo puedes pedirla, no ofrecer trueque ni compra.", null);

            if (!esProductoDeDonacion && dto.TipoOferta == TipoOferta.SolicitudDonacion)
                return (false, "Este artículo no es una donación.", null);

            // --- Validaciones segun el tipo de oferta ---
            api_tienda_web_odi.Data.Producto.Producto? productoOfrecido = null;

            var requiereProducto = dto.TipoOferta == TipoOferta.Trueque || dto.TipoOferta == TipoOferta.TruequeConDiferencia;
            var requiereMonto = dto.TipoOferta == TipoOferta.Compra || dto.TipoOferta == TipoOferta.TruequeConDiferencia;

            if (requiereProducto)
            {
                if (dto.ProductoOfrecidoId == null)
                    return (false, "Selecciona el artículo que quieres ofrecer.", null);

                productoOfrecido = await _context.Producto
                    .FirstOrDefaultAsync(p => p.Id == dto.ProductoOfrecidoId);

                if (productoOfrecido == null)
                    return (false, "El artículo que quieres ofrecer no existe.", null);

                if (productoOfrecido.VendedorId != proponenteId)
                    return (false, "Solo puedes ofrecer artículos que te pertenecen.", null);
            }
            else if (dto.ProductoOfrecidoId != null)
            {
                return (false, "Este tipo de oferta no debe incluir un artículo ofrecido.", null);
            }

            if (requiereMonto)
            {
                if (dto.Monto == null || dto.Monto <= 0)
                    return (false, "Indica un monto válido.", null);
            }
            else if (dto.Monto != null)
            {
                return (false, "Este tipo de oferta no debe incluir un monto.", null);
            }

            if (dto.TipoOferta == TipoOferta.TruequeConDiferencia && dto.DireccionMonto == null)
                return (false, "Indica quién pone la diferencia en efectivo.", null);

            var yaExistePendiente = await _context.Propuesta.AnyAsync(p =>
                p.ChatId == dto.ChatId &&
                p.Estado == EstadoPropuesta.Pendiente);

            if (yaExistePendiente)
                return (false, "Ya existe una oferta pendiente en este chat.", null);

            var propuesta = new Propuesta
            {
                Id = Guid.NewGuid(),
                ChatId = dto.ChatId,
                ProductoSolicitadoId = chat.ProductoId.Value,
                TipoOferta = dto.TipoOferta,
                ProductoOfrecidoId = requiereProducto ? dto.ProductoOfrecidoId : null,
                Monto = requiereMonto ? dto.Monto : null,
                DireccionMonto = dto.TipoOferta == TipoOferta.TruequeConDiferencia ? dto.DireccionMonto : null,
                ProponenteId = proponenteId,
                VendedorId = vendedorId,
                Mensaje = dto.Mensaje,
                Estado = EstadoPropuesta.Pendiente,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Propuesta.Add(propuesta);
            var result = await _context.SaveChangesAsync();

            if (result <= 0)
                return (false, "No se pudo registrar la oferta.", null);

            await _chatsService.EnviarMensajeSistema(
                propuesta.ChatId,
                "Se envió una nueva oferta: " + DescribirOferta(propuesta, productoOfrecido?.Titulo, chat.Producto.Titulo));

            await _notificacionesService.CrearNotificacion(
                vendedorId,
                "Nueva oferta",
                "Recibiste una oferta por " + chat.Producto.Titulo + ".",
                TipoNotificacion.PropuestaRecibida,
                propuesta.Id);

            var propuestaDto = await ObtenerDto(propuesta.Id, proponenteId);
            return (true, "Oferta enviada correctamente.", propuestaDto);
        }

        public async Task<List<PropuestaDTO>> ObtenerPorChat(Guid chatId, Guid usuarioId)
        {
            var propuestas = await _context.Propuesta
                .Include(p => p.ProductoSolicitado).ThenInclude(pr => pr!.FotosProducto)
                .Include(p => p.ProductoOfrecido).ThenInclude(pr => pr!.FotosProducto)
                .Where(p => p.ChatId == chatId && (p.ProponenteId == usuarioId || p.VendedorId == usuarioId))
                .OrderByDescending(p => p.FechaCreacion)
                .ToListAsync();

            var idsCalificadas = await _context.Calificacion
                .Where(c => propuestas.Select(p => p.Id).Contains(c.PropuestaId))
                .Select(c => c.PropuestaId)
                .ToListAsync();

            return propuestas.Select(p => MapearDto(p, usuarioId, idsCalificadas)).ToList();
        }

        public async Task<(bool Exito, string Mensaje)> Responder(Guid propuestaId, Guid vendedorId, bool aceptar)
        {
            var propuesta = await _context.Propuesta
                .Include(p => p.ProductoSolicitado)
                .Include(p => p.ProductoOfrecido)
                .FirstOrDefaultAsync(p => p.Id == propuestaId);

            if (propuesta == null)
                return (false, "La propuesta no existe.");

            if (propuesta.VendedorId != vendedorId)
                return (false, "No tienes permiso para responder esta propuesta.");

            if (propuesta.Estado != EstadoPropuesta.Pendiente)
                return (false, "Esta propuesta ya fue respondida.");

            propuesta.Estado = aceptar ? EstadoPropuesta.Aceptada : EstadoPropuesta.Rechazada;
            propuesta.FechaResolucion = DateTime.UtcNow;

            if (aceptar)
            {
                if (propuesta.ProductoSolicitado != null)
                    propuesta.ProductoSolicitado.Disponible = false;

                if (propuesta.ProductoOfrecido != null)
                    propuesta.ProductoOfrecido.Disponible = false;
            }

            await _context.SaveChangesAsync();

            var textoEstado = aceptar ? "aceptada" : "rechazada";
            await _chatsService.EnviarMensajeSistema(
                propuesta.ChatId,
                "La oferta fue " + textoEstado + ".");

            await _notificacionesService.CrearNotificacion(
                propuesta.ProponenteId,
                aceptar ? "Oferta aceptada" : "Oferta rechazada",
                aceptar
                    ? "Tu oferta fue aceptada. Ya puedes calificar al vendedor."
                    : "Tu oferta fue rechazada.",
                TipoNotificacion.PropuestaRespondida,
                propuesta.Id);

            return (true, "Respuesta registrada correctamente.");
        }

        public async Task<List<PropuestaDTO>> ObtenerPendientesDeCalificar(Guid usuarioId)
        {
            var aceptadas = await _context.Propuesta
                .Include(p => p.ProductoSolicitado).ThenInclude(pr => pr!.FotosProducto)
                .Include(p => p.ProductoOfrecido).ThenInclude(pr => pr!.FotosProducto)
                .Where(p => p.ProponenteId == usuarioId && p.Estado == EstadoPropuesta.Aceptada)
                .ToListAsync();

            var idsCalificadas = await _context.Calificacion
                .Where(c => c.CalificadorId == usuarioId)
                .Select(c => c.PropuestaId)
                .ToListAsync();

            return aceptadas
                .Where(p => !idsCalificadas.Contains(p.Id))
                .Select(p => MapearDto(p, usuarioId, idsCalificadas))
                .ToList();
        }

        private async Task<PropuestaDTO?> ObtenerDto(Guid propuestaId, Guid usuarioId)
        {
            var propuesta = await _context.Propuesta
                .Include(p => p.ProductoSolicitado).ThenInclude(pr => pr!.FotosProducto)
                .Include(p => p.ProductoOfrecido).ThenInclude(pr => pr!.FotosProducto)
                .FirstOrDefaultAsync(p => p.Id == propuestaId);

            if (propuesta == null)
                return null;

            return MapearDto(propuesta, usuarioId, new List<Guid>());
        }

        private static string DescribirOferta(Propuesta p, string? tituloOfrecido, string tituloSolicitado)
        {
            return p.TipoOferta switch
            {
                TipoOferta.Compra => $"compra de \"{tituloSolicitado}\" por ${p.Monto:0.##}",
                TipoOferta.TruequeConDiferencia => p.DireccionMonto == DireccionMonto.ProponentePagaAlVendedor
                    ? $"\"{tituloOfrecido}\" + ${p.Monto:0.##} a cambio de \"{tituloSolicitado}\""
                    : $"\"{tituloOfrecido}\" a cambio de \"{tituloSolicitado}\" + ${p.Monto:0.##}",
                TipoOferta.SolicitudDonacion => $"solicitud de la donación \"{tituloSolicitado}\"",
                _ => $"\"{tituloOfrecido}\" a cambio de \"{tituloSolicitado}\""
            };
        }

        private static PropuestaDTO MapearDto(Propuesta p, Guid usuarioId, List<Guid> idsCalificadas)
        {
            return new PropuestaDTO
            {
                Id = p.Id,
                ChatId = p.ChatId,
                ProductoSolicitadoId = p.ProductoSolicitadoId,
                ProductoSolicitadoTitulo = p.ProductoSolicitado?.Titulo ?? "",
                ProductoSolicitadoFoto = p.ProductoSolicitado?.FotosProducto
                    .OrderBy(f => f.Orden).Select(f => f.FotoRuta).FirstOrDefault(),
                TipoOferta = p.TipoOferta,
                ProductoOfrecidoId = p.ProductoOfrecidoId,
                ProductoOfrecidoTitulo = p.ProductoOfrecido?.Titulo,
                ProductoOfrecidoFoto = p.ProductoOfrecido?.FotosProducto
                    .OrderBy(f => f.Orden).Select(f => f.FotoRuta).FirstOrDefault(),
                Monto = p.Monto,
                DireccionMonto = p.DireccionMonto,
                ProponenteId = p.ProponenteId,
                VendedorId = p.VendedorId,
                Mensaje = p.Mensaje,
                Estado = p.Estado,
                FechaCreacion = p.FechaCreacion,
                FechaResolucion = p.FechaResolucion,
                PuedeCalificar = p.Estado == EstadoPropuesta.Aceptada &&
                    p.ProponenteId == usuarioId &&
                    !idsCalificadas.Contains(p.Id)
            };
        }
    }
}
