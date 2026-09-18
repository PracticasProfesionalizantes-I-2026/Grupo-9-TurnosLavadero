using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.DTOs.Turnos;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.API.Controllers;

[ApiController]
[Authorize]
[Route("api/turnos")]
public sealed class TurnosController(ITurnoService turnoService) : ControllerBase
{
    [HttpGet("mis-turnos")]
    public Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await turnoService.GetMineAsync(cancellationToken)));

    [HttpGet("agenda")]
    public Task<IActionResult> GetAgenda(
        [FromQuery] DateOnly fecha,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await turnoService.GetAgendaAsync(fecha, cancellationToken)));

    [HttpGet("disponibilidad")]
    public Task<IActionResult> IsAvailable(
        [FromQuery] DateTimeOffset fechaHora,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
            Ok(new { disponible = await turnoService.IsAvailableAsync(fechaHora, cancellationToken) }));

    [HttpPost]
    public Task<IActionResult> Create(
        TurnoCreateDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
    {
        var created = await turnoService.CreateAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    });

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        TurnoUpdateDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
            Ok(await turnoService.UpdateAsync(id, dto, cancellationToken)));

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await turnoService.CancelAsync(id, cancellationToken);
            return NoContent();
        });

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
        catch (ClienteNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (ServicioNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (TurnoNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (HorarioNoDisponibleException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (TurnoNoModificableException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (TurnoNoCancelableException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
