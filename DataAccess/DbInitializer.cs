using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.Shared.Enums;

namespace TurnosLavadero.DataAccess;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        LavaderoDbContext context,
        CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        var clientes = await SeedClientesAsync(context, cancellationToken);
        var servicios = await SeedServiciosAsync(context, cancellationToken);
        await SeedTurnosAsync(context, clientes, servicios, cancellationToken);
        await SeedOptionalUsersAsync(context, clientes, cancellationToken);
    }

    private static async Task<IReadOnlyCollection<Cliente>> SeedClientesAsync(
        LavaderoDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Clientes.AnyAsync(cancellationToken))
        {
            context.Clientes.AddRange(
                new Cliente
                {
                    Id = Guid.NewGuid(),
                    Nombre = "Ana",
                    Apellido = "Pérez",
                    EmailContacto = "ana.perez@example.test",
                    Telefono = "+54 3493 000001",
                    NotificacionesHabilitadas = true,
                    Activo = true
                },
                new Cliente
                {
                    Id = Guid.NewGuid(),
                    Nombre = "Luis",
                    Apellido = "Gómez",
                    EmailContacto = "luis.gomez@example.test",
                    Telefono = "+54 3493 000002",
                    NotificacionesHabilitadas = false,
                    Activo = true
                });

            await context.SaveChangesAsync(cancellationToken);
        }

        return await context.Clientes.OrderBy(x => x.EmailContacto).ToListAsync(cancellationToken);
    }

    private static async Task<IReadOnlyCollection<Servicio>> SeedServiciosAsync(
        LavaderoDbContext context,
        CancellationToken cancellationToken)
    {
        if (!await context.Servicios.AnyAsync(cancellationToken))
        {
            context.Servicios.AddRange(
                new Servicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = "Lavado exterior",
                    Importe = 8000m,
                    Activo = true
                },
                new Servicio
                {
                    Id = Guid.NewGuid(),
                    Nombre = "Lavado completo",
                    Importe = 15000m,
                    Activo = true
                });

            await context.SaveChangesAsync(cancellationToken);
        }

        return await context.Servicios.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
    }

    private static async Task SeedTurnosAsync(
        LavaderoDbContext context,
        IReadOnlyCollection<Cliente> clientes,
        IReadOnlyCollection<Servicio> servicios,
        CancellationToken cancellationToken)
    {
        if (await context.Turnos.AnyAsync(cancellationToken))
        {
            return;
        }

        var fechaHora = DateTimeOffset.UtcNow.AddDays(1);
        fechaHora = new DateTimeOffset(
            fechaHora.Year,
            fechaHora.Month,
            fechaHora.Day,
            13,
            0,
            0,
            TimeSpan.Zero);

        context.Turnos.Add(new Turno
        {
            Id = Guid.NewGuid(),
            ClienteId = clientes.First().Id,
            ServicioId = servicios.First().Id,
            FechaHora = fechaHora,
            Estado = EstadoTurno.Confirmado,
            FechaCreacion = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedOptionalUsersAsync(
        LavaderoDbContext context,
        IReadOnlyCollection<Cliente> clientes,
        CancellationToken cancellationToken)
    {
        var passwordHasher = new PasswordHasher<Usuario>();
        await SeedUserFromEnvironmentAsync(
            context,
            passwordHasher,
            "TURNOSLAVADERO_SEED_ADMIN_EMAIL",
            "TURNOSLAVADERO_SEED_ADMIN_PASSWORD",
            RolUsuario.Administrador,
            null,
            cancellationToken);

        await SeedUserFromEnvironmentAsync(
            context,
            passwordHasher,
            "TURNOSLAVADERO_SEED_EMPLEADO_EMAIL",
            "TURNOSLAVADERO_SEED_EMPLEADO_PASSWORD",
            RolUsuario.Empleado,
            null,
            cancellationToken);

        await SeedUserFromEnvironmentAsync(
            context,
            passwordHasher,
            "TURNOSLAVADERO_SEED_CLIENTE_EMAIL",
            "TURNOSLAVADERO_SEED_CLIENTE_PASSWORD",
            RolUsuario.Cliente,
            clientes.First().Id,
            cancellationToken);
    }

    private static async Task SeedUserFromEnvironmentAsync(
        LavaderoDbContext context,
        IPasswordHasher<Usuario> passwordHasher,
        string emailVariable,
        string passwordVariable,
        RolUsuario role,
        Guid? clienteId,
        CancellationToken cancellationToken)
    {
        var email = Environment.GetEnvironmentVariable(emailVariable)?.Trim().ToLowerInvariant();
        var password = Environment.GetEnvironmentVariable(passwordVariable);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await context.Usuarios.AnyAsync(x => x.Email == email, cancellationToken))
        {
            return;
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = email,
            Rol = role,
            ClienteId = clienteId,
            Activo = true
        };

        usuario.PasswordHash = passwordHasher.HashPassword(usuario, password);
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(cancellationToken);
    }
}
