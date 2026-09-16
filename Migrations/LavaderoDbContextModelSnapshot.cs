using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TurnosLavadero.DataAccess.Context;

#nullable disable

namespace TurnosLavadero.DataAccess.Migrations;

[DbContext(typeof(LavaderoDbContext))]
partial class LavaderoDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.11");

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Cliente", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<bool>("Activo").HasColumnType("INTEGER");
            b.Property<string>("Apellido").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
            b.Property<string>("EmailContacto").IsRequired().HasMaxLength(200).HasColumnType("TEXT").UseCollation("NOCASE");
            b.Property<string>("Nombre").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
            b.Property<bool>("NotificacionesHabilitadas").HasColumnType("INTEGER");
            b.Property<string>("Telefono").HasMaxLength(30).HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("EmailContacto").IsUnique();
            b.ToTable("Clientes");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Servicio", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<bool>("Activo").HasColumnType("INTEGER");
            b.Property<decimal>("Importe").HasPrecision(12, 2).HasColumnType("TEXT");
            b.Property<string>("Nombre").IsRequired().HasMaxLength(150).HasColumnType("TEXT").UseCollation("NOCASE");
            b.HasKey("Id");
            b.HasIndex("Nombre").IsUnique();
            b.ToTable("Servicios", t =>
            {
                t.HasCheckConstraint("CK_Servicios_Importe_Positivo", "Importe > 0");
            });
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Turno", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<Guid>("ClienteId").HasColumnType("TEXT");
            b.Property<int>("Estado").HasColumnType("INTEGER");
            b.Property<DateTimeOffset>("FechaCreacion").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("FechaHora").HasColumnType("TEXT");
            b.Property<Guid>("ServicioId").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("ClienteId");
            b.HasIndex("ServicioId");
            b.HasIndex("FechaHora").IsUnique().HasFilter("Estado = 1");
            b.ToTable("Turnos");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Usuario", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<bool>("Activo").HasColumnType("INTEGER");
            b.Property<Guid?>("ClienteId").HasColumnType("TEXT");
            b.Property<string>("Email").IsRequired().HasMaxLength(200).HasColumnType("TEXT").UseCollation("NOCASE");
            b.Property<string>("PasswordHash").IsRequired().HasMaxLength(500).HasColumnType("TEXT");
            b.Property<int>("Rol").HasColumnType("INTEGER");
            b.HasKey("Id");
            b.HasIndex("ClienteId").IsUnique();
            b.HasIndex("Email").IsUnique();
            b.ToTable("Usuarios");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Recordatorio", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<string>("Detalle").HasMaxLength(500).HasColumnType("TEXT");
            b.Property<int>("Estado").HasColumnType("INTEGER");
            b.Property<DateTimeOffset?>("FechaProcesada").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("FechaProgramada").HasColumnType("TEXT");
            b.Property<Guid>("TurnoId").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("TurnoId", "FechaProgramada").IsUnique();
            b.ToTable("Recordatorios");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Turno", b =>
        {
            b.HasOne("TurnosLavadero.DataAccess.Entities.Cliente", "Cliente")
                .WithMany("Turnos")
                .HasForeignKey("ClienteId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("TurnosLavadero.DataAccess.Entities.Servicio", "Servicio")
                .WithMany("Turnos")
                .HasForeignKey("ServicioId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Cliente");
            b.Navigation("Servicio");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Usuario", b =>
        {
            b.HasOne("TurnosLavadero.DataAccess.Entities.Cliente", "Cliente")
                .WithOne("Usuario")
                .HasForeignKey("TurnosLavadero.DataAccess.Entities.Usuario", "ClienteId")
                .OnDelete(DeleteBehavior.Restrict);

            b.Navigation("Cliente");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Recordatorio", b =>
        {
            b.HasOne("TurnosLavadero.DataAccess.Entities.Turno", "Turno")
                .WithMany("Recordatorios")
                .HasForeignKey("TurnoId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.Navigation("Turno");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Cliente", b =>
        {
            b.Navigation("Turnos");
            b.Navigation("Usuario");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Servicio", b =>
        {
            b.Navigation("Turnos");
        });

        modelBuilder.Entity("TurnosLavadero.DataAccess.Entities.Turno", b =>
        {
            b.Navigation("Recordatorios");
        });
#pragma warning restore 612, 618
    }
}
