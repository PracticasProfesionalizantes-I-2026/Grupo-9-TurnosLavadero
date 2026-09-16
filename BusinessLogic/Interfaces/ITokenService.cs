using TurnosLavadero.DataAccess.Entities;

namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface ITokenService
{
    TokenResult CreateToken(Usuario usuario);
}

public sealed record TokenResult(string Token, DateTimeOffset ExpiresAt);
