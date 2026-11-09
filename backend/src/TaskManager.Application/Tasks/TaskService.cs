using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

/// <summary>
/// Operaciones sobre tareas. Valida lo que llega, deja que la entidad aplique las reglas,
/// guarda y devuelve la tarea actualizada. Aquí no hay SQL ni nada de HTTP
/// </summary>
internal sealed partial class TaskService(
    ITaskRepository taskRepository,
    IUserRepository userRepository,
    ITaskQueries taskQueries,
    IUnitOfWork unitOfWork,
    IValidator<CreateTaskRequest> createValidator,
    IValidator<UpdateTaskStatusRequest> statusValidator,
    IValidator<AssignTaskRequest> assignValidator,
    IValidator<TaskQueryParameters> queryValidator,
    TimeProvider timeProvider,
    ILogger<TaskService> logger) : ITaskService
{
    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureUserExistsAsync(request.UserId, cancellationToken);

        var task = TaskItem.Create(
            request.Title,
            request.Description,
            request.UserId,
            request.AdditionalInfo.ToDomain(),
            UtcNow());

        taskRepository.Add(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogTaskCreated(logger, task.Id, task.UserId);
        return await GetByIdAsync(task.Id, cancellationToken);
    }

    public async Task<PagedResult<TaskResponse>> SearchAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        await queryValidator.ValidateAndThrowAsync(parameters, cancellationToken);

        return await taskQueries.SearchAsync(parameters, cancellationToken);
    }

    public async Task<TaskResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await taskQueries.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException("Tarea", id);

    public async Task<TaskResponse> ChangeStatusAsync(int id, UpdateTaskStatusRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await statusValidator.ValidateAndThrowAsync(request, cancellationToken);

        var task = await GetTaskOrThrowAsync(id, cancellationToken);
        var previousStatus = task.Status;

        // La entidad es la que decide si el cambio de estado es válido.
        if (task.ChangeStatus(request.Status!.Value, UtcNow()))
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            LogStatusChanged(logger, task.Id, previousStatus, task.Status);
        }

        return await GetByIdAsync(task.Id, cancellationToken);
    }

    public async Task<TaskResponse> AssignAsync(int id, AssignTaskRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await assignValidator.ValidateAndThrowAsync(request, cancellationToken);

        var task = await GetTaskOrThrowAsync(id, cancellationToken);
        await EnsureUserExistsAsync(request.UserId, cancellationToken);

        task.AssignTo(request.UserId, UtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogTaskAssigned(logger, task.Id, task.UserId);
        return await GetByIdAsync(task.Id, cancellationToken);
    }

    private async Task<TaskItem> GetTaskOrThrowAsync(int id, CancellationToken cancellationToken) =>
        await taskRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException("Tarea", id);

    /// <summary>
    /// Si el usuario no existe se responde 400 y no 404, porque el dato viene en el cuerpo
    /// de la petición y no en la URL. Es un campo inválido.
    /// </summary>
    private async Task EnsureUserExistsAsync(int userId, CancellationToken cancellationToken)
    {
        if (!await userRepository.ExistsAsync(userId, cancellationToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(CreateTaskRequest.UserId), $"El usuario con id '{userId}' no existe."),
            ]);
        }
    }

    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;

    [LoggerMessage(Level = LogLevel.Information, Message = "Tarea {TaskId} creada y asignada al usuario {UserId}")]
    private static partial void LogTaskCreated(ILogger logger, int taskId, int userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tarea {TaskId} cambió de estado {From} a {To}")]
    private static partial void LogStatusChanged(ILogger logger, int taskId, TaskItemStatus from, TaskItemStatus to);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tarea {TaskId} reasignada al usuario {UserId}")]
    private static partial void LogTaskAssigned(ILogger logger, int taskId, int userId);
}
