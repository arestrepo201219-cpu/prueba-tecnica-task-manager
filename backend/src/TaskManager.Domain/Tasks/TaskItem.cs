using TaskManager.Domain.Common;

namespace TaskManager.Domain.Tasks;

/// <summary>
/// Tarea asignada a un usuario, se llama TaskItem y no Task para no confundirla con
/// System.Threading.Tasks.Task. En la base de datos es la tabla dbo.Tasks
/// Los datos solo se cambian con los métodos de la clase, así nadie se salta las reglas
/// </summary>
public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    // EF Core necesita este constructor vacío para poder crear la entidad al leer de la base
    private TaskItem()
    {
    }

    private TaskItem(
        string title,
        string? description,
        int userId,
        TaskAdditionalInfo? additionalInfo,
        DateTime createdAt)
    {
        Title = title;
        Description = description;
        UserId = userId;
        AdditionalInfo = additionalInfo;
        Status = TaskItemStatus.Pending;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    /// <summary>usuario responsable, es obligatorio</summary>
    public int UserId { get; private set; }

    public TaskAdditionalInfo? AdditionalInfo { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Estados a los que se puede mover la tarea desde el estado en que está</summary>
    public IReadOnlyList<TaskItemStatus> AllowedNextStatuses => TaskStatusTransitions.AllowedFrom(Status);

    /// <summary>crea una tarea nueva Siempre arranca en Pending</summary>
    public static TaskItem Create(
        string title,
        string? description,
        int userId,
        TaskAdditionalInfo? additionalInfo,
        DateTime utcNow)
    {
        return new TaskItem(
            Guard.NotBlank(title, nameof(Title), TitleMaxLength),
            Guard.Optional(description, nameof(Description), DescriptionMaxLength),
            Guard.PositiveId(userId, nameof(UserId)),
            additionalInfo is { IsEmpty: true } ? null : additionalInfo,
            utcNow);
    }

    /// <summary>Asigna la tarea a otro usuario</summary>
    public void AssignTo(int userId, DateTime utcNow)
    {
        Guard.PositiveId(userId, nameof(UserId));

        if (UserId == userId)
        {
            return;
        }

        UserId = userId;
        UpdatedAt = utcNow;
    }

    /// <summary>
    /// Cambia el estado si el cambio está permitido, si se pide el mismo estado en el que
    /// ya está, no hace nada y no lanza error
    /// </summary>
    /// <returns>true si el estado cambió</returns>
    /// <exception cref="InvalidStatusTransitionException">Cuando el cambio no está permitido</exception>
    public bool ChangeStatus(TaskItemStatus newStatus, DateTime utcNow)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new DomainException("El estado indicado no es válido.", "invalid_status");
        }

        if (Status == newStatus)
        {
            return false;
        }

        if (!TaskStatusTransitions.CanTransition(Status, newStatus))
        {
            throw new InvalidStatusTransitionException(Status, newStatus);
        }

        Status = newStatus;
        UpdatedAt = utcNow;
        return true;
    }
}
