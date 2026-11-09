using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;

namespace TaskManager.Api.IntegrationTests.Infrastructure;

internal sealed class InMemoryUserRepository(InMemoryDatabase database) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(database.Users.FirstOrDefault(user => user.Id == id));

    public Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>([.. database.Users.OrderBy(user => user.Name, StringComparer.Ordinal)]);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(database.Users.Exists(user => user.Id == id));

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(database.Users.Exists(user => user.Email == normalizedEmail));

    public void Add(User user)
    {
        lock (database.SyncRoot)
        {
            database.AssignIdentity(user);
            database.Users.Add(user);
        }
    }
}

internal sealed class InMemoryTaskRepository(InMemoryDatabase database) : ITaskRepository
{
    public Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(database.Tasks.FirstOrDefault(task => task.Id == id));

    public void Add(TaskItem task)
    {
        lock (database.SyncRoot)
        {
            database.AssignIdentity(task);
            database.Tasks.Add(task);
        }
    }
}

internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

/// <summary>Hace en memoria lo mismo que el procedimiento dbo.usp_Tasks_Search.</summary>
internal sealed class InMemoryTaskQueries(InMemoryDatabase database) : ITaskQueries
{
    public Task<PagedResult<TaskResponse>> SearchAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var query = database.Tasks.AsEnumerable();

        if (parameters.UserId is { } userId)
        {
            query = query.Where(task => task.UserId == userId);
        }

        if (parameters.Status is { } status)
        {
            query = query.Where(task => task.Status == status);
        }

        if (parameters.Priority is { } priority)
        {
            query = query.Where(task => task.AdditionalInfo?.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Tag))
        {
            query = query.Where(task => task.AdditionalInfo?.Tags.Contains(parameters.Tag.Trim()) == true);
        }

        var ordered = parameters.Sort == SortDirection.Asc
            ? query.OrderBy(task => task.CreatedAt).ThenByDescending(task => task.Id)
            : query.OrderByDescending(task => task.CreatedAt).ThenByDescending(task => task.Id);

        var all = ordered.ToList();
        var page = all.Skip((parameters.Page - 1) * parameters.PageSize).Take(parameters.PageSize).Select(ToResponse).ToList();

        return Task.FromResult(new PagedResult<TaskResponse>(page, parameters.Page, parameters.PageSize, all.Count));
    }

    public Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var task = database.Tasks.FirstOrDefault(item => item.Id == id);
        return Task.FromResult(task is null ? null : ToResponse(task));
    }

    private TaskResponse ToResponse(TaskItem task)
    {
        var user = database.Users.Single(item => item.Id == task.UserId);
        return new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            new AssignedUserDto(user.Id, user.Name, user.Email),
            task.AdditionalInfo.ToDto(),
            task.AllowedNextStatuses,
            task.CreatedAt,
            task.UpdatedAt);
    }
}
