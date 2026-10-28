using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.Shared.Exceptions;

namespace TurnosLavadero.DataAccess.Repositories;

// La comprobación previa del Service no basta cuando dos peticiones escriben a la vez.
internal static class UniqueConstraintGuard
{
    public static async Task SaveChangesAsync(LavaderoDbContext context, CancellationToken cancellationToken)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException
            { SqliteExtendedErrorCode: 2067 } sqlite
            && (sqlite.Message.Contains("Clientes.EmailContacto", StringComparison.Ordinal)
                || sqlite.Message.Contains("Usuarios.Email", StringComparison.Ordinal)))
        {
            throw new ClienteDuplicadoException("Ya existe una cuenta o cliente con ese correo.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException
            { SqliteExtendedErrorCode: 2067 } sqlite
            && sqlite.Message.Contains("Servicios.Nombre", StringComparison.Ordinal))
        {
            throw new ServicioDuplicadoException("Ya existe un servicio con ese nombre.");
        }
    }
}
