using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.DTOs.Clientes;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.API.Controllers;

[ApiController]
[Authorize]
[Route("api/clientes")]
public sealed class ClientesController(IClienteService clienteService) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await clienteService.GetAllAsync(cancellationToken)));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        ExecuteAsync(async () => Ok(await clienteService.GetByIdAsync(id, cancellationToken)));

    [HttpPost]
    public Task<IActionResult> Create(
        ClienteCreateDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
    {
        var created = await clienteService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    });

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(
        Guid id,
        ClienteUpdateDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
            Ok(await clienteService.UpdateAsync(id, dto, cancellationToken)));

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
        catch (ClienteDuplicadoException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
