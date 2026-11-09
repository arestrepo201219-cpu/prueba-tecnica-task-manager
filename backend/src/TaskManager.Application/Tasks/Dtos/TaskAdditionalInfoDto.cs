using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>Información adicional de la tarea. En la base de datos queda como JSON</summary>
public sealed record TaskAdditionalInfoDto(
    TaskPriority? Priority,
    DateOnly? DueDate,
    IReadOnlyList<string>? Tags,
    IReadOnlyDictionary<string, string>? Metadata);
