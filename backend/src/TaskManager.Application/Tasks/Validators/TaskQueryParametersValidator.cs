using FluentValidation;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Validators;

public sealed class TaskQueryParametersValidator : AbstractValidator<TaskQueryParameters>
{
    public TaskQueryParametersValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue)
            .WithMessage("El usuario debe ser un identificador válido.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("El estado debe ser Pending, InProgress o Done.");
        RuleFor(x => x.Priority).IsInEnum().WithMessage("La prioridad debe ser Low, Medium o High.");
        RuleFor(x => x.Tag).MaximumLength(TaskAdditionalInfo.TagMaxLength)
            .WithMessage($"La etiqueta admite como máximo {TaskAdditionalInfo.TagMaxLength} caracteres.");
        RuleFor(x => x.Sort).IsInEnum().WithMessage("El orden debe ser Asc o Desc.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("La página debe ser mayor o igual a 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, TaskQueryParameters.MaxPageSize)
            .WithMessage($"El tamaño de página debe estar entre 1 y {TaskQueryParameters.MaxPageSize}.");
    }
}
