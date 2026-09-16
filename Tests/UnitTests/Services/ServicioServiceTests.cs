using TurnosLavadero.BusinessLogic.Services;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Servicios;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;
using TurnosLavadero.UnitTests.TestDoubles;

namespace TurnosLavadero.UnitTests.Services;

public sealed class ServicioServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidData_NormalizesAndSavesServicio()
    {
        var repository = new Mock<IServicioRepository>();
        repository.Setup(x => x.ExistsByNormalizedNameAsync(
                "Lavado completo", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.CreateAsync(It.IsAny<Servicio>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Servicio servicio, CancellationToken _) =>
            {
                servicio.Id = Guid.NewGuid();
                return servicio;
            });
        var service = CreateService(repository, RolUsuario.Administrador);

        var result = await service.CreateAsync(new ServicioCreateDTO
        {
            Nombre = "  Lavado completo  ",
            Importe = 15000m
        });

        Assert.Equal("Lavado completo", result.Nombre);
        Assert.Equal(15000m, result.Importe);
        repository.Verify(x => x.CreateAsync(It.IsAny<Servicio>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsDuplicated_ThrowsServicioDuplicadoException()
    {
        var repository = new Mock<IServicioRepository>();
        repository.Setup(x => x.ExistsByNormalizedNameAsync(
                "Lavado", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(repository, RolUsuario.Administrador);

        await Assert.ThrowsAsync<ServicioDuplicadoException>(() => service.CreateAsync(
            new ServicioCreateDTO { Nombre = "Lavado", Importe = 1000m }));
    }

    [Fact]
    public async Task CreateAsync_WhenImporteIsNotPositive_ThrowsValidationException()
    {
        var service = CreateService(new Mock<IServicioRepository>(), RolUsuario.Administrador);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new ServicioCreateDTO { Nombre = "Lavado", Importe = 0m }));
    }

    [Fact]
    public async Task DeleteAsync_WhenServicioHasTurnos_ThrowsServicioConTurnosException()
    {
        var servicio = new Servicio { Id = Guid.NewGuid(), Nombre = "Lavado", Importe = 1000m };
        var repository = new Mock<IServicioRepository>();
        repository.Setup(x => x.GetByIdAsync(servicio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(servicio);
        repository.Setup(x => x.HasRelatedTurnosAsync(servicio.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(repository, RolUsuario.Administrador);

        await Assert.ThrowsAsync<ServicioConTurnosException>(() => service.DeleteAsync(servicio.Id));
    }

    [Fact]
    public async Task CreateAsync_WhenActorIsEmployee_ThrowsForbiddenException()
    {
        var service = CreateService(new Mock<IServicioRepository>(), RolUsuario.Empleado);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(
            new ServicioCreateDTO { Nombre = "Lavado", Importe = 1000m }));
    }

    private static ServicioService CreateService(Mock<IServicioRepository> repository, RolUsuario role) =>
        new(repository.Object, new ActorContextStub { Rol = role });
}
