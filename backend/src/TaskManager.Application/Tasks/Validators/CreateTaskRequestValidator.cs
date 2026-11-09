using FluentValidation;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Validators;

public sealed class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    public CreateTaskRequestValidator(IValidator<TaskAdditionalInfoDto> additionalInfoValidator)
    {
        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title)).WithMessage("El título de la tarea es obligatorio.")
            .MaximumLength(TaskItem.TitleMaxLength)
            .WithMessage($"El título no puede superar {TaskItem.TitleMaxLength} caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.DescriptionMaxLength)
            .WithMessage($"La descripción no puede superar {TaskItem.DescriptionMaxLength} caracteres.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("La tarea debe tener un usuario asignado.");

        RuleFor(x => x.AdditionalInfo!)
            .SetValidator(additionalInfoValidator)
            .When(x => x.AdditionalInfo is not null);
    }
}
