using System.Reflection;
using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Reemplaza la base de datos en las pruebas de la API. Así se prueba todo lo demás
/// (rutas, validaciones, errores, JSON) sin necesitar SQL Server
/// </summary>
public sealed class InMemoryDatabase
{
    private int _nextUserId;
    private int _nextTaskId;

    public List<User> Users { get; } = [];

    public List<TaskItem> Tasks { get; } = [];

    public object SyncRoot { get; } = new();

    public void AssignIdentity(User user) => SetId(user, Interlocked.Increment(ref _nextUserId));

    public void AssignIdentity(TaskItem task) => SetId(task, Interlocked.Increment(ref _nextTaskId));

    private static void SetId(object entity, int id) =>
        entity.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(entity, id);
}
