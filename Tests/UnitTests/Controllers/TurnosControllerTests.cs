using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TurnosLavadero.API.Controllers;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.DTOs.Turnos;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.UnitTests.TestDoubles;

namespace TurnosLavadero.UnitTests.Controllers;

public sealed class TurnosControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClienteAjenoOClaimAusente_Devuelve403SinInvocarService(bool missingClaim)
    {
        var service = new Mock<ITurnoService>(MockBehavior.Strict);
        var controller = new TurnosController(service.Object, new ActorContextStub {
            Rol = RolUsuario.Cliente, ClienteId = missingClaim ? null : Guid.NewGuid()
        }) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var response = await controller.Create(new TurnoCreateDTO {
            ClienteId = Guid.NewGuid(), ServicioId = Guid.NewGuid(), FechaHora = DateTimeOffset.UtcNow.AddDays(1)
        }, CancellationToken.None);

        Assert.Equal(403, Assert.IsType<ObjectResult>(response).StatusCode);
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClientePropioOInferido_DerivaAlServiceYDevuelve201(bool explicitId)
    {
        var clienteId = Guid.NewGuid();
        var dto = new TurnoCreateDTO { ClienteId = explicitId ? clienteId : null };
        var service = new Mock<ITurnoService>(MockBehavior.Strict);
        service.Setup(x => x.CreateAsync(dto, CancellationToken.None)).ReturnsAsync(new TurnoResponseDTO());
        var controller = new TurnosController(service.Object, new ActorContextStub {
            Rol = RolUsuario.Cliente, ClienteId = clienteId
        });
        Assert.Equal(201, Assert.IsType<ObjectResult>(await controller.Create(dto, CancellationToken.None)).StatusCode);
        service.Verify(x => x.CreateAsync(dto, CancellationToken.None), Times.Once);
    }
}
