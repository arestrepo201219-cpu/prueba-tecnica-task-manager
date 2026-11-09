using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Tasks;

namespace TaskManager.Infrastructure.Persistence.Repositories;

internal sealed class TaskRepository(TaskManagerDbContext dbContext) : ITaskRepository
{
    public Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public void Add(TaskItem task) => dbContext.Tasks.Add(task);
}
