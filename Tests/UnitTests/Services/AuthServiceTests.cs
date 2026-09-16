using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.BusinessLogic.Services;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Auth;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.UnitTests.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterClienteAsync_WithValidData_CreatesClientUserAndToken()
    {
        var clienteRepository = new Mock<IClienteRepository>();
        var usuarioRepository = new Mock<IUsuarioRepository>();
        var passwordService = new Mock<IPasswordService>();
        var tokenService = new Mock<ITokenService>();
        clienteRepository.Setup(x => x.ExistsByEmailAsync(
                "ana@example.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        usuarioRepository.Setup(x => x.GetByEmailAsync("ana@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
        passwordService.Setup(x => x.Hash("Password123!")).Returns("secure-hash");
        usuarioRepository.Setup(x => x.CreateClienteUserAsync(
                It.IsAny<Cliente>(), It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente cliente, Usuario usuario, CancellationToken _) =>
            {
                cliente.Id = Guid.NewGuid();
                usuario.Id = Guid.NewGuid();
                usuario.ClienteId = cliente.Id;
                return usuario;
            });
        tokenService.Setup(x => x.CreateToken(It.IsAny<Usuario>()))
            .Returns(new TokenResult("token", DateTimeOffset.UtcNow.AddHours(1)));
        var service = new AuthService(
            clienteRepository.Object,
            usuarioRepository.Object,
            passwordService.Object,
            tokenService.Object);

        var result = await service.RegisterClienteAsync(new RegisterClienteDTO
        {
            Nombre = "Ana",
            Apellido = "Pérez",
            Email = " ANA@EXAMPLE.TEST ",
            Password = "Password123!"
        });

        Assert.Equal("token", result.Token);
        Assert.Equal("ana@example.test", result.Email);
        Assert.Equal(RolUsuario.Cliente, result.Rol);
        Assert.NotNull(result.ClienteId);
    }

    [Fact]
    public async Task RegisterClienteAsync_WhenEmailExists_ThrowsClienteDuplicadoException()
    {
        var clienteRepository = new Mock<IClienteRepository>();
        clienteRepository.Setup(x => x.ExistsByEmailAsync(
                "ana@example.test", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new AuthService(
            clienteRepository.Object,
            Mock.Of<IUsuarioRepository>(),
            Mock.Of<IPasswordService>(),
            Mock.Of<ITokenService>());

        await Assert.ThrowsAsync<ClienteDuplicadoException>(() => service.RegisterClienteAsync(
            new RegisterClienteDTO
            {
                Nombre = "Ana",
                Apellido = "Pérez",
                Email = "ana@example.test",
                Password = "Password123!"
            }));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.test",
            PasswordHash = "hash",
            Rol = RolUsuario.Administrador,
            Activo = true
        };
        var usuarioRepository = new Mock<IUsuarioRepository>();
        var passwordService = new Mock<IPasswordService>();
        var tokenService = new Mock<ITokenService>();
        usuarioRepository.Setup(x => x.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        passwordService.Setup(x => x.Verify("hash", "Password123!")).Returns(true);
        tokenService.Setup(x => x.CreateToken(usuario))
            .Returns(new TokenResult("jwt", DateTimeOffset.UtcNow.AddHours(1)));
        var service = new AuthService(
            Mock.Of<IClienteRepository>(),
            usuarioRepository.Object,
            passwordService.Object,
            tokenService.Object);

        var result = await service.LoginAsync(new LoginDTO
        {
            Email = usuario.Email,
            Password = "Password123!"
        });

        Assert.Equal("jwt", result.Token);
        Assert.Equal(RolUsuario.Administrador, result.Rol);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsCredencialesInvalidasException()
    {
        var usuario = new Usuario
        {
            Email = "admin@example.test",
            PasswordHash = "hash",
            Rol = RolUsuario.Administrador,
            Activo = true
        };
        var usuarioRepository = new Mock<IUsuarioRepository>();
        var passwordService = new Mock<IPasswordService>();
        usuarioRepository.Setup(x => x.GetByEmailAsync(usuario.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        passwordService.Setup(x => x.Verify("hash", "wrong")).Returns(false);
        var service = new AuthService(
            Mock.Of<IClienteRepository>(),
            usuarioRepository.Object,
            passwordService.Object,
            Mock.Of<ITokenService>());

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => service.LoginAsync(
            new LoginDTO { Email = usuario.Email, Password = "wrong" }));
    }
}
