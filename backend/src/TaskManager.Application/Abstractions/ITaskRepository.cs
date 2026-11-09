using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Abstractions;

/// <summary>Para crear y modificar tareas. Las consultas del listado van por ITaskQueries</summary>
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    void Add(TaskItem task);
}
