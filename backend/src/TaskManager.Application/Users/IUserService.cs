using TaskManager.Application.Users.Dtos;

namespace TaskManager.Application.Users;

public interface IUserService
{
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
