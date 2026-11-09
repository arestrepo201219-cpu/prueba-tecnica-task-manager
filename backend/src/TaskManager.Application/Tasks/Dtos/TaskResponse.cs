using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Dtos;

public sealed record TaskResponse(
    int Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    AssignedUserDto AssignedUser,
    TaskAdditionalInfoDto? AdditionalInfo,
    IReadOnlyList<TaskItemStatus> AllowedTransitions,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record AssignedUserDto(int Id, string Name, string Email);
