using api_tienda_web_odi.Data;
using api_tienda_web_odi.Data.Calificaciones;
using api_tienda_web_odi.Data.Notificacion;
using api_tienda_web_odi.Data.Propuestas;
using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Calificaciones;
using Microsoft.EntityFrameworkCore;

namespace api_tienda_web_odi.Service
{
    public class CalificacionesService : ICalificacionesService
    {
        private readonly AppDbContext _context;
        private readonly INotificacionesService _notificacionesService;

        public CalificacionesService(AppDbContext context, INotificacionesService notificacionesService)
        {
            _context = context;
            _notificacionesService = notificacionesService;
        }

        public async Task<(bool Exito, string Mensaje)> CrearCalificacion(CrearCalificacionDTO dto, Guid calificadorId)
        {
            if (dto.Estrellas < 1 || dto.Estrellas > 5)
                return (false, "La calificacion debe ser de 1 a 5 estrellas.");

            var propuesta = await _context.Propuesta
                .FirstOrDefaultAsync(p => p.Id == dto.PropuestaId);

            if (propuesta == null)
                return (false, "La propuesta indicada no existe.");

            if (propuesta.Estado != EstadoPropuesta.Aceptada)
                return (false, "Solo puedes calificar propuestas de trueque aceptadas.");

            if (propuesta.ProponenteId != calificadorId)
                return (false, "No tienes permiso para calificar esta propuesta.");

            var yaCalifico = await _context.Calificacion.AnyAsync(c => c.PropuestaId == dto.PropuestaId);
            if (yaCalifico)
                return (false, "Ya calificaste esta propuesta.");

            _context.Calificacion.Add(new Calificacion
            {
                PropuestaId = dto.PropuestaId,
                CalificadorId = calificadorId,
                CalificadoId = propuesta.VendedorId,
                Estrellas = dto.Estrellas,
                Comentario = dto.Comentario,
                FechaCreacion = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _notificacionesService.CrearNotificacion(
                propuesta.VendedorId,
                "Nueva calificacion",
                "Recibiste una calificacion de " + dto.Estrellas + " estrellas.",
                TipoNotificacion.CalificacionRecibida,
                propuesta.Id);

            return (true, "Calificacion registrada correctamente.");
        }

        public async Task<ResumenCalificacionesDTO> ObtenerDeUsuario(Guid usuarioId)
        {
            var calificaciones = await _context.Calificacion
                .Include(c => c.Calificador)
                .Where(c => c.CalificadoId == usuarioId)
                .OrderByDescending(c => c.FechaCreacion)
                .ToListAsync();

            return new ResumenCalificacionesDTO
            {
                Promedio = calificaciones.Count > 0 ? Math.Round(calificaciones.Average(c => c.Estrellas), 1) : 0,
                Total = calificaciones.Count,
                Recientes = calificaciones.Take(10).Select(c => new CalificacionDTO
                {
                    Id = c.Id,
                    CalificadorId = c.CalificadorId,
                    CalificadorNombre = c.Calificador != null ? c.Calificador.Nombre : "Usuario",
                    Estrellas = c.Estrellas,
                    Comentario = c.Comentario,
                    FechaCreacion = c.FechaCreacion
                }).ToList()
            };
        }
    }
}
