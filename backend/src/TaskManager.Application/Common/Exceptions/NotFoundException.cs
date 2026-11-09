namespace TaskManager.Application.Common.Exceptions;

/// <summary>Cuando no se encuentra el registro buscado. La API la devuelve como 404.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} con id '{key}' no existe.")
    {
        ResourceName = resourceName;
        Key = key;
    }

    public NotFoundException()
    {
        ResourceName = string.Empty;
        Key = string.Empty;
    }

    public NotFoundException(string message)
        : base(message)
    {
        ResourceName = string.Empty;
        Key = string.Empty;
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
        ResourceName = string.Empty;
        Key = string.Empty;
    }

    public string ResourceName { get; }

    public object Key { get; }
}
