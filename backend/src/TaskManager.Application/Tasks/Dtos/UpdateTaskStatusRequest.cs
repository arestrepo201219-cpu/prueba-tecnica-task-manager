using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>Status acepta null para poder responder "el estado es obligatorio" cuando no lo envían</summary>
public sealed record UpdateTaskStatusRequest(TaskItemStatus? Status);
