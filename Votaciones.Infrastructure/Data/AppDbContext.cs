using Microsoft.EntityFrameworkCore;
using Votaciones.Domain.Models;

namespace Votaciones.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Eleccion> Elecciones { get; set; }
        public DbSet<ListaElectoral> ListasElectorales { get; set; }
        public DbSet<Candidato> Candidatos { get; set; }
        public DbSet<ActaEleccion> ActasEleccion { get; set; }
        public DbSet<ActaDetalle> ActasDetalle { get; set; }
        public DbSet<MesaElectoral> MesasElectorales { get; set; }
        //ADM
        public DbSet<AdmUsuario> Usuarios { get; set; }
        public DbSet<AdmRol> Roles { get; set; }
        public DbSet<AdmBitacora> Bitacora { get; set; }
        //Zonas
        public DbSet<Provincia> Provincias { get; set; }
        public DbSet<Canton> Cantones { get; set; }
        public DbSet<Parroquia> Parroquias { get; set; }
        public DbSet<Zona> Zonas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //AdmBitacora 
            modelBuilder.Entity<AdmBitacora>(entity =>
            {
                entity.Property(x => x.ValoresAnteriores)
                .HasColumnType("nvarchar(max)");
                entity.Property(x => x.ValoresNuevos)
                    .HasColumnType("nvarchar(max)");

                entity.ToTable("AdmBitacora", table =>
                {
                    table.HasCheckConstraint(
                        "CK_AdmBitacora_ValoresAnteriores_Json",
                        "[ValoresAnteriores] IS NULL OR ISJSON([ValoresAnteriores]) = 1"
                    );
                    table.HasCheckConstraint(
                        "CK_AdmBitacora_ValoresNuevos_Json",
                        "[ValoresNuevos] IS NULL OR ISJSON([ValoresNuevos]) = 1"
                    );
                });
            });

            //Usuarios
            //Relacion Rol - Usuario
            modelBuilder.Entity<AdmUsuario>()
                .HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict); // No se permite eliminar un rol si tiene usuario asociados

            //Relacion MesaElectoral - Eleccion
            modelBuilder.Entity<MesaElectoral>()
                .HasOne(f => f.Eleccion)
                .WithMany()
                .HasForeignKey(f => f.EleccionId)
                .OnDelete(DeleteBehavior.Restrict); // No se permite eliminar una elección si tiene mesas asociadas

            // Relacion Candidatos - Eleccion
            modelBuilder.Entity<Candidato>()
                .HasOne(c => c.Eleccion)
                .WithMany()
                .HasForeignKey(c => c.EleccionId)
                .OnDelete(DeleteBehavior.Restrict); // No se permite eliminar una eleccion si tiene candidatos asociados
            // Relacion Candidatos - Lista
            modelBuilder.Entity<Candidato>()
                .HasOne(c => c.ListaElectoral)
                .WithMany()
                .HasForeignKey(c => c.ListaElectoralId)
                .OnDelete(DeleteBehavior.Restrict); // No se permite eliminar una lista si tiene candidatos asociados

            // Relacion ActaCab - MesaElectoral
            modelBuilder.Entity<ActaEleccion>()
                .HasOne(f => f.Eleccion)
                .WithMany()
                .HasForeignKey(f => f.EleccionId)
                .OnDelete(DeleteBehavior.Restrict); // No se permite eliminar una eleccion si tiene actas asociadas

            // Relacion ActaCab - MesaElectoral
            modelBuilder.Entity<ActaEleccion>()
                .HasOne(f => f.MesaElectoral)
                .WithMany()
                .HasForeignKey(f => f.MesaElectoralId)
                .OnDelete(DeleteBehavior.Restrict); // No se permite eliminar una mesa si tiene actas asociadas

            // Relación ActaCab - ActaDetalle
            modelBuilder.Entity<ActaDetalle>()
                .HasOne(f => f.ActaCab)
                .WithMany(f => f.ActaDetalles) 
                .HasForeignKey(f => f.ActaId)
                .OnDelete(DeleteBehavior.Cascade); // Si se elimina una acta, se eliminan sus detalles

        }
    }
}
