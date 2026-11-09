using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Tasks;

public interface ITaskService
{
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<TaskResponse>> SearchAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<TaskResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TaskResponse> ChangeStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default);

    Task<TaskResponse> AssignAsync(int id, AssignTaskRequest request, CancellationToken cancellationToken = default);
}
