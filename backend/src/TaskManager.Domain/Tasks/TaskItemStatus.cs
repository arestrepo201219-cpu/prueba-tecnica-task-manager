namespace TaskManager.Domain.Tasks;

/// <summary>
/// Estados de una tarea, los números son los mismos Id de la tabla dbo.TaskStatuses
/// Es byte porque en SQL Server la columna es TINYINT
/// </summary>
public enum TaskItemStatus : byte
{
    Pending = 1,
    InProgress = 2,
    Done = 3,
}
