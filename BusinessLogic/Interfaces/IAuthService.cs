using TurnosLavadero.Shared.DTOs.Auth;

namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterClienteAsync(RegisterClienteDTO dto, CancellationToken cancellationToken = default);
    Task<AuthResponseDTO> LoginAsync(LoginDTO dto, CancellationToken cancellationToken = default);
}
