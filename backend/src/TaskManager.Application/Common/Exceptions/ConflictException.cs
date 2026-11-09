namespace TaskManager.Application.Common.Exceptions;

/// <summary>
/// Cuando lo que se intenta hacer choca con datos que ya existen, como un correo repetido.
/// La API la devuelve como 409.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException()
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
