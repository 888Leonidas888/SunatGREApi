using Microsoft.EntityFrameworkCore;
using SunatGreApi.Models;

namespace SunatGreApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Guia> Guias { get; set; }
        public DbSet<GuiaBien> GuiaBienes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configuración adicional si fuera necesaria
            modelBuilder.Entity<Guia>()
                .HasIndex(g => new { g.RucEmisor, g.TipoDocumento, g.Serie, g.Numero })
                .IsUnique();

            modelBuilder.Entity<GuiaBien>()
                .HasOne(b => b.Guia)
                .WithMany(g => g.Bienes)
                .HasForeignKey(b => b.GuiaId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
