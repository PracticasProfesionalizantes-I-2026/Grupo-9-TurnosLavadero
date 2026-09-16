using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.Shared.DTOs.Auth;
using TurnosLavadero.Shared.Enums;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.BusinessLogic.Services;

public sealed class AuthService(
    IClienteRepository clienteRepository,
    IUsuarioRepository usuarioRepository,
    IPasswordService passwordService,
    ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponseDTO> RegisterClienteAsync(
        RegisterClienteDTO dto,
        CancellationToken cancellationToken = default)
    {
        var nombre = NormalizeRequired(dto.Nombre, "nombre");
        var apellido = NormalizeRequired(dto.Apellido, "apellido");
        var email = NormalizeEmail(dto.Email);

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
        {
            throw new ValidationException("La contraseña debe tener al menos 8 caracteres.");
        }

        if (await clienteRepository.ExistsByEmailAsync(email, cancellationToken: cancellationToken)
            || await usuarioRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ClienteDuplicadoException($"Ya existe una cuenta con el email {email}.");
        }

        var cliente = new Cliente
        {
            Nombre = nombre,
            Apellido = apellido,
            EmailContacto = email,
            Telefono = NormalizeOptional(dto.Telefono),
            NotificacionesHabilitadas = true,
            Activo = true
        };

        var usuario = new Usuario
        {
            Email = email,
            PasswordHash = passwordService.Hash(dto.Password),
            Rol = RolUsuario.Cliente,
            Activo = true
        };

        var created = await usuarioRepository.CreateClienteUserAsync(
            cliente,
            usuario,
            cancellationToken);
        created.Cliente = cliente;
        return MapToResponseDTO(created);
    }

    public async Task<AuthResponseDTO> LoginAsync(
        LoginDTO dto,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(dto.Email);
        var usuario = await usuarioRepository.GetByEmailAsync(email, cancellationToken);

        if (usuario is null
            || !usuario.Activo
            || !passwordService.Verify(usuario.PasswordHash, dto.Password))
        {
            throw new CredencialesInvalidasException("Email o contraseña incorrectos.");
        }

        return MapToResponseDTO(usuario);
    }

    private AuthResponseDTO MapToResponseDTO(Usuario usuario)
    {
        var token = tokenService.CreateToken(usuario);
        return new AuthResponseDTO
        {
            Token = token.Token,
            ExpiraEn = token.ExpiresAt,
            UsuarioId = usuario.Id,
            ClienteId = usuario.ClienteId,
            Email = usuario.Email,
            Rol = usuario.Rol
        };
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

    private static string NormalizeEmail(string value)
    {
        var normalized = NormalizeRequired(value, "email").ToLowerInvariant();
        if (!normalized.Contains('@'))
        {
            throw new ValidationException("El email no tiene un formato válido.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
