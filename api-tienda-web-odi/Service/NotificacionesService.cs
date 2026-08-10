using api_tienda_web_odi.Data;
using api_tienda_web_odi.Data.Notificacion;
using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Notificaciones;
using Microsoft.EntityFrameworkCore;

namespace api_tienda_web_odi.Service
{
    public class NotificacionesService : INotificacionesService
    {
        private readonly AppDbContext _context;

        public NotificacionesService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CrearNotificacion(
            Guid usuarioId,
            string titulo,
            string contenido,
            TipoNotificacion tipo,
            Guid? referenciaId = null,
            string urlImagenIcono = "")
        {
            _context.Notificaciones.Add(new Notificaciones
            {
                UsuarioNotificadoId = usuarioId,
                Titulo = titulo,
                Contenido = contenido,
                Tipo = tipo,
                ReferenciaId = referenciaId,
                UrlImagenIcono = urlImagenIcono,
                Leida = false,
                FechaCreacion = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificacionDTO>> ObtenerNotificaciones(Guid usuarioId, int pagina = 0)
        {
            const int cantidadPorPagina = 20;

            return await _context.Notificaciones
                .Where(n => n.UsuarioNotificadoId == usuarioId)
                .OrderByDescending(n => n.FechaCreacion)
                .Skip(pagina * cantidadPorPagina)
                .Take(cantidadPorPagina)
                .Select(n => new NotificacionDTO
                {
                    Id = n.Id,
                    Titulo = n.Titulo,
                    Contenido = n.Contenido,
                    Leida = n.Leida,
                    Tipo = n.Tipo,
                    ReferenciaId = n.ReferenciaId,
                    FechaCreacion = n.FechaCreacion,
                    UrlImagenIcono = n.UrlImagenIcono
                })
                .ToListAsync();
        }

        public async Task<int> ContarNoLeidas(Guid usuarioId)
        {
            return await _context.Notificaciones
                .CountAsync(n => n.UsuarioNotificadoId == usuarioId && !n.Leida);
        }

        public async Task<bool> MarcarLeida(Guid usuarioId, int notificacionId)
        {
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == notificacionId && n.UsuarioNotificadoId == usuarioId);

            if (notificacion == null)
                return false;

            notificacion.Leida = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarcarTodasLeidas(Guid usuarioId)
        {
            await _context.Notificaciones
                .Where(n => n.UsuarioNotificadoId == usuarioId && !n.Leida)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.Leida, true));

            return true;
        }
    }
}
