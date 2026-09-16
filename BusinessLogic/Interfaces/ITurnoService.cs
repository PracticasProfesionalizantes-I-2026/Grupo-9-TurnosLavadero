using TurnosLavadero.Shared.DTOs.Turnos;

namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface ITurnoService
{
    Task<IReadOnlyCollection<TurnoResponseDTO>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TurnoResponseDTO>> GetAgendaAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(DateTimeOffset fechaHora, CancellationToken cancellationToken = default);
    Task<TurnoResponseDTO> CreateAsync(TurnoCreateDTO dto, CancellationToken cancellationToken = default);
    Task<TurnoResponseDTO> UpdateAsync(Guid id, TurnoUpdateDTO dto, CancellationToken cancellationToken = default);
    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
