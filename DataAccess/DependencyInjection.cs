using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TurnosLavadero.DataAccess.Context;
using TurnosLavadero.DataAccess.Interfaces;
using TurnosLavadero.DataAccess.Repositories;

namespace TurnosLavadero.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<LavaderoDbContext>(options =>
            options.UseSqlite(connectionString, sqlite =>
                sqlite.MigrationsAssembly(typeof(LavaderoDbContext).Assembly.GetName().Name!)));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IServicioRepository, ServicioRepository>();
        services.AddScoped<ITurnoRepository, TurnoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRecordatorioRepository, RecordatorioRepository>();

        return services;
    }
}
