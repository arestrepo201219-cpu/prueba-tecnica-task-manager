namespace TaskManager.Domain.Common;

/// <summary>
/// Validaciones básicas que usan las entidades al crearse o modificarse
/// Los mensajes para el usuario salen de los validadores de Application; esto es solo
/// para que una entidad nunca quede con datos inválidos
/// </summary>
internal static class Guard
{
    public static string NotBlank(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"El campo '{fieldName}' es obligatorio.", "required_field");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                $"El campo '{fieldName}' no puede superar {maxLength} caracteres.", "max_length_exceeded");
        }

        return trimmed;
    }

    public static string? Optional(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(
                $"El campo '{fieldName}' no puede superar {maxLength} caracteres.", "max_length_exceeded");
        }

        return trimmed;
    }

    public static int PositiveId(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainException($"El campo '{fieldName}' debe ser un identificador válido.", "invalid_identifier");
        }

        return value;
    }
}
