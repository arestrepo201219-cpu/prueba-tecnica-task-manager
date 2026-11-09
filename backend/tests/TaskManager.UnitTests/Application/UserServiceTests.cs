using FluentValidation;
using NSubstitute;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Users;
using TaskManager.Application.Users.Dtos;
using TaskManager.Application.Users.Validators;
using TaskManager.Domain.Users;
using TaskManager.UnitTests.TestDoubles;

namespace TaskManager.UnitTests.Application;

public sealed class UserServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepository, _unitOfWork, new CreateUserRequestValidator(), new FixedTimeProvider());
    }

    [Fact]
    public async Task CreateAsync_DatosValidos_PersisteUsuarioConCorreoNormalizado()
    {
        var response = await _sut.CreateAsync(new CreateUserRequest("Ana Gómez", "Ana.Gomez@Empresa.com"));

        Assert.Equal("ana.gomez@empresa.com", response.Email);
        _userRepository.Received(1).Add(Arg.Is<User>(user => user.Email == "ana.gomez@empresa.com"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_CorreoDuplicado_LanzaConflictException()
    {
        _userRepository.EmailExistsAsync("ana@empresa.com", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.CreateAsync(new CreateUserRequest("Ana", " ANA@empresa.com ")));

        _userRepository.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Theory]
    [InlineData("", "ana@empresa.com", "Name")]
    [InlineData("Ana", "", "Email")]
    [InlineData("Ana", "correo-invalido", "Email")]
    [InlineData("Ana", "ana@empresa", "Email")]
    public async Task CreateAsync_DatosInvalidos_LanzaValidationException(string name, string email, string expectedProperty)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(new CreateUserRequest(name, email)));

        Assert.Contains(exception.Errors, error => error.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task GetByIdAsync_UsuarioInexistente_LanzaNotFoundException()
    {
        _userRepository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(5));
    }
}
