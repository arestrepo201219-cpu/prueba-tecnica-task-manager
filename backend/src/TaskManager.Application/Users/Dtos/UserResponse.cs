namespace TaskManager.Application.Users.Dtos;

public sealed record UserResponse(int Id, string Name, string Email, DateTime CreatedAt);
