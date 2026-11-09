using TaskManager.Domain.Users;

namespace TaskManager.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>El correo debe llegar ya normalizado con User.NormalizeEmail</summary>
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    void Add(User user);
}
