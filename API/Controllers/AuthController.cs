using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.DTOs.Auth;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("registro")]
    public Task<IActionResult> Register(
        RegisterClienteDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
    {
        var result = await authService.RegisterClienteAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    });

    [AllowAnonymous]
    [HttpPost("login")]
    public Task<IActionResult> Login(
        LoginDTO dto,
        CancellationToken cancellationToken) => ExecuteAsync(async () =>
    {
        var result = await authService.LoginAsync(dto, cancellationToken);
        return Ok(result);
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
        catch (CredencialesInvalidasException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (ClienteDuplicadoException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
