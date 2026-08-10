using api_tienda_web_odi.Data.Auth;
using api_tienda_web_odi.Data.Calificaciones;
using api_tienda_web_odi.Data.Chats;
using api_tienda_web_odi.Data.Notificacion;
using api_tienda_web_odi.Data.Producto;
using api_tienda_web_odi.Data.Propuestas;
using Microsoft.EntityFrameworkCore;

namespace api_tienda_web_odi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { 
        }

        #region Auth
        public DbSet<Usuario> Usuario { get; set; }
        #endregion

        #region Chats
        public DbSet<Chat> Chat { get; set; }
        public DbSet<MensajeChat> MensajeChat { get; set; }
        public DbSet<ArchivosMensaje> ArchivosMensaje { get; set; }
        #endregion

        #region Producto
        public DbSet<FotosProducto> FotosProducto { get; set; }
        public DbSet<Producto.Producto> Producto { get; set; }
        #endregion

        #region Notificaciones
        public DbSet<Notificaciones> Notificaciones { get; set; }
        #endregion

        #region Propuestas
        public DbSet<Propuesta> Propuesta { get; set; }
        #endregion

        #region Calificaciones
        public DbSet<Calificacion> Calificacion { get; set; }
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Usuario -> Productos
            modelBuilder.Entity<Producto.Producto>()
                .HasOne(p => p.Vendedor)
                .WithMany(u => u.Productos)
                .HasForeignKey(p => p.VendedorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Producto -> Chats
            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Producto)
                .WithMany(p => p.Chats)
                .HasForeignKey(c => c.ProductoId)
                .OnDelete(DeleteBehavior.NoAction);

            // Usuario (El que pregunta por el producto) -> Chats
            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Comprador)
                .WithMany(u => u.ChatsComprador)
                .HasForeignKey(c => c.CompradorId)
                .OnDelete(DeleteBehavior.NoAction);

            // --- Propuesta ---
            // Múltiples FKs hacia Usuario/Producto/Chat en la misma entidad:
            // se usa NoAction en todas para evitar rutas de cascada múltiples
            // (SQL Server no permite varios caminos de ON DELETE CASCADE
            // hacia la misma tabla).
            modelBuilder.Entity<Propuesta>()
                .HasOne(pr => pr.Chat)
                .WithMany()
                .HasForeignKey(pr => pr.ChatId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Propuesta>()
                .HasOne(pr => pr.ProductoSolicitado)
                .WithMany()
                .HasForeignKey(pr => pr.ProductoSolicitadoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Propuesta>()
                .HasOne(pr => pr.ProductoOfrecido)
                .WithMany()
                .HasForeignKey(pr => pr.ProductoOfrecidoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Propuesta>()
                .HasOne(pr => pr.Proponente)
                .WithMany()
                .HasForeignKey(pr => pr.ProponenteId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Propuesta>()
                .HasOne(pr => pr.Vendedor)
                .WithMany()
                .HasForeignKey(pr => pr.VendedorId)
                .OnDelete(DeleteBehavior.NoAction);

            // --- Calificacion ---
            modelBuilder.Entity<Calificacion>()
                .HasOne(c => c.Propuesta)
                .WithMany()
                .HasForeignKey(c => c.PropuestaId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Calificacion>()
                .HasIndex(c => c.PropuestaId)
                .IsUnique();

            modelBuilder.Entity<Calificacion>()
                .HasOne(c => c.Calificador)
                .WithMany()
                .HasForeignKey(c => c.CalificadorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Calificacion>()
                .HasOne(c => c.Calificado)
                .WithMany()
                .HasForeignKey(c => c.CalificadoId)
                .OnDelete(DeleteBehavior.NoAction);

            // --- Notificaciones ---
            modelBuilder.Entity<Notificaciones>()
                .HasOne(n => n.UsuarioNotificado)
                .WithMany()
                .HasForeignKey(n => n.UsuarioNotificadoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
