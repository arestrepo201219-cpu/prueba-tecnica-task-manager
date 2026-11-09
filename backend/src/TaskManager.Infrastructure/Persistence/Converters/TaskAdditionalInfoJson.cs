using System.Globalization;
using System.Text;
using System.Text.Json;
using TaskManager.Domain.Tasks;

namespace TaskManager.Infrastructure.Persistence.Converters;

/// <summary>
/// Convierte TaskAdditionalInfo a JSON y de vuelta, para la columna Tasks.AdditionalInfo
/// El JSON se arma a mano porque las consultas SQL buscan nombres exactos ($.priority, $.tags...)
/// y no queremos que un cambio en el serializador los rompa. Queda así:
/// {"priority":"High","dueDate":"2026-12-01","tags":["api"],"metadata":{"sprint":"S-24"}}
/// Al leer se ignora lo que no se reconozca, porque el JSON también se puede editar desde SQL
/// </summary>
internal static class TaskAdditionalInfoJson
{
    public static string? Serialize(TaskAdditionalInfo? info)
    {
        if (info is null || info.IsEmpty)
        {
            return null;
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            if (info.Priority is { } priority)
            {
                writer.WriteString("priority", priority.ToString());
            }

            if (info.DueDate is { } dueDate)
            {
                writer.WriteString("dueDate", dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }

            if (info.Tags.Count > 0)
            {
                writer.WriteStartArray("tags");
                foreach (var tag in info.Tags)
                {
                    writer.WriteStringValue(tag);
                }

                writer.WriteEndArray();
            }

            if (info.Metadata.Count > 0)
            {
                writer.WriteStartObject("metadata");
                foreach (var (key, value) in info.Metadata)
                {
                    writer.WriteString(key, value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static TaskAdditionalInfo? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var priority = root.TryGetProperty("priority", out var priorityElement) && priorityElement.ValueKind == JsonValueKind.String
            ? ParsePriority(priorityElement.GetString())
            : null;

        var dueDate = root.TryGetProperty("dueDate", out var dueDateElement) && dueDateElement.ValueKind == JsonValueKind.String
            ? ParseDate(dueDateElement.GetString())
            : null;

        var tags = root.TryGetProperty("tags", out var tagsElement) ? ReadTags(tagsElement) : [];
        var metadata = root.TryGetProperty("metadata", out var metadataElement)
            ? ReadMetadata(metadataElement)
            : new Dictionary<string, string>();

        var info = TaskAdditionalInfo.Create(priority, dueDate, tags, metadata);
        return info.IsEmpty ? null : info;
    }

    /// <summary>Lee las etiquetas que devuelve el procedimiento de búsqueda.</summary>
    public static IReadOnlyList<string> ReadTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
        {
            return [];
        }

        using var document = JsonDocument.Parse(tagsJson);
        return ReadTags(document.RootElement);
    }

    /// <summary>Lee los metadatos que devuelve el procedimiento de búsqueda.</summary>
    public static IReadOnlyDictionary<string, string> ReadMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>();
        }

        using var document = JsonDocument.Parse(metadataJson);
        return ReadMetadata(document.RootElement);
    }

    public static TaskPriority? ParsePriority(string? value) =>
        Enum.TryParse<TaskPriority>(value, ignoreCase: true, out var priority) && Enum.IsDefined(priority)
            ? priority
            : null;

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static List<string> ReadTags(JsonElement element) =>
        element.ValueKind != JsonValueKind.Array
            ? []
            : [.. element.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()!)];

    private static Dictionary<string, string> ReadMetadata(JsonElement element)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        if (element.ValueKind != JsonValueKind.Object)
        {
            return metadata;
        }

        foreach (var property in element.EnumerateObject())
        {
            // Si desde SQL guardaron un número o un objeto, se toma como texto para no perderlo.
            metadata[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()!
                : property.Value.GetRawText();
        }

        return metadata;
    }
}
