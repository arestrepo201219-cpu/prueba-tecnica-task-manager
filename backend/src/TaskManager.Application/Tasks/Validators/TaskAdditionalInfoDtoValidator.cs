using FluentValidation;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Tasks;

namespace TaskManager.Application.Tasks.Validators;

public sealed class TaskAdditionalInfoDtoValidator : AbstractValidator<TaskAdditionalInfoDto>
{
    public TaskAdditionalInfoDtoValidator()
    {
        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("La prioridad debe ser Low, Medium o High.");

        RuleFor(x => x.Tags)
            .Must(tags => tags!.Count <= TaskAdditionalInfo.MaxTags)
            .WithMessage($"Se permiten como máximo {TaskAdditionalInfo.MaxTags} etiquetas.")
            .When(x => x.Tags is not null);

        RuleForEach(x => x.Tags)
            .Must(tag => !string.IsNullOrWhiteSpace(tag)).WithMessage("Las etiquetas no pueden estar vacías.")
            .MaximumLength(TaskAdditionalInfo.TagMaxLength)
            .WithMessage($"Cada etiqueta admite como máximo {TaskAdditionalInfo.TagMaxLength} caracteres.");

        RuleFor(x => x.Metadata)
            .Must(metadata => metadata!.Count <= TaskAdditionalInfo.MaxMetadataEntries)
            .WithMessage($"Se permiten como máximo {TaskAdditionalInfo.MaxMetadataEntries} metadatos.")
            .Must(metadata => metadata!.Keys.All(key =>
                !string.IsNullOrWhiteSpace(key) && key.Trim().Length <= TaskAdditionalInfo.MetadataKeyMaxLength))
            .WithMessage($"Las claves de metadatos son obligatorias y admiten como máximo {TaskAdditionalInfo.MetadataKeyMaxLength} caracteres.")
            .Must(metadata => metadata!.Values.All(value => (value?.Length ?? 0) <= TaskAdditionalInfo.MetadataValueMaxLength))
            .WithMessage($"Los valores de metadatos admiten como máximo {TaskAdditionalInfo.MetadataValueMaxLength} caracteres.")
            .When(x => x.Metadata is not null);
    }
}
