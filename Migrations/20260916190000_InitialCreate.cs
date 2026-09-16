using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TurnosLavadero.DataAccess.Context;

#nullable disable

namespace TurnosLavadero.DataAccess.Migrations;

[DbContext(typeof(LavaderoDbContext))]
[Migration("20260916190000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Clientes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Apellido = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                EmailContacto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                Telefono = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                NotificacionesHabilitadas = table.Column<bool>(type: "INTEGER", nullable: false),
                Activo = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Clientes", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Servicios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false, collation: "NOCASE"),
                Importe = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                Activo = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Servicios", x => x.Id);
                table.CheckConstraint("CK_Servicios_Importe_Positivo", "Importe > 0");
            });

        migrationBuilder.CreateTable(
            name: "Usuarios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Rol = table.Column<int>(type: "INTEGER", nullable: false),
                ClienteId = table.Column<Guid>(type: "TEXT", nullable: true),
                Activo = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Usuarios", x => x.Id);
                table.ForeignKey(
                    name: "FK_Usuarios_Clientes_ClienteId",
                    column: x => x.ClienteId,
                    principalTable: "Clientes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Turnos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                ServicioId = table.Column<Guid>(type: "TEXT", nullable: false),
                FechaHora = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Estado = table.Column<int>(type: "INTEGER", nullable: false),
                FechaCreacion = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Turnos", x => x.Id);
                table.ForeignKey(
                    name: "FK_Turnos_Clientes_ClienteId",
                    column: x => x.ClienteId,
                    principalTable: "Clientes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Turnos_Servicios_ServicioId",
                    column: x => x.ServicioId,
                    principalTable: "Servicios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Recordatorios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                TurnoId = table.Column<Guid>(type: "TEXT", nullable: false),
                FechaProgramada = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                FechaProcesada = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                Estado = table.Column<int>(type: "INTEGER", nullable: false),
                Detalle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Recordatorios", x => x.Id);
                table.ForeignKey(
                    name: "FK_Recordatorios_Turnos_TurnoId",
                    column: x => x.TurnoId,
                    principalTable: "Turnos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Clientes_EmailContacto",
            table: "Clientes",
            column: "EmailContacto",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Servicios_Nombre",
            table: "Servicios",
            column: "Nombre",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Usuarios_ClienteId",
            table: "Usuarios",
            column: "ClienteId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Usuarios_Email",
            table: "Usuarios",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Turnos_ClienteId",
            table: "Turnos",
            column: "ClienteId");

        migrationBuilder.CreateIndex(
            name: "IX_Turnos_ServicioId",
            table: "Turnos",
            column: "ServicioId");

        migrationBuilder.CreateIndex(
            name: "IX_Turnos_FechaHora",
            table: "Turnos",
            column: "FechaHora",
            unique: true,
            filter: "Estado = 1");

        migrationBuilder.CreateIndex(
            name: "IX_Recordatorios_TurnoId_FechaProgramada",
            table: "Recordatorios",
            columns: new[] { "TurnoId", "FechaProgramada" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Recordatorios");
        migrationBuilder.DropTable(name: "Usuarios");
        migrationBuilder.DropTable(name: "Turnos");
        migrationBuilder.DropTable(name: "Clientes");
        migrationBuilder.DropTable(name: "Servicios");
    }
}
