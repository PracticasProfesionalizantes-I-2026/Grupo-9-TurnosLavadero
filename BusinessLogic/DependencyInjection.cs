using Microsoft.Extensions.DependencyInjection;
using TurnosLavadero.BusinessLogic.Interfaces;
using TurnosLavadero.BusinessLogic.Services;

namespace TurnosLavadero.BusinessLogic;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IServicioService, ServicioService>();
        services.AddScoped<ITurnoService, TurnoService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRecordatorioService, RecordatorioService>();
        return services;
    }
}
