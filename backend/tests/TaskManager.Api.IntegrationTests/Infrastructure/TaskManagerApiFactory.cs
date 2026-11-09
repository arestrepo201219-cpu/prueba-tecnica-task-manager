using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManager.Application.Abstractions;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

/// <summary>Levanta la API completa para las pruebas, cambiando solo la base de datos por listas en memoria</summary>
public sealed class TaskManagerApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public InMemoryDatabase Database { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TaskManagerDb", "Server=not-used;Database=not-used");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ITaskRepository>();
            services.RemoveAll<ITaskQueries>();
            services.RemoveAll<IUnitOfWork>();

            services.AddSingleton(Database);
            services.AddScoped<IUserRepository, InMemoryUserRepository>();
            services.AddScoped<ITaskRepository, InMemoryTaskRepository>();
            services.AddScoped<ITaskQueries, InMemoryTaskQueries>();
            services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();
        });
    }
}
