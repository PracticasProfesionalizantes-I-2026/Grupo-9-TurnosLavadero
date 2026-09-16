using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.BusinessLogic.Services;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;
using TurnosLavadero.UnitTests.TestDoubles;

namespace TurnosLavadero.UnitTests.Services;

public sealed class RecordatorioServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_WithValidTurno_SendsAndRegistersReminder()
    {
        var turno = CreateTurno();
        var turnoRepository = new Mock<ITurnoRepository>();
        var recordatorioRepository = new Mock<IRecordatorioRepository>();
        var sender = new Mock<INotificationSender>();
        turnoRepository.Setup(x => x.GetUpcomingAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([turno]);
        recordatorioRepository.Setup(x => x.ExistsProcessedForTurnoAsync(
                turno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        recordatorioRepository.Setup(x => x.CreateAsync(
                It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recordatorio reminder, CancellationToken _) =>
            {
                reminder.Id = Guid.NewGuid();
                return reminder;
            });
        var service = CreateService(turnoRepository, recordatorioRepository, sender);

        var result = await service.ProcessAsync(Now, Now.AddDays(2));

        Assert.Equal(1, result.Enviados);
        Assert.Equal(0, result.Fallidos);
        sender.Verify(x => x.SendTurnoReminderAsync(turno, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenNotificationsDisabled_RegistersOmittedReminder()
    {
        var turno = CreateTurno();
        turno.Cliente.NotificacionesHabilitadas = false;
        var turnoRepository = new Mock<ITurnoRepository>();
        var recordatorioRepository = new Mock<IRecordatorioRepository>();
        turnoRepository.Setup(x => x.GetUpcomingAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([turno]);
        recordatorioRepository.Setup(x => x.ExistsProcessedForTurnoAsync(
                turno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        recordatorioRepository.Setup(x => x.CreateAsync(
                It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recordatorio reminder, CancellationToken _) => reminder);
        var sender = new Mock<INotificationSender>();
        var service = CreateService(turnoRepository, recordatorioRepository, sender);

        var result = await service.ProcessAsync(Now, Now.AddDays(2));

        Assert.Equal(1, result.Omitidos);
        sender.Verify(x => x.SendTurnoReminderAsync(
            It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenSenderFails_RegistersFailedReminder()
    {
        var turno = CreateTurno();
        var turnoRepository = new Mock<ITurnoRepository>();
        var recordatorioRepository = new Mock<IRecordatorioRepository>();
        var sender = new Mock<INotificationSender>();
        turnoRepository.Setup(x => x.GetUpcomingAsync(
                It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([turno]);
        recordatorioRepository.Setup(x => x.ExistsProcessedForTurnoAsync(
                turno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        sender.Setup(x => x.SendTurnoReminderAsync(turno, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Proveedor no disponible"));
        recordatorioRepository.Setup(x => x.CreateAsync(
                It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recordatorio reminder, CancellationToken _) => reminder);
        var service = CreateService(turnoRepository, recordatorioRepository, sender);

        var result = await service.ProcessAsync(Now, Now.AddDays(2));

        Assert.Equal(1, result.Fallidos);
        Assert.Contains("Proveedor no disponible", result.Resultados.Single().Detalle);
    }

    [Fact]
    public async Task ProcessAsync_WhenActorIsNotAdmin_ThrowsForbiddenException()
    {
        var service = new RecordatorioService(
            Mock.Of<ITurnoRepository>(),
            Mock.Of<IRecordatorioRepository>(),
            Mock.Of<INotificationSender>(),
            new ActorContextStub { Rol = RolUsuario.Empleado },
            new FixedClock(Now));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ProcessAsync(Now, Now.AddDays(1)));
    }

    private static RecordatorioService CreateService(
        Mock<ITurnoRepository> turnoRepository,
        Mock<IRecordatorioRepository> recordatorioRepository,
        Mock<INotificationSender> sender) => new(
            turnoRepository.Object,
            recordatorioRepository.Object,
            sender.Object,
            new ActorContextStub { Rol = RolUsuario.Administrador },
            new FixedClock(Now));

    private static Turno CreateTurno()
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Nombre = "Ana",
            Apellido = "Pérez",
            EmailContacto = "ana@example.test",
            NotificacionesHabilitadas = true,
            Activo = true
        };
        var servicio = new Servicio
        {
            Id = Guid.NewGuid(),
            Nombre = "Lavado completo",
            Importe = 15000m,
            Activo = true
        };
        return new Turno
        {
            Id = Guid.NewGuid(),
            ClienteId = cliente.Id,
            Cliente = cliente,
            ServicioId = servicio.Id,
            Servicio = servicio,
            FechaHora = Now.AddDays(1),
            FechaCreacion = Now,
            Estado = EstadoTurno.Confirmado
        };
    }
}
