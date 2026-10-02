using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.Shared.DTOs.Recordatorios;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.BusinessLogic.Services;

public sealed class RecordatorioService(
    IRecordatorioProcessor processor,
    IActorContext actorContext) : IRecordatorioService
{
    public Task<ProcesamientoRecordatoriosResponseDTO> ProcessAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        if (!actorContext.IsAuthenticated || actorContext.Rol != RolUsuario.Administrador)
            throw new ForbiddenException("El procesamiento manual requiere un Administrador.");
        return processor.ProcessAsync(from, to, cancellationToken);
    }
}
