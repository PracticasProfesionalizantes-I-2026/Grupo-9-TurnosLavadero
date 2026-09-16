using TurnosLavadero.Shared.DTOs.Servicios;

namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface IServicioService
{
    Task<IReadOnlyCollection<ServicioResponseDTO>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ServicioResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServicioResponseDTO> CreateAsync(ServicioCreateDTO dto, CancellationToken cancellationToken = default);
    Task<ServicioResponseDTO> UpdateAsync(Guid id, ServicioUpdateDTO dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
