namespace TaskManager.Domain.Common;

/// <summary>
/// Se lanza cuando se incumple una regla de negocio. La API la devuelve como 422
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message, string code = "domain_rule_violation")
        : base(message) => Code = code;

    public DomainException()
    {
        Code = "domain_rule_violation";
    }

    public DomainException(string message, Exception innerException)
        : this(message, "domain_rule_violation", innerException)
    {
    }

    protected DomainException(string message, string code, Exception? innerException)
        : base(message, innerException) => Code = code;

    /// <summary>Código corto del error, para que el front pueda identificarlo sin leer el mensaje</summary>
    public string Code { get; }
}
