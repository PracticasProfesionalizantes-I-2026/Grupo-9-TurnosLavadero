using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Servicios;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.BusinessLogic.Services;

public sealed class ServicioService(
    IServicioRepository servicioRepository,
    IActorContext actorContext) : IServicioService
{
    public async Task<IReadOnlyCollection<ServicioResponseDTO>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var servicios = await servicioRepository.GetActiveAsync(cancellationToken);
        return servicios.Select(MapToResponseDTO).ToArray();
    }

    public async Task<ServicioResponseDTO> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var servicio = await servicioRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ServicioNotFoundException($"No existe el servicio con ID {id}.");
        return MapToResponseDTO(servicio);
    }

    public async Task<ServicioResponseDTO> CreateAsync(
        ServicioCreateDTO dto,
        CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        var nombre = NormalizeName(dto.Nombre);
        ValidateImporte(dto.Importe);

        if (await servicioRepository.ExistsByNormalizedNameAsync(
                nombre,
                cancellationToken: cancellationToken))
        {
            throw new ServicioDuplicadoException($"Ya existe un servicio llamado {nombre}.");
        }

        var servicio = new Servicio
        {
            Nombre = nombre,
            Importe = dto.Importe,
            Activo = true
        };

        var created = await servicioRepository.CreateAsync(servicio, cancellationToken);
        return MapToResponseDTO(created);
    }

    public async Task<ServicioResponseDTO> UpdateAsync(
        Guid id,
        ServicioUpdateDTO dto,
        CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        var servicio = await servicioRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ServicioNotFoundException($"No existe el servicio con ID {id}.");
        var nombre = NormalizeName(dto.Nombre);
        ValidateImporte(dto.Importe);

        if (await servicioRepository.ExistsByNormalizedNameAsync(nombre, id, cancellationToken))
        {
            throw new ServicioDuplicadoException($"Ya existe un servicio llamado {nombre}.");
        }

        servicio.Nombre = nombre;
        servicio.Importe = dto.Importe;
        servicio.Activo = dto.Activo;
        await servicioRepository.UpdateAsync(servicio, cancellationToken);
        return MapToResponseDTO(servicio);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAdministrator();
        var servicio = await servicioRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ServicioNotFoundException($"No existe el servicio con ID {id}.");

        if (await servicioRepository.HasRelatedTurnosAsync(id, cancellationToken))
        {
            throw new ServicioConTurnosException(
                "No se puede eliminar un servicio que posee turnos relacionados.");
        }

        await servicioRepository.DeleteAsync(servicio, cancellationToken);
    }

    private void EnsureAuthenticated()
    {
        if (!actorContext.IsAuthenticated)
        {
            throw new ForbiddenException("El usuario debe estar autenticado.");
        }
    }

    private void EnsureAdministrator()
    {
        if (!actorContext.IsAuthenticated || actorContext.Rol != RolUsuario.Administrador)
        {
            throw new ForbiddenException("La operación requiere rol Administrador.");
        }
    }

    private static string NormalizeName(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationException("El nombre del servicio es obligatorio.");
        }

        return normalized;
    }

    private static void ValidateImporte(decimal importe)
    {
        if (importe <= 0)
        {
            throw new ValidationException("El importe debe ser mayor que cero.");
        }
    }

    private static ServicioResponseDTO MapToResponseDTO(Servicio servicio) => new()
    {
        Id = servicio.Id,
        Nombre = servicio.Nombre,
        Importe = servicio.Importe,
        Activo = servicio.Activo
    };
}
