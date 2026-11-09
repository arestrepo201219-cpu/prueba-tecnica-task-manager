using System.Text.RegularExpressions;
using FluentValidation;
using TaskManager.Application.Users.Dtos;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Users.Validators;

public sealed partial class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("El nombre es obligatorio.")
            .Must(name => name is null || name.Trim().Length <= User.NameMaxLength)
            .WithMessage($"El nombre no puede superar {User.NameMaxLength} caracteres.");

        // Se valida sin los espacios de los extremos, porque así es como se guarda
        RuleFor(x => x.Email)
            .Must(email => !string.IsNullOrWhiteSpace(email)).WithMessage("El correo electrónico es obligatorio.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Email)
                    .Must(email => email.Trim().Length <= User.EmailMaxLength)
                    .WithMessage($"El correo no puede superar {User.EmailMaxLength} caracteres.")
                    .Must(email => EmailPattern().IsMatch(email.Trim()))
                    .WithMessage("El correo electrónico no tiene un formato válido.");
            });
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 250)]
    private static partial Regex EmailPattern();
}
