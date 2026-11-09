using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Tasks;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Tasks.Validators;
using TaskManager.Domain.Tasks;
using TaskManager.UnitTests.TestDoubles;

namespace TaskManager.UnitTests.Application;

public sealed class TaskServiceTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITaskQueries _taskQueries = Substitute.For<ITaskQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedTimeProvider _timeProvider = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(
            _taskRepository,
            _userRepository,
            _taskQueries,
            _unitOfWork,
            new CreateTaskRequestValidator(new TaskAdditionalInfoDtoValidator()),
            new UpdateTaskStatusRequestValidator(),
            new AssignTaskRequestValidator(),
            new TaskQueryParametersValidator(),
            _timeProvider,
            NullLogger<TaskService>.Instance);

        _taskQueries.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => SampleResponse(call.Arg<int>()));
    }

    [Fact]
    public async Task CreateAsync_DatosValidos_PersisteLaTareaEnPending()
    {
        _userRepository.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        var request = new CreateTaskRequest(
            "Nueva tarea",
            "Descripción",
            1,
            new TaskAdditionalInfoDto(TaskPriority.High, new DateOnly(2026, 12, 1), ["api"], null));

        await _sut.CreateAsync(request);

        _taskRepository.Received(1).Add(Arg.Is<TaskItem>(task =>
            task.Title == "Nueva tarea" &&
            task.UserId == 1 &&
            task.Status == TaskItemStatus.Pending &&
            task.AdditionalInfo!.Priority == TaskPriority.High &&
            task.CreatedAt == _timeProvider.GetUtcNow().UtcDateTime));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SinTitulo_LanzaValidationExceptionSinPersistir()
    {
        var request = new CreateTaskRequest("   ", null, 1, null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateTaskRequest.Title));
        _taskRepository.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task CreateAsync_SinUsuarioAsignado_LanzaValidationException()
    {
        var request = new CreateTaskRequest("Tarea", null, 0, null);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateTaskRequest.UserId));
    }

    [Fact]
    public async Task CreateAsync_UsuarioInexistente_LanzaValidationExceptionSobreUserId()
    {
        _userRepository.ExistsAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CreateAsync(new CreateTaskRequest("Tarea", null, 99, null)));

        Assert.Single(exception.Errors, error => error.PropertyName == nameof(CreateTaskRequest.UserId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ChangeStatusAsync_TransicionValida_GuardaCambios()
    {
        var task = TaskItem.Create("Tarea", null, 1, null, DateTime.UtcNow);
        _taskRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(task);

        await _sut.ChangeStatusAsync(10, new UpdateTaskStatusRequest(TaskItemStatus.InProgress));

        Assert.Equal(TaskItemStatus.InProgress, task.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatusAsync_PendingADone_LanzaInvalidStatusTransitionSinGuardar()
    {
        var task = TaskItem.Create("Tarea", null, 1, null, DateTime.UtcNow);
        _taskRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(task);

        await Assert.ThrowsAsync<InvalidStatusTransitionException>(
            () => _sut.ChangeStatusAsync(10, new UpdateTaskStatusRequest(TaskItemStatus.Done)));

        Assert.Equal(TaskItemStatus.Pending, task.Status);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ChangeStatusAsync_MismoEstado_NoGuardaCambios()
    {
        var task = TaskItem.Create("Tarea", null, 1, null, DateTime.UtcNow);
        _taskRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(task);

        await _sut.ChangeStatusAsync(10, new UpdateTaskStatusRequest(TaskItemStatus.Pending));

        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ChangeStatusAsync_TareaInexistente_LanzaNotFoundException()
    {
        _taskRepository.GetByIdAsync(404, Arg.Any<CancellationToken>()).Returns((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.ChangeStatusAsync(404, new UpdateTaskStatusRequest(TaskItemStatus.InProgress)));
    }

    [Fact]
    public async Task ChangeStatusAsync_SinEstado_LanzaValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.ChangeStatusAsync(1, new UpdateTaskStatusRequest(null)));
    }

    [Fact]
    public async Task AssignAsync_UsuarioExistente_ReasignaYGuarda()
    {
        var task = TaskItem.Create("Tarea", null, 1, null, DateTime.UtcNow);
        _taskRepository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(task);
        _userRepository.ExistsAsync(2, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.AssignAsync(10, new AssignTaskRequest(2));

        Assert.Equal(2, task.UserId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_TamanoDePaginaFueraDeRango_LanzaValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.SearchAsync(new TaskQueryParameters { PageSize = TaskQueryParameters.MaxPageSize + 1 }));

        await _taskQueries.DidNotReceiveWithAnyArgs().SearchAsync(default!, default);
    }

    [Fact]
    public async Task GetByIdAsync_TareaInexistente_LanzaNotFoundException()
    {
        _taskQueries.GetByIdAsync(404, Arg.Any<CancellationToken>()).Returns((TaskResponse?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(404));
    }

    private static TaskResponse SampleResponse(int id) => new(
        id,
        "Tarea",
        null,
        TaskItemStatus.Pending,
        new AssignedUserDto(1, "Ana", "ana@empresa.com"),
        null,
        [TaskItemStatus.InProgress],
        DateTime.UtcNow,
        null);
}
