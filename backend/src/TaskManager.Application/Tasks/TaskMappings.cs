using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks;

public static class TaskMappings
{
    public static TaskAdditionalInfo? ToDomain(this TaskAdditionalInfoDto? dto) =>
        dto is null ? null : TaskAdditionalInfo.Create(dto.Priority, dto.DueDate, dto.Tags, dto.Metadata);

    public static TaskAdditionalInfoDto? ToDto(this TaskAdditionalInfo? info) =>
        info is null || info.IsEmpty
            ? null
            : new TaskAdditionalInfoDto(info.Priority, info.DueDate, info.Tags, info.Metadata);
}
