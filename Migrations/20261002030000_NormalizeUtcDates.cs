using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TurnosLavadero.DataAccess.Context;

namespace TurnosLavadero.DataAccess.Migrations;

[DbContext(typeof(LavaderoDbContext))]
[Migration("20261002030000_NormalizeUtcDates")]
public sealed class NormalizeUtcDates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Las fechas existentes de la API están normalizadas a UTC (+00:00).
        // Quitar el sufijo conserva fracciones y unifica la representación DateTime.
        foreach (var (table, column) in new[] {
            ("Turnos", "FechaHora"), ("Turnos", "FechaCreacion"),
            ("Recordatorios", "FechaProgramada"), ("Recordatorios", "FechaProcesada") })
        {
            migrationBuilder.Sql($"UPDATE {table} SET {column} = substr({column}, 1, length({column}) - 6) WHERE {column} LIKE '%+00:00';");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var (table, column) in new[] {
            ("Turnos", "FechaHora"), ("Turnos", "FechaCreacion"),
            ("Recordatorios", "FechaProgramada"), ("Recordatorios", "FechaProcesada") })
        {
            migrationBuilder.Sql($"UPDATE {table} SET {column} = {column} || '+00:00' WHERE {column} IS NOT NULL AND {column} NOT LIKE '%+00:00';");
        }
    }
}
