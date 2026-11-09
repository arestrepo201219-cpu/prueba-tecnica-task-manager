using System.Collections.Frozen;

namespace TaskManager.Domain.Tasks;

/// <summary>
/// Define a qué estado puede pasar una tarea desde cada estado
/// si se necesita un estado o un cambio nuevo, basta con agregarlo aquí; la entidad no cambia
/// hay que mantenerlo igual a la tabla dbo.TaskStatusTransitions
/// </summary>
/// <remarks>
/// de Pending no se puede ir directo a Done: primero tiene que pasar por InProgress
/// </remarks>
public static class TaskStatusTransitions
{
    private static readonly FrozenDictionary<TaskItemStatus, TaskItemStatus[]> Allowed =
        new Dictionary<TaskItemStatus, TaskItemStatus[]>
        {
            [TaskItemStatus.Pending] = [TaskItemStatus.InProgress],
            [TaskItemStatus.InProgress] = [TaskItemStatus.Pending, TaskItemStatus.Done],
            [TaskItemStatus.Done] = [TaskItemStatus.InProgress],
        }.ToFrozenDictionary();

    public static bool CanTransition(TaskItemStatus from, TaskItemStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyList<TaskItemStatus> AllowedFrom(TaskItemStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : [];
}
