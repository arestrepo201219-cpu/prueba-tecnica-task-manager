using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Tasks;
using TaskManager.Application.Users;

namespace TaskManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITaskService, TaskService>();

        // Se inyecta el reloj para poder fijar la hora en las pruebas
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
