using TurnosLavadero.BusinessLogic.Services;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Clientes;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;
using TurnosLavadero.UnitTests.TestDoubles;

namespace TurnosLavadero.UnitTests.Services;

public sealed class ClienteServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidData_NormalizesAndSavesCliente()
    {
        var repository = new Mock<IClienteRepository>();
        repository.Setup(x => x.ExistsByEmailAsync(
                "ana@example.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(x => x.CreateAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente cliente, CancellationToken _) =>
            {
                cliente.Id = Guid.NewGuid();
                return cliente;
            });
        var service = CreateService(repository, new ActorContextStub { Rol = RolUsuario.Empleado });

        var result = await service.CreateAsync(new ClienteCreateDTO
        {
            Nombre = " Ana ",
            Apellido = " Pérez ",
            EmailContacto = " ANA@EXAMPLE.TEST "
        });

        Assert.Equal("Ana", result.Nombre);
        Assert.Equal("ana@example.test", result.EmailContacto);
    }

    [Fact]
    public async Task CreateAsync_WhenEmailExists_ThrowsClienteDuplicadoException()
    {
        var repository = new Mock<IClienteRepository>();
        repository.Setup(x => x.ExistsByEmailAsync(
                "ana@example.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = CreateService(repository, new ActorContextStub { Rol = RolUsuario.Administrador });

        await Assert.ThrowsAsync<ClienteDuplicadoException>(() => service.CreateAsync(new ClienteCreateDTO
        {
            Nombre = "Ana",
            Apellido = "Pérez",
            EmailContacto = "ana@example.test"
        }));
    }

    [Fact]
    public async Task GetByIdAsync_WhenClientRequestsAnotherClient_ThrowsForbiddenException()
    {
        var service = CreateService(
            new Mock<IClienteRepository>(),
            new ActorContextStub { Rol = RolUsuario.Cliente, ClienteId = Guid.NewGuid() });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_WhenClientUpdatesSelf_DoesNotChangeActiveState()
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Nombre = "Ana",
            Apellido = "Pérez",
            EmailContacto = "ana@example.test",
            Activo = true
        };
        var repository = new Mock<IClienteRepository>();
        repository.Setup(x => x.GetByIdAsync(cliente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);
        repository.Setup(x => x.ExistsByEmailAsync(
                "ana@example.test", cliente.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = CreateService(
            repository,
            new ActorContextStub { Rol = RolUsuario.Cliente, ClienteId = cliente.Id });

        await service.UpdateAsync(cliente.Id, new ClienteUpdateDTO
        {
            Nombre = "Ana",
            Apellido = "Pérez",
            EmailContacto = "ana@example.test",
            Activo = false
        });

        Assert.True(cliente.Activo);
    }

    private static ClienteService CreateService(
        Mock<IClienteRepository> repository,
        ActorContextStub actor) => new(repository.Object, actor);
}
