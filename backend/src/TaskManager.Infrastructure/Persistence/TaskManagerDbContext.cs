using Microsoft.EntityFrameworkCore;
using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Persistence.Converters;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core. Las tablas se crean con el script database/01_create_database.sql,
/// no con migraciones; aquí solo se mapean
/// </summary>
public sealed class TaskManagerDbContext(DbContextOptions<TaskManagerDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagerDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }
}
