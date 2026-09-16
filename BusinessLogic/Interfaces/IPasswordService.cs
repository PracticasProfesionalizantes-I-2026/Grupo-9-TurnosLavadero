namespace TurnosLavadero.BusinessLogic.Interfaces;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string passwordHash, string providedPassword);
}
