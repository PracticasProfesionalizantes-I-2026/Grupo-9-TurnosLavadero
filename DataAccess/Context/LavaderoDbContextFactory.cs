using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TurnosLavadero.DataAccess.Context;

public sealed class LavaderoDbContextFactory : IDesignTimeDbContextFactory<LavaderoDbContext>
{
    public LavaderoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LavaderoDbContext>()
            .UseSqlite("Data Source=turnos-lavadero.db", sqlite =>
                sqlite.MigrationsAssembly(typeof(LavaderoDbContext).Assembly.GetName().Name!))
            .Options;

        return new LavaderoDbContext(options);
    }
}
