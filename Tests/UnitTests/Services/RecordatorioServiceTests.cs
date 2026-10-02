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
            Mock.Of<IRecordatorioProcessor>(),
            new ActorContextStub { Rol = RolUsuario.Empleado });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.ProcessAsync(Now, Now.AddDays(1)));
    }

    [Theory]
    [InlineData("cancelado", 1, 0)]
    [InlineData("sin-contacto", 0, 1)]
    [InlineData("contacto-invalido", 0, 1)]
    [InlineData("procesado", 0, 0)]
    public async Task ProcessAsync_WithExcludedTurno_DoesNotSend(string scenario, int omitted, int failed)
    {
        var turno = CreateTurno();
        if (scenario == "cancelado") turno.Estado = EstadoTurno.Cancelado;
        if (scenario == "sin-contacto") turno.Cliente.EmailContacto = "";
        if (scenario == "contacto-invalido") turno.Cliente.EmailContacto = "correo-invalido";
        var turnos = new Mock<ITurnoRepository>();
        var reminders = new Mock<IRecordatorioRepository>();
        var sender = new Mock<INotificationSender>();
        turnos.Setup(x => x.GetUpcomingAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([turno]);
        reminders.Setup(x => x.ExistsProcessedForTurnoAsync(turno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario == "procesado");
        reminders.Setup(x => x.CreateAsync(It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Recordatorio reminder, CancellationToken _) => reminder);
        var result = await CreateService(turnos, reminders, sender).ProcessAsync(Now, Now.AddDays(2));
        Assert.Equal(omitted, result.Omitidos);
        Assert.Equal(failed, result.Fallidos);
        sender.Verify(x => x.SendTurnoReminderAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Never);
        reminders.Verify(x => x.CreateAsync(It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()),
            scenario == "procesado" ? Times.Never() : Times.Once());
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidRange_ThrowsWithoutQuerying()
    {
        var turnos = new Mock<ITurnoRepository>();
        var processor = CreateService(turnos, new Mock<IRecordatorioRepository>(), new Mock<INotificationSender>());
        await Assert.ThrowsAsync<ValidationException>(() => processor.ProcessAsync(Now, Now));
        turnos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProcessAsync_WhenCanceled_DoesNotRegisterFailedDelivery()
    {
        using var cancellation = new CancellationTokenSource();
        var turno = CreateTurno();
        var turnos = new Mock<ITurnoRepository>();
        var reminders = new Mock<IRecordatorioRepository>();
        var sender = new Mock<INotificationSender>();
        turnos.Setup(x => x.GetUpcomingAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([turno]);
        sender.Setup(x => x.SendTurnoReminderAsync(turno, It.IsAny<CancellationToken>()))
            .Callback(() => cancellation.Cancel())
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(turnos, reminders, sender).ProcessAsync(Now, Now.AddDays(2), cancellation.Token));
        reminders.Verify(x => x.CreateAsync(It.IsAny<Recordatorio>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RecordatorioProcessor CreateService(
        Mock<ITurnoRepository> turnoRepository,
        Mock<IRecordatorioRepository> recordatorioRepository,
        Mock<INotificationSender> sender) => new(
            turnoRepository.Object,
            recordatorioRepository.Object,
            sender.Object,
            new FixedClock(Now),
            new RecordatorioProcessingLock());

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
