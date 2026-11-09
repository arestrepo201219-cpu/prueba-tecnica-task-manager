using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Abstractions;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Persistence.Queries;
using TaskManager.Infrastructure.Persistence.Repositories;

namespace TaskManager.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "TaskManagerDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"No se encontró la cadena de conexión '{ConnectionStringName}' (ConnectionStrings:{ConnectionStringName}).");

        services.AddDbContext<TaskManagerDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskQueries, TaskQueries>();

        return services;
    }
}
