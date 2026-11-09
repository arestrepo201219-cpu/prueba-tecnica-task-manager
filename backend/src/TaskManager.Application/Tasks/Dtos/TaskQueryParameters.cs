using TaskManager.Application.Common.Models;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Dtos;

/// <summary>Filtros del listado de tareas. Ninguno es obligatorio</summary>
public sealed record TaskQueryParameters
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int? UserId { get; init; }

    public TaskItemStatus? Status { get; init; }

    /// <summary>Prioridad guardada en el JSON</summary>
    public TaskPriority? Priority { get; init; }

    /// <summary>Etiqueta guardada en el JSON</summary>
    public string? Tag { get; init; }

    /// <summary>Orden por fecha de creación. Por defecto, de la más nueva a la más vieja</summary>
    public SortDirection Sort { get; init; } = SortDirection.Desc;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
