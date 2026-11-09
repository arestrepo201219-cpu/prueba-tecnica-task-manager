using TaskManager.Domain.Common;
using TaskManager.Domain.Tasks;

namespace TaskManager.UnitTests.Domain;

public sealed class TaskItemTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 15, 0, 0, DateTimeKind.Utc);

    private static TaskItem NewTask(TaskItemStatus status = TaskItemStatus.Pending)
    {
        var task = TaskItem.Create("Tarea", null, userId: 1, additionalInfo: null, Now);
        if (status == TaskItemStatus.InProgress || status == TaskItemStatus.Done)
        {
            task.ChangeStatus(TaskItemStatus.InProgress, Now);
        }

        if (status == TaskItemStatus.Done)
        {
            task.ChangeStatus(TaskItemStatus.Done, Now);
        }

        return task;
    }

    [Fact]
    public void Create_ConDatosValidos_IniciaEnPendingYNormalizaTextos()
    {
        var task = TaskItem.Create("  Configurar CI  ", "   ", userId: 7, additionalInfo: null, Now);

        Assert.Equal("Configurar CI", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(7, task.UserId);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Null(task.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_SinTitulo_LanzaDomainException(string? title)
    {
        var exception = Assert.Throws<DomainException>(() => TaskItem.Create(title!, null, 1, null, Now));

        Assert.Equal("required_field", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_SinUsuarioAsignado_LanzaDomainException(int userId)
    {
        Assert.Throws<DomainException>(() => TaskItem.Create("Tarea", null, userId, null, Now));
    }

    [Fact]
    public void Create_TituloMuyLargo_LanzaDomainException()
    {
        var title = new string('x', TaskItem.TitleMaxLength + 1);

        Assert.Throws<DomainException>(() => TaskItem.Create(title, null, 1, null, Now));
    }

    [Fact]
    public void Create_ConInformacionAdicionalVacia_LaDescarta()
    {
        var emptyInfo = TaskAdditionalInfo.Create(null, null, [], null);

        var task = TaskItem.Create("Tarea", null, 1, emptyInfo, Now);

        Assert.Null(task.AdditionalInfo);
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Pending)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]
    public void ChangeStatus_TransicionPermitida_CambiaEstadoYFechaDeActualizacion(TaskItemStatus from, TaskItemStatus to)
    {
        var task = NewTask(from);
        var later = Now.AddHours(1);

        var changed = task.ChangeStatus(to, later);

        Assert.True(changed);
        Assert.Equal(to, task.Status);
        Assert.Equal(later, task.UpdatedAt);
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, TaskItemStatus.Done)]   // la regla del enunciado
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Pending)]
    public void ChangeStatus_TransicionNoPermitida_LanzaExcepcionYNoModificaLaTarea(TaskItemStatus from, TaskItemStatus to)
    {
        var task = NewTask(from);
        var updatedAtBefore = task.UpdatedAt;

        var exception = Assert.Throws<InvalidStatusTransitionException>(() => task.ChangeStatus(to, Now.AddHours(1)));

        Assert.Equal(from, exception.From);
        Assert.Equal(to, exception.To);
        Assert.Equal("invalid_status_transition", exception.Code);
        Assert.Equal(from, task.Status);
        Assert.Equal(updatedAtBefore, task.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_PendingADone_MensajeIndicaLosEstadosPermitidos()
    {
        var task = NewTask();

        var exception = Assert.Throws<InvalidStatusTransitionException>(() => task.ChangeStatus(TaskItemStatus.Done, Now));

        Assert.Contains("InProgress", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangeStatus_MismoEstado_EsIdempotente()
    {
        var task = NewTask();

        var changed = task.ChangeStatus(TaskItemStatus.Pending, Now.AddHours(1));

        Assert.False(changed);
        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_ValorFueraDelEnum_LanzaDomainException()
    {
        var task = NewTask();

        Assert.Throws<DomainException>(() => task.ChangeStatus((TaskItemStatus)99, Now));
    }

    [Theory]
    [InlineData(TaskItemStatus.Pending, new[] { TaskItemStatus.InProgress })]
    [InlineData(TaskItemStatus.InProgress, new[] { TaskItemStatus.Pending, TaskItemStatus.Done })]
    [InlineData(TaskItemStatus.Done, new[] { TaskItemStatus.InProgress })]
    public void AllowedNextStatuses_DevuelveLasTransicionesDelEstadoActual(TaskItemStatus status, TaskItemStatus[] expected)
    {
        var task = NewTask(status);

        Assert.Equal(expected, task.AllowedNextStatuses);
    }

    [Fact]
    public void AssignTo_OtroUsuario_ReasignaYActualizaFecha()
    {
        var task = NewTask();

        task.AssignTo(2, Now.AddHours(2));

        Assert.Equal(2, task.UserId);
        Assert.Equal(Now.AddHours(2), task.UpdatedAt);
    }

    [Fact]
    public void AssignTo_MismoUsuario_NoModificaLaTarea()
    {
        var task = NewTask();

        task.AssignTo(1, Now.AddHours(2));

        Assert.Null(task.UpdatedAt);
    }

    [Fact]
    public void AssignTo_UsuarioInvalido_LanzaDomainException()
    {
        var task = NewTask();

        Assert.Throws<DomainException>(() => task.AssignTo(0, Now));
    }
}
