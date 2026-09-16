using TurnosLavadero.BusinessLogic.Services;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Turnos;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;
using TurnosLavadero.UnitTests.TestDoubles;

namespace TurnosLavadero.UnitTests.Services;

public sealed class TurnoServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WithValidClientData_SavesAndReturnsTurno()
    {
        var cliente = CreateCliente();
        var servicio = CreateServicio();
        var turnoRepository = new Mock<ITurnoRepository>();
        var clienteRepository = new Mock<IClienteRepository>();
        var servicioRepository = new Mock<IServicioRepository>();
        clienteRepository.Setup(x => x.GetByIdAsync(cliente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);
        servicioRepository.Setup(x => x.GetByIdAsync(servicio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servicio);
        turnoRepository.Setup(x => x.ExistsActiveAtAsync(
                It.IsAny<DateTimeOffset>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        turnoRepository.Setup(x => x.CreateAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Turno turno, CancellationToken _) =>
            {
                turno.Id = Guid.NewGuid();
                return turno;
            });

        var service = CreateService(
            turnoRepository,
            clienteRepository,
            servicioRepository,
            new ActorContextStub { Rol = RolUsuario.Cliente, ClienteId = cliente.Id });

        var result = await service.CreateAsync(new TurnoCreateDTO
        {
            ServicioId = servicio.Id,
            FechaHora = Now.AddDays(1)
        });

        Assert.Equal(cliente.Id, result.ClienteId);
        Assert.Equal(servicio.Id, result.ServicioId);
        Assert.Equal(EstadoTurno.Confirmado, result.Estado);
        turnoRepository.Verify(x => x.CreateAsync(It.IsAny<Turno>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenClientRequestsAnotherClient_ThrowsForbiddenException()
    {
        var ownId = Guid.NewGuid();
        var service = CreateService(
            new Mock<ITurnoRepository>(),
            new Mock<IClienteRepository>(),
            new Mock<IServicioRepository>(),
            new ActorContextStub { Rol = RolUsuario.Cliente, ClienteId = ownId });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(new TurnoCreateDTO
        {
            ClienteId = Guid.NewGuid(),
            ServicioId = Guid.NewGuid(),
            FechaHora = Now.AddDays(1)
        }));
    }

    [Fact]
    public async Task CreateAsync_WhenScheduleOverlaps_ThrowsHorarioNoDisponibleException()
    {
        var cliente = CreateCliente();
        var servicio = CreateServicio();
        var turnoRepository = new Mock<ITurnoRepository>();
        var clienteRepository = new Mock<IClienteRepository>();
        var servicioRepository = new Mock<IServicioRepository>();
        clienteRepository.Setup(x => x.GetByIdAsync(cliente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);
        servicioRepository.Setup(x => x.GetByIdAsync(servicio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servicio);
        turnoRepository.Setup(x => x.ExistsActiveAtAsync(
                It.IsAny<DateTimeOffset>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService(
            turnoRepository,
            clienteRepository,
            servicioRepository,
            new ActorContextStub { Rol = RolUsuario.Empleado });

        await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => service.CreateAsync(new TurnoCreateDTO
        {
            ClienteId = cliente.Id,
            ServicioId = servicio.Id,
            FechaHora = Now.AddDays(1)
        }));
    }

    [Fact]
    public async Task CreateAsync_WhenDateIsPast_ThrowsValidationException()
    {
        var cliente = CreateCliente();
        var servicio = CreateServicio();
        var clienteRepository = new Mock<IClienteRepository>();
        var servicioRepository = new Mock<IServicioRepository>();
        clienteRepository.Setup(x => x.GetByIdAsync(cliente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);
        servicioRepository.Setup(x => x.GetByIdAsync(servicio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servicio);
        var service = CreateService(
            new Mock<ITurnoRepository>(),
            clienteRepository,
            servicioRepository,
            new ActorContextStub { Rol = RolUsuario.Empleado });

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new TurnoCreateDTO
        {
            ClienteId = cliente.Id,
            ServicioId = servicio.Id,
            FechaHora = Now.AddMinutes(-1)
        }));
    }

    [Fact]
    public async Task UpdateAsync_WhenTurnoBelongsToAnotherClient_ThrowsForbiddenException()
    {
        var turno = CreateTurno();
        var turnoRepository = new Mock<ITurnoRepository>();
        turnoRepository.Setup(x => x.GetByIdAsync(turno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(turno);
        var service = CreateService(
            turnoRepository,
            new Mock<IClienteRepository>(),
            new Mock<IServicioRepository>(),
            new ActorContextStub { Rol = RolUsuario.Cliente, ClienteId = Guid.NewGuid() });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(turno.Id, new TurnoUpdateDTO
        {
            ServicioId = turno.ServicioId,
            FechaHora = Now.AddDays(2)
        }));
    }

    [Fact]
    public async Task UpdateAsync_WhenTurnoIsCanceled_ThrowsTurnoNoModificableException()
    {
        var turno = CreateTurno();
        turno.Estado = EstadoTurno.Cancelado;
        var turnoRepository = new Mock<ITurnoRepository>();
        turnoRepository.Setup(x => x.GetByIdAsync(turno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(turno);
        var service = CreateService(
            turnoRepository,
            new Mock<IClienteRepository>(),
            new Mock<IServicioRepository>(),
            new ActorContextStub { Rol = RolUsuario.Administrador });

        await Assert.ThrowsAsync<TurnoNoModificableException>(() => service.UpdateAsync(
            turno.Id,
            new TurnoUpdateDTO { ServicioId = turno.ServicioId, FechaHora = Now.AddDays(2) }));
    }

    [Fact]
    public async Task CancelAsync_WithValidTurno_ChangesStateToCanceled()
    {
        var turno = CreateTurno();
        var turnoRepository = new Mock<ITurnoRepository>();
        turnoRepository.Setup(x => x.GetByIdAsync(turno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(turno);
        var service = CreateService(
            turnoRepository,
            new Mock<IClienteRepository>(),
            new Mock<IServicioRepository>(),
            new ActorContextStub { Rol = RolUsuario.Empleado });

        await service.CancelAsync(turno.Id);

        Assert.Equal(EstadoTurno.Cancelado, turno.Estado);
        turnoRepository.Verify(x => x.UpdateAsync(turno, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_WhenTurnoIsExpired_ThrowsTurnoNoCancelableException()
    {
        var turno = CreateTurno();
        turno.FechaHora = Now.AddMinutes(-1);
        var turnoRepository = new Mock<ITurnoRepository>();
        turnoRepository.Setup(x => x.GetByIdAsync(turno.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(turno);
        var service = CreateService(
            turnoRepository,
            new Mock<IClienteRepository>(),
            new Mock<IServicioRepository>(),
            new ActorContextStub { Rol = RolUsuario.Administrador });

        await Assert.ThrowsAsync<TurnoNoCancelableException>(() => service.CancelAsync(turno.Id));
    }

    private static TurnoService CreateService(
        Mock<ITurnoRepository> turnoRepository,
        Mock<IClienteRepository> clienteRepository,
        Mock<IServicioRepository> servicioRepository,
        ActorContextStub actor) => new(
            turnoRepository.Object,
            clienteRepository.Object,
            servicioRepository.Object,
            actor,
            new FixedClock(Now));

    private static Cliente CreateCliente() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Ana",
        Apellido = "Pérez",
        EmailContacto = "ana@example.test",
        Activo = true
    };

    private static Servicio CreateServicio() => new()
    {
        Id = Guid.NewGuid(),
        Nombre = "Lavado completo",
        Importe = 15000m,
        Activo = true
    };

    private static Turno CreateTurno()
    {
        var cliente = CreateCliente();
        var servicio = CreateServicio();
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
