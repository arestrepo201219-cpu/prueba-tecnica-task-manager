using FluentValidation;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Tasks.Validators;

public sealed class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotNull().WithMessage("El estado es obligatorio.")
            .IsInEnum().WithMessage("El estado debe ser Pending, InProgress o Done.");
    }
}
