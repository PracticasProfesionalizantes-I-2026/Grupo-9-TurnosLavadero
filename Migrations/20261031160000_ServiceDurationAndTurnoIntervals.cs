using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TurnosLavadero.DataAccess.Context;

namespace TurnosLavadero.DataAccess.Migrations;

[DbContext(typeof(LavaderoDbContext))]
[Migration("20261031160000_ServiceDurationAndTurnoIntervals")]
public sealed class ServiceDurationAndTurnoIntervals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Valor de transición para datos existentes; el administrador debe revisar su catálogo.
        migrationBuilder.AddColumn<int>(name: "DuracionMinutos", table: "Servicios", type: "INTEGER", nullable: false, defaultValue: 30);
        migrationBuilder.AddColumn<int>(name: "DuracionMinutos", table: "Turnos", type: "INTEGER", nullable: false, defaultValue: 30);
        migrationBuilder.AddColumn<DateTime>(name: "FechaFin", table: "Turnos", type: "TEXT", nullable: false, defaultValue: DateTime.MinValue);
        // Conserva la fracción de segundo guardada por EF Core.
        migrationBuilder.Sql("UPDATE Turnos SET FechaFin = strftime('%Y-%m-%d %H:%M:%S', FechaHora, '+30 minutes') || substr(FechaHora, 20);");
        // Una base antigua puede contener intervalos superpuestos. No cancelar ni borrar reservas automáticamente.
        migrationBuilder.Sql("""
            CREATE TEMP TABLE VerificacionIntervalos (Valor INTEGER);
            CREATE TEMP TRIGGER VerificarIntervalos BEFORE INSERT ON VerificacionIntervalos
            WHEN EXISTS (SELECT 1 FROM Turnos a JOIN Turnos b ON a.Id < b.Id
                WHERE a.Estado = 1 AND b.Estado = 1 AND a.FechaHora < b.FechaFin AND a.FechaFin > b.FechaHora)
            BEGIN SELECT RAISE(ABORT, 'REVISAR_TURNOS_EXISTENTES: hay reservas superpuestas al asignar 30 minutos'); END;
            INSERT INTO VerificacionIntervalos VALUES (1);
            DROP TRIGGER VerificarIntervalos;
            DROP TABLE VerificacionIntervalos;
            """);
        foreach (var operation in new[] { "INSERT", "UPDATE" })
            migrationBuilder.Sql($"""
                CREATE TRIGGER TR_Turnos_NoOverlap_{(operation == "INSERT" ? "Insert" : "Update")}
                BEFORE {operation} ON Turnos
                WHEN NEW.Estado = 1 AND EXISTS (
                    SELECT 1 FROM Turnos t WHERE t.Estado = 1 AND t.Id <> NEW.Id
                    AND t.FechaHora < NEW.FechaFin AND t.FechaFin > NEW.FechaHora)
                BEGIN SELECT RAISE(ABORT, 'TURNOS_SOLAPADOS'); END;
                """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_Turnos_ValidInterval_Insert BEFORE INSERT ON Turnos
            WHEN NEW.DuracionMinutos < 1 OR NEW.DuracionMinutos > 480 OR NEW.FechaFin <= NEW.FechaHora
            BEGIN SELECT RAISE(ABORT, 'INTERVALO_INVALIDO'); END;
            CREATE TRIGGER TR_Turnos_ValidInterval_Update BEFORE UPDATE ON Turnos
            WHEN NEW.DuracionMinutos < 1 OR NEW.DuracionMinutos > 480 OR NEW.FechaFin <= NEW.FechaHora
            BEGIN SELECT RAISE(ABORT, 'INTERVALO_INVALIDO'); END;
            CREATE TRIGGER TR_Servicios_Duration_Insert BEFORE INSERT ON Servicios
            WHEN NEW.DuracionMinutos < 1 OR NEW.DuracionMinutos > 480
            BEGIN SELECT RAISE(ABORT, 'DURACION_INVALIDA'); END;
            CREATE TRIGGER TR_Servicios_Duration_Update BEFORE UPDATE ON Servicios
            WHEN NEW.DuracionMinutos < 1 OR NEW.DuracionMinutos > 480
            BEGIN SELECT RAISE(ABORT, 'DURACION_INVALIDA'); END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var trigger in new[] { "TR_Turnos_NoOverlap_Insert", "TR_Turnos_NoOverlap_Update", "TR_Turnos_ValidInterval_Insert", "TR_Turnos_ValidInterval_Update", "TR_Servicios_Duration_Insert", "TR_Servicios_Duration_Update" })
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {trigger};");
        migrationBuilder.DropColumn(name: "FechaFin", table: "Turnos");
        migrationBuilder.DropColumn(name: "DuracionMinutos", table: "Turnos");
        migrationBuilder.DropColumn(name: "DuracionMinutos", table: "Servicios");
    }
}
