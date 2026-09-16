using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Turnos;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.BusinessLogic.Services;

public sealed class TurnoService(
    ITurnoRepository turnoRepository,
    IClienteRepository clienteRepository,
    IServicioRepository servicioRepository,
    IActorContext actorContext,
    IClock clock) : ITurnoService
{
    public async Task<IReadOnlyCollection<TurnoResponseDTO>> GetMineAsync(
        CancellationToken cancellationToken = default)
    {
        if (!actorContext.IsAuthenticated
            || actorContext.Rol != RolUsuario.Cliente
            || !actorContext.ClienteId.HasValue)
        {
            throw new ForbiddenException("La consulta requiere un cliente autenticado.");
        }

        var turnos = await turnoRepository.GetByClienteIdAsync(
            actorContext.ClienteId.Value,
            cancellationToken);
        return turnos.Select(MapToResponseDTO).ToArray();
    }

    public async Task<IReadOnlyCollection<TurnoResponseDTO>> GetAgendaAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var turnos = await turnoRepository.GetByDateAsync(date, cancellationToken);
        return turnos.Select(MapToResponseDTO).ToArray();
    }

    public async Task<bool> IsAvailableAsync(
        DateTimeOffset fechaHora,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var utcDate = ValidateFuture(fechaHora);
        return !await turnoRepository.ExistsActiveAtAsync(
            utcDate,
            cancellationToken: cancellationToken);
    }

    public async Task<TurnoResponseDTO> CreateAsync(
        TurnoCreateDTO dto,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var clienteId = ResolveClienteId(dto.ClienteId);
        var cliente = await GetActiveClienteAsync(clienteId, cancellationToken);
        var servicio = await GetActiveServicioAsync(dto.ServicioId, cancellationToken);
        var fechaHora = ValidateFuture(dto.FechaHora);

        if (await turnoRepository.ExistsActiveAtAsync(
                fechaHora,
                cancellationToken: cancellationToken))
        {
            throw new HorarioNoDisponibleException(
                "El horario seleccionado ya no se encuentra disponible.");
        }

        var turno = new Turno
        {
            ClienteId = cliente.Id,
            ServicioId = servicio.Id,
            FechaHora = fechaHora,
            Estado = EstadoTurno.Confirmado,
            FechaCreacion = clock.UtcNow
        };

        var created = await turnoRepository.CreateAsync(turno, cancellationToken);
        created.Cliente = cliente;
        created.Servicio = servicio;
        return MapToResponseDTO(created);
    }

    public async Task<TurnoResponseDTO> UpdateAsync(
        Guid id,
        TurnoUpdateDTO dto,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var turno = await turnoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new TurnoNotFoundException($"No existe el turno con ID {id}.");

        EnsureCanOperate(turno);
        EnsureModifiable(turno);
        var servicio = await GetActiveServicioAsync(dto.ServicioId, cancellationToken);
        var fechaHora = ValidateFuture(dto.FechaHora);

        if (await turnoRepository.ExistsActiveAtAsync(fechaHora, id, cancellationToken))
        {
            throw new HorarioNoDisponibleException(
                "El horario seleccionado ya no se encuentra disponible.");
        }

        turno.ServicioId = servicio.Id;
        turno.Servicio = servicio;
        turno.FechaHora = fechaHora;
        await turnoRepository.UpdateAsync(turno, cancellationToken);
        return MapToResponseDTO(turno);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var turno = await turnoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new TurnoNotFoundException($"No existe el turno con ID {id}.");

        EnsureCanOperate(turno);
        if (turno.Estado != EstadoTurno.Confirmado || turno.FechaHora <= clock.UtcNow)
        {
            throw new TurnoNoCancelableException(
                "El turno no puede cancelarse porque no está activo o ya venció.");
        }

        turno.Estado = EstadoTurno.Cancelado;
        await turnoRepository.UpdateAsync(turno, cancellationToken);
    }

    private Guid ResolveClienteId(Guid? requestedClienteId)
    {
        if (actorContext.Rol == RolUsuario.Cliente)
        {
            var ownClienteId = actorContext.ClienteId
                ?? throw new ForbiddenException("El usuario no está asociado a un cliente.");

            if (requestedClienteId.HasValue && requestedClienteId.Value != ownClienteId)
            {
                throw new ForbiddenException("El cliente solo puede crear turnos propios.");
            }

            return ownClienteId;
        }

        if (actorContext.Rol is RolUsuario.Empleado or RolUsuario.Administrador)
        {
            return requestedClienteId
                ?? throw new ValidationException("El cliente es obligatorio al crear el turno.");
        }

        throw new ForbiddenException("El rol no tiene permiso para crear turnos.");
    }

    private async Task<Cliente> GetActiveClienteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ClienteNotFoundException($"No existe el cliente con ID {id}.");
        if (!cliente.Activo)
        {
            throw new ClienteNotFoundException($"El cliente con ID {id} no está activo.");
        }

        return cliente;
    }

    private async Task<Servicio> GetActiveServicioAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var servicio = await servicioRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ServicioNotFoundException($"No existe el servicio con ID {id}.");
        if (!servicio.Activo)
        {
            throw new ServicioNotFoundException($"El servicio con ID {id} no está activo.");
        }

        return servicio;
    }

    private DateTimeOffset ValidateFuture(DateTimeOffset value)
    {
        var utcDate = value.ToUniversalTime();
        if (utcDate <= clock.UtcNow)
        {
            throw new ValidationException("La fecha y hora del turno deben ser futuras.");
        }

        return utcDate;
    }

    private void EnsureCanOperate(Turno turno)
    {
        if (actorContext.Rol == RolUsuario.Cliente && actorContext.ClienteId != turno.ClienteId)
        {
            throw new ForbiddenException("El cliente solo puede operar sobre sus propios turnos.");
        }

        if (actorContext.Rol is not (RolUsuario.Cliente or RolUsuario.Empleado or RolUsuario.Administrador))
        {
            throw new ForbiddenException("El rol no tiene permiso sobre turnos.");
        }
    }

    private void EnsureModifiable(Turno turno)
    {
        if (turno.Estado != EstadoTurno.Confirmado || turno.FechaHora <= clock.UtcNow)
        {
            throw new TurnoNoModificableException(
                "El turno no puede modificarse porque no está activo o ya venció.");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!actorContext.IsAuthenticated)
        {
            throw new ForbiddenException("El usuario debe estar autenticado.");
        }
    }

    private void EnsureStaff()
    {
        if (!actorContext.IsAuthenticated
            || actorContext.Rol is not (RolUsuario.Empleado or RolUsuario.Administrador))
        {
            throw new ForbiddenException("La agenda requiere rol Empleado o Administrador.");
        }
    }

    private static TurnoResponseDTO MapToResponseDTO(Turno turno) => new()
    {
        Id = turno.Id,
        ClienteId = turno.ClienteId,
        Cliente = $"{turno.Cliente.Nombre} {turno.Cliente.Apellido}".Trim(),
        ServicioId = turno.ServicioId,
        Servicio = turno.Servicio.Nombre,
        Importe = turno.Servicio.Importe,
        FechaHora = turno.FechaHora,
        Estado = turno.Estado,
        FechaCreacion = turno.FechaCreacion
    };
}
