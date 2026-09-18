using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.DTOs.Servicios;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.API.Controllers;

[ApiController]
[Authorize]
[Route("api/servicios")]
public sealed class ServiciosController(IServicioService servicioService) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetActive(CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await servicioService.GetActiveAsync(cancellationToken)));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await servicioService.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public Task<IActionResult> Create(
        ServicioCreateDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
    {
        var created = await servicioService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    });

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        ServicioUpdateDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
            Ok(await servicioService.UpdateAsync(id, dto, cancellationToken)));

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            await servicioService.DeleteAsync(id, cancellationToken);
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
        catch (ServicioNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (ServicioDuplicadoException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
        catch (ServicioConTurnosException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
