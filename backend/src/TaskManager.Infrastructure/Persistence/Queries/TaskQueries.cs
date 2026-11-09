using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Tasks;
using TaskManager.Infrastructure.Persistence.Converters;

namespace TaskManager.Infrastructure.Persistence.Queries;

/// <summary>
/// Consulta las tareas con el procedimiento dbo.usp_Tasks_Search y las convierte en DTOs
/// Los filtros se mandan como parámetros, nunca concatenados en el SQL
/// </summary>
internal sealed class TaskQueries(TaskManagerDbContext dbContext) : ITaskQueries
{
    private const string SearchProcedure =
        "EXEC dbo.usp_Tasks_Search @TaskId = @TaskId, @UserId = @UserId, @StatusId = @StatusId, " +
        "@Priority = @Priority, @Tag = @Tag, @SortAscending = @SortAscending, " +
        "@PageNumber = @PageNumber, @PageSize = @PageSize";

    public async Task<PagedResult<TaskResponse>> SearchAsync(
        TaskQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var filter = new SearchFilter(
            TaskId: null,
            parameters.UserId,
            (byte?)parameters.Status,
            parameters.Priority?.ToString(),
            string.IsNullOrWhiteSpace(parameters.Tag) ? null : parameters.Tag.Trim(),
            parameters.Sort == SortDirection.Asc);

        var rows = await ExecuteAsync(filter, parameters.Page, parameters.PageSize, cancellationToken);

        var totalCount = rows.Count > 0
            ? rows[0].TotalCount
            : await CountWhenPageIsOutOfRangeAsync(filter, parameters.Page, cancellationToken);

        return new PagedResult<TaskResponse>(
            [.. rows.Select(ToResponse)],
            parameters.Page,
            parameters.PageSize,
            totalCount);
    }

    public async Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var filter = new SearchFilter(TaskId: id, UserId: null, StatusId: null, Priority: null, Tag: null, SortAscending: false);
        var rows = await ExecuteAsync(filter, page: 1, pageSize: 1, cancellationToken);
        return rows.Count == 0 ? null : ToResponse(rows[0]);
    }

    /// <summary>
    /// Si piden una página que no existe, la consulta no trae filas y tampoco el total
    /// En ese caso se consulta la primera página solo para saber cuántas tareas hay
    /// </summary>
    private async Task<int> CountWhenPageIsOutOfRangeAsync(SearchFilter filter, int page, CancellationToken cancellationToken)
    {
        if (page <= 1)
        {
            return 0;
        }

        var firstRow = await ExecuteAsync(filter, page: 1, pageSize: 1, cancellationToken);
        return firstRow.Count > 0 ? firstRow[0].TotalCount : 0;
    }

    private Task<List<TaskSearchRow>> ExecuteAsync(
        SearchFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        SqlParameter[] sqlParameters =
        [
            Parameter("@TaskId", SqlDbType.Int, filter.TaskId),
            Parameter("@UserId", SqlDbType.Int, filter.UserId),
            Parameter("@StatusId", SqlDbType.TinyInt, filter.StatusId),
            Parameter("@Priority", SqlDbType.NVarChar, filter.Priority, size: 20),
            Parameter("@Tag", SqlDbType.NVarChar, filter.Tag, size: 50),
            Parameter("@SortAscending", SqlDbType.Bit, filter.SortAscending),
            Parameter("@PageNumber", SqlDbType.Int, page),
            Parameter("@PageSize", SqlDbType.Int, pageSize),
        ];

        return dbContext.Database
            .SqlQueryRaw<TaskSearchRow>(SearchProcedure, sqlParameters)
            .ToListAsync(cancellationToken);
    }

    private static SqlParameter Parameter(string name, SqlDbType type, object? value, int size = 0)
    {
        var parameter = new SqlParameter(name, type) { Value = value ?? DBNull.Value };
        if (size > 0)
        {
            parameter.Size = size;
        }

        return parameter;
    }

    private static TaskResponse ToResponse(TaskSearchRow row)
    {
        var status = (TaskItemStatus)row.StatusId;

        return new TaskResponse(
            row.Id,
            row.Title,
            row.Description,
            status,
            new AssignedUserDto(row.UserId, row.UserName, row.UserEmail),
            ToAdditionalInfo(row),
            TaskStatusTransitions.AllowedFrom(status),
            DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc),
            row.UpdatedAt is { } updatedAt ? DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc) : null);
    }

    private static TaskAdditionalInfoDto? ToAdditionalInfo(TaskSearchRow row)
    {
        var priority = TaskAdditionalInfoJson.ParsePriority(row.Priority);
        var tags = TaskAdditionalInfoJson.ReadTags(row.TagsJson);
        var metadata = TaskAdditionalInfoJson.ReadMetadata(row.MetadataJson);

        var isEmpty = priority is null && row.DueDate is null && tags.Count == 0 && metadata.Count == 0;
        return isEmpty ? null : new TaskAdditionalInfoDto(priority, row.DueDate, tags, metadata);
    }

    private sealed record SearchFilter(
        int? TaskId,
        int? UserId,
        byte? StatusId,
        string? Priority,
        string? Tag,
        bool SortAscending);
}
