using api_tienda_web_odi.Data;
using api_tienda_web_odi.Data.Auth;
using api_tienda_web_odi.Data.Propuestas;
using api_tienda_web_odi.Infraestructure;
using api_tienda_web_odi.Models.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace api_tienda_web_odi.Service
{
    public class UsuariosService : IUsuariosService
    {
        private readonly AppDbContext _context;

        public UsuariosService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PerfilUsuarioDTO?> ObtenerPerfil(Guid usuarioId)
        {
            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                return null;

            var truequesRealizados = await _context.Propuesta.CountAsync(p =>
                p.Estado == EstadoPropuesta.Aceptada &&
                (p.ProponenteId == usuarioId || p.VendedorId == usuarioId));

            var articulosActivos = await _context.Producto.CountAsync(p =>
                p.VendedorId == usuarioId && p.Disponible);

            var calificaciones = await _context.Calificacion
                .Where(c => c.CalificadoId == usuarioId)
                .Select(c => c.Estrellas)
                .ToListAsync();

            return new PerfilUsuarioDTO
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                ApellidoPaterno = usuario.ApellidoPaterno,
                ApellidoMaterno = usuario.ApellidoMaterno,
                Correo = usuario.Correo,
                Biografia = usuario.Biografia,
                FotoPerfilUrl = usuario.FotoPerfilUrl,
                FechaRegistro = usuario.FechaRegistro,
                Rol = usuario.Rol.ToString(),
                TruequesRealizados = truequesRealizados,
                ArticulosActivos = articulosActivos,
                PromedioCalificacion = calificaciones.Count > 0 ? Math.Round(calificaciones.Average(), 1) : 0,
                TotalCalificaciones = calificaciones.Count
            };
        }

        public async Task<List<UsuarioAdminDTO>> ObtenerTodos()
        {
            return await _context.Usuario
                .OrderBy(u => u.Nombre)
                .Select(u => new UsuarioAdminDTO
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    ApellidoPaterno = u.ApellidoPaterno,
                    ApellidoMaterno = u.ApellidoMaterno,
                    Correo = u.Correo,
                    Rol = u.Rol.ToString(),
                    Activo = u.Activo,
                    FechaRegistro = u.FechaRegistro
                })
                .ToListAsync();
        }

        public async Task<(bool Exito, string Mensaje)> CambiarRol(Guid usuarioId, Rol nuevoRol, Guid solicitanteId)
        {
            if (!Enum.IsDefined(nuevoRol))
                return (false, "El rol indicado no es valido.");

            if (usuarioId == solicitanteId)
                return (false, "No puedes cambiar tu propio rol.");

            var usuario = await _context.Usuario.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario == null)
                return (false, "El usuario indicado no existe.");

            if (usuario.Rol == nuevoRol)
                return (false, "El usuario ya tiene ese rol.");

            usuario.Rol = nuevoRol;
            await _context.SaveChangesAsync();

            return (true, "Rol actualizado correctamente.");
        }
    }
}
