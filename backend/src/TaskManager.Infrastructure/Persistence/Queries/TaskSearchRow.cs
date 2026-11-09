namespace TaskManager.Infrastructure.Persistence.Queries;

/// <summary>
/// Cada fila que devuelve dbo.usp_Tasks_Search. Los nombres tienen que ser iguales a los
/// de las columnas del procedimiento, si no EF no las llena
/// </summary>
internal sealed class TaskSearchRow
{
    public int Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public byte StatusId { get; init; }

    public string StatusCode { get; init; } = string.Empty;

    public int UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string UserEmail { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    /// <summary>Sale de la columna calculada Priority</summary>
    public string? Priority { get; init; }

    public DateOnly? DueDate { get; init; }

    /// <summary>Arreglo de etiquetas tal como sale del JSON</summary>
    public string? TagsJson { get; init; }

    /// <summary>Objeto de metadatos tal como sale del JSON</summary>
    public string? MetadataJson { get; init; }

    /// <summary>Total de tareas que cumplen el filtro, sin importar la página</summary>
    public int TotalCount { get; init; }
}
