using TaskManager.Domain.Common;

namespace TaskManager.Domain.Tasks;

/// <summary>
/// Información adicional de una tarea: prioridad, fecha estimada, etiquetas y metadatos
/// en la base de datos se guarda como JSON, pero eso lo maneja Infrastructure; aquí es
/// un objeto normal.
/// </summary>
public sealed record TaskAdditionalInfo
{
    public const int MaxTags = 10;
    public const int TagMaxLength = 50;
    public const int MaxMetadataEntries = 20;
    public const int MetadataKeyMaxLength = 50;
    public const int MetadataValueMaxLength = 500;

    private TaskAdditionalInfo(
        TaskPriority? priority,
        DateOnly? dueDate,
        IReadOnlyList<string> tags,
        IReadOnlyDictionary<string, string> metadata)
    {
        Priority = priority;
        DueDate = dueDate;
        Tags = tags;
        Metadata = metadata;
    }

    public TaskPriority? Priority { get; }

    /// <summary>Fecha en la que se espera terminar la tarea.</summary>
    public DateOnly? DueDate { get; }

    public IReadOnlyList<string> Tags { get; }

    /// <summary>Datos libres en formato clave/valor, por ejemplo el sprint.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public bool IsEmpty => Priority is null && DueDate is null && Tags.Count == 0 && Metadata.Count == 0;

    /// <summary>
    /// Limpia las etiquetas (quita espacios, vacías y repetidas sin importar mayúsculas)
    /// y las claves de los metadatos antes de crear el objeto.
    /// </summary>
    public static TaskAdditionalInfo Create(
        TaskPriority? priority,
        DateOnly? dueDate,
        IEnumerable<string>? tags,
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (priority is not null && !Enum.IsDefined(priority.Value))
        {
            throw new DomainException("La prioridad indicada no es válida.", "invalid_priority");
        }

        var normalizedTags = (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedTags.Count > MaxTags)
        {
            throw new DomainException($"Una tarea admite como máximo {MaxTags} etiquetas.", "too_many_tags");
        }

        if (normalizedTags.Any(tag => tag.Length > TagMaxLength))
        {
            throw new DomainException(
                $"Cada etiqueta admite como máximo {TagMaxLength} caracteres.", "tag_too_long");
        }

        var normalizedMetadata = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in metadata ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new DomainException("Las claves de los metadatos no pueden estar vacías.", "invalid_metadata");
            }

            var trimmedKey = key.Trim();
            if (trimmedKey.Length > MetadataKeyMaxLength || (value?.Length ?? 0) > MetadataValueMaxLength)
            {
                throw new DomainException("Los metadatos superan la longitud permitida.", "invalid_metadata");
            }

            normalizedMetadata[trimmedKey] = value ?? string.Empty;
        }

        if (normalizedMetadata.Count > MaxMetadataEntries)
        {
            throw new DomainException(
                $"Una tarea admite como máximo {MaxMetadataEntries} metadatos.", "too_many_metadata_entries");
        }

        return new TaskAdditionalInfo(priority, dueDate, normalizedTags, normalizedMetadata);
    }
}
