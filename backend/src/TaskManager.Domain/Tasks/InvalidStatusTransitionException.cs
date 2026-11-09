using TaskManager.Domain.Common;

namespace TaskManager.Domain.Tasks;

public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(TaskItemStatus from, TaskItemStatus to)
        : base(BuildMessage(from, to), "invalid_status_transition")
    {
        From = from;
        To = to;
    }

    public InvalidStatusTransitionException()
    {
    }

    public InvalidStatusTransitionException(string message)
        : base(message, "invalid_status_transition")
    {
    }

    public InvalidStatusTransitionException(string message, Exception innerException)
        : base(message, "invalid_status_transition", innerException)
    {
    }

    public TaskItemStatus From { get; }

    public TaskItemStatus To { get; }

    private static string BuildMessage(TaskItemStatus from, TaskItemStatus to)
    {
        var allowed = TaskStatusTransitions.AllowedFrom(from);
        var allowedText = allowed.Count == 0 ? "ninguno" : string.Join(", ", allowed);
        return $"No se permite cambiar una tarea de '{from}' a '{to}'. Estados permitidos desde '{from}': {allowedText}.";
    }
}
