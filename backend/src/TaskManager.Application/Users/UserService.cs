using FluentValidation;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Users.Dtos;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Users;

internal sealed class UserService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateUserRequest> createValidator,
    TimeProvider timeProvider) : IUserService
{
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var normalizedEmail = User.NormalizeEmail(request.Email);
        if (await userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException($"Ya existe un usuario con el correo '{normalizedEmail}'.");
        }

        var user = User.Create(request.Name, request.Email, timeProvider.GetUtcNow().UtcDateTime);
        userRepository.Add(user);

        // Si dos personas crean el mismo correo al mismo tiempo, las dos pasan la validación de
        // arriba,n ese caso el índice único de la base lo frena y UnitOfWork lo devuelve como 409
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToResponse();
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        return [.. users.Select(user => user.ToResponse())];
    }

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException("Usuario", id);

        return user.ToResponse();
    }
}
