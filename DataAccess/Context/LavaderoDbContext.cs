using Microsoft.EntityFrameworkCore;
using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.DataAccess.Context;

public sealed class LavaderoDbContext(DbContextOptions<LavaderoDbContext> options)
    : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Recordatorio> Recordatorios => Set<Recordatorio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Apellido).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EmailContacto).HasMaxLength(200).UseCollation("NOCASE").IsRequired();
            entity.Property(x => x.Telefono).HasMaxLength(30);
            entity.HasIndex(x => x.EmailContacto).IsUnique();
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable("Servicios", table =>
                table.HasCheckConstraint("CK_Servicios_Importe_Positivo", "Importe > 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nombre).HasMaxLength(150).UseCollation("NOCASE").IsRequired();
            entity.Property(x => x.Importe).HasPrecision(12, 2);
            entity.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.ToTable("Turnos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Estado).HasConversion<int>();
            entity.HasIndex(x => x.FechaHora)
                .IsUnique()
                .HasFilter("Estado = 1");

            entity.HasOne(x => x.Cliente)
                .WithMany(x => x.Turnos)
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Servicio)
                .WithMany(x => x.Turnos)
                .HasForeignKey(x => x.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(200).UseCollation("NOCASE").IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Rol).HasConversion<int>();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.ClienteId).IsUnique();

            entity.HasOne(x => x.Cliente)
                .WithOne(x => x.Usuario)
                .HasForeignKey<Usuario>(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recordatorio>(entity =>
        {
            entity.ToTable("Recordatorios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Estado).HasConversion<int>();
            entity.Property(x => x.Detalle).HasMaxLength(500);
            entity.HasIndex(x => new { x.TurnoId, x.FechaProgramada }).IsUnique();

            entity.HasOne(x => x.Turno)
                .WithMany(x => x.Recordatorios)
                .HasForeignKey(x => x.TurnoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
