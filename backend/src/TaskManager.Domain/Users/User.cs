using TaskManager.Domain.Common;

namespace TaskManager.Domain.Users;

/// <summary>Persona de la empresa a la que se le asignan tareas</summary>
public sealed class User
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 256;

    // EF Core necesita este constructor vacío para poder crear la entidad al leer de la base
    private User()
    {
    }

    private User(string name, string email, DateTime createdAt)
    {
        Name = name;
        Email = email;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Se guarda sin espacios y en minúsculas</summary>
    public string Email { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static User Create(string name, string email, DateTime utcNow)
    {
        var normalizedName = Guard.NotBlank(name, nameof(Name), NameMaxLength);
        var normalizedEmail = NormalizeEmail(email);

        if (!normalizedEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new DomainException("El correo electrónico no tiene un formato válido.", "invalid_email");
        }

        return new User(normalizedName, normalizedEmail, utcNow);
    }

    /// <summary>Deja el correo como se guarda. También se usa para buscar duplicados</summary>
    public static string NormalizeEmail(string? email) =>
        Guard.NotBlank(email, nameof(Email), EmailMaxLength).ToLowerInvariant();
}
