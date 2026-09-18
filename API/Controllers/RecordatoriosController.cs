using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.API.Controllers;

[ApiController]
[Authorize]
[Route("api/recordatorios")]
public sealed class RecordatoriosController(
    IRecordatorioService recordatorioService) : ControllerBase
{
    [HttpPost("procesar")]
    public Task<IActionResult> Process(
        [FromQuery] DateTimeOffset desde,
        [FromQuery] DateTimeOffset hasta,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
            Ok(await recordatorioService.ProcessAsync(desde, hasta, cancellationToken)));

    private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ValidationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ForbiddenException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status403Forbidden);
        }
    }
}
