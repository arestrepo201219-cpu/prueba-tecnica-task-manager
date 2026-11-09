using TaskManager.Application.Users.Dtos;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Users;

internal static class UserMappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Email, user.CreatedAt);
}
