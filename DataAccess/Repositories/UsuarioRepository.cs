using Microsoft.EntityFrameworkCore;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Entities;
using TurnosLavadero.DataAccess.Interfaces;

namespace TurnosLavadero.DataAccess.Repositories;

public sealed class UsuarioRepository(LavaderoDbContext context) : IUsuarioRepository
{
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Usuarios.AsNoTracking().Include(x => x.Cliente)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    public async Task<Usuario> CreateAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        usuario.Id = usuario.Id == Guid.Empty ? Guid.NewGuid() : usuario.Id;
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(cancellationToken);
        return usuario;
    }

    public async Task<Usuario> CreateClienteUserAsync(
        Cliente cliente,
        Usuario usuario,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        cliente.Id = cliente.Id == Guid.Empty ? Guid.NewGuid() : cliente.Id;
        usuario.Id = usuario.Id == Guid.Empty ? Guid.NewGuid() : usuario.Id;
        usuario.ClienteId = cliente.Id;

        context.Clientes.Add(cliente);
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return usuario;
    }

    public async Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        context.Usuarios.Update(usuario);
        await context.SaveChangesAsync(cancellationToken);
    }
}
