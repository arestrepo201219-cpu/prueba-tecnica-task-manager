using FluentValidation;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Tasks.Validators;

public sealed class AssignTaskRequestValidator : AbstractValidator<AssignTaskRequest>
{
    public AssignTaskRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("La tarea debe tener un usuario asignado.");
    }
}
