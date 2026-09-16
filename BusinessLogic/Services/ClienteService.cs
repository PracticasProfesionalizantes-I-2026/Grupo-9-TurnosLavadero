using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Clientes;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.BusinessLogic.Services;

public sealed class ClienteService(
    IClienteRepository clienteRepository,
    IActorContext actorContext) : IClienteService
{
    public async Task<IReadOnlyCollection<ClienteResponseDTO>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var clientes = await clienteRepository.GetAllAsync(cancellationToken);
        return clientes.Select(MapToResponseDTO).ToArray();
    }

    public async Task<ClienteResponseDTO> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsureCanAccess(id);
        var cliente = await clienteRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ClienteNotFoundException($"No existe el cliente con ID {id}.");
        return MapToResponseDTO(cliente);
    }

    public async Task<ClienteResponseDTO> CreateAsync(
        ClienteCreateDTO dto,
        CancellationToken cancellationToken = default)
    {
        EnsureStaff();
        var nombre = NormalizeRequired(dto.Nombre, "nombre");
        var apellido = NormalizeRequired(dto.Apellido, "apellido");
        var email = NormalizeEmail(dto.EmailContacto);

        if (await clienteRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken))
        {
            throw new ClienteDuplicadoException($"Ya existe un cliente con el email {email}.");
        }

        var cliente = new Cliente
        {
            Nombre = nombre,
            Apellido = apellido,
            EmailContacto = email,
            Telefono = NormalizeOptional(dto.Telefono),
            NotificacionesHabilitadas = dto.NotificacionesHabilitadas,
            Activo = true
        };

        var created = await clienteRepository.CreateAsync(cliente, cancellationToken);
        return MapToResponseDTO(created);
    }

    public async Task<ClienteResponseDTO> UpdateAsync(
        Guid id,
        ClienteUpdateDTO dto,
        CancellationToken cancellationToken = default)
    {
        EnsureCanAccess(id);
        var cliente = await clienteRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ClienteNotFoundException($"No existe el cliente con ID {id}.");

        var email = NormalizeEmail(dto.EmailContacto);
        if (await clienteRepository.ExistsByEmailAsync(email, id, cancellationToken))
        {
            throw new ClienteDuplicadoException($"Ya existe un cliente con el email {email}.");
        }

        cliente.Nombre = NormalizeRequired(dto.Nombre, "nombre");
        cliente.Apellido = NormalizeRequired(dto.Apellido, "apellido");
        cliente.EmailContacto = email;
        cliente.Telefono = NormalizeOptional(dto.Telefono);
        cliente.NotificacionesHabilitadas = dto.NotificacionesHabilitadas;

        if (actorContext.Rol is RolUsuario.Empleado or RolUsuario.Administrador)
        {
            cliente.Activo = dto.Activo;
        }

        await clienteRepository.UpdateAsync(cliente, cancellationToken);
        return MapToResponseDTO(cliente);
    }

    private void EnsureStaff()
    {
        if (!actorContext.IsAuthenticated
            || actorContext.Rol is not (RolUsuario.Empleado or RolUsuario.Administrador))
        {
            throw new ForbiddenException("La operación requiere rol Empleado o Administrador.");
        }
    }

    private void EnsureCanAccess(Guid clienteId)
    {
        if (!actorContext.IsAuthenticated)
        {
            throw new ForbiddenException("El usuario debe estar autenticado.");
        }

        if (actorContext.Rol == RolUsuario.Cliente && actorContext.ClienteId != clienteId)
        {
            throw new ForbiddenException("El cliente solo puede acceder a sus propios datos.");
        }
    }

    private static string NormalizeRequired(string value, string field)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationException($"El campo {field} es obligatorio.");
        }

        return normalized;
    }

    private static string NormalizeEmail(string email)
    {
        var normalized = NormalizeRequired(email, "email").ToLowerInvariant();
        if (!normalized.Contains('@'))
        {
            throw new ValidationException("El email no tiene un formato válido.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ClienteResponseDTO MapToResponseDTO(Cliente cliente) => new()
    {
        Id = cliente.Id,
        Nombre = cliente.Nombre,
        Apellido = cliente.Apellido,
        EmailContacto = cliente.EmailContacto,
        Telefono = cliente.Telefono,
        NotificacionesHabilitadas = cliente.NotificacionesHabilitadas,
        Activo = cliente.Activo
    };
}
