using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions;
using TaskManager.Domain.Users;

namespace TaskManager.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(TaskManagerDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(user => user.Id == id, cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
