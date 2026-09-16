using TurnosLavadero.Shared.DTOs.Clientes;

namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface IClienteService
{
    Task<IReadOnlyCollection<ClienteResponseDTO>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ClienteResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClienteResponseDTO> CreateAsync(ClienteCreateDTO dto, CancellationToken cancellationToken = default);
    Task<ClienteResponseDTO> UpdateAsync(Guid id, ClienteUpdateDTO dto, CancellationToken cancellationToken = default);
}
