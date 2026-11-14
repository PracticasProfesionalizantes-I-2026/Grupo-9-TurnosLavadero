using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.DTOs.Turnos;
using TurnosLavadero.Shared.Exceptions;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Validation;

namespace TurnosLavadero.API.Controllers;

[ApiController]
[Authorize]
[Route("api/turnos")]
public sealed class TurnosController(ITurnoService turnoService, IActorContext actorContext) : ControllerBase
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
        [FromQuery] string fechaHora,
        [FromQuery] Guid servicioId,
        [FromQuery] Guid? excludeId,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
        {
            if (!ExplicitOffsetDateTime.TryParse(fechaHora, out var fecha))
                return Problem(ExplicitOffsetDateTime.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);

            return Ok(new { disponible = await turnoService.IsAvailableAsync(fecha, servicioId, excludeId, cancellationToken) });
        });

    [HttpPost]
    public Task<IActionResult> Create(
        TurnoCreateDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
    {
        // Rechazo explícito antes de invocar negocio. El service conserva la misma
        // defensa para llamadas que no ingresen por este controller.
        if (actorContext.Rol == RolUsuario.Cliente
            && (!actorContext.ClienteId.HasValue
                || dto.ClienteId.HasValue && dto.ClienteId != actorContext.ClienteId))
            return Problem("El cliente solo puede crear turnos propios.", statusCode: StatusCodes.Status403Forbidden);

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
