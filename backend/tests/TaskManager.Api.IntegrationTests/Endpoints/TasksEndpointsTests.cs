using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Domain.Tasks;

namespace TaskManager.Api.IntegrationTests.Endpoints;

public sealed class TasksEndpointsTests(TaskManagerApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task PostTasks_DatosValidos_Responde201EnPendingConInformacionJson()
    {
        var user = await CreateUserAsync();
        var info = new TaskAdditionalInfoDto(
            TaskPriority.High,
            new DateOnly(2026, 12, 1),
            ["api", "backend"],
            new Dictionary<string, string> { ["sprint"] = "S-24" });

        var response = await Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("  Crear endpoint  ", null, user.Id, info), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
        Assert.Equal("Crear endpoint", task.Title);
        Assert.Equal(TaskItemStatus.Pending, task.Status);
        Assert.Equal(user.Id, task.AssignedUser.Id);
        Assert.Equal([TaskItemStatus.InProgress], task.AllowedTransitions);
        Assert.Equal(TaskPriority.High, task.AdditionalInfo!.Priority);
        Assert.Equal("S-24", task.AdditionalInfo.Metadata!["sprint"]);
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task PostTasks_EnumsSeSerializanComoTexto()
    {
        var user = await CreateUserAsync();
        var created = await CreateTaskAsync(user.Id);

        var json = await Client.GetStringAsync(new Uri($"/api/tasks/{created.Id}", UriKind.Relative));

        Assert.Contains("\"status\":\"Pending\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostTasks_SinTitulo_Responde400ConErrorEnTitle()
    {
        var user = await CreateUserAsync();

        var response = await Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("", null, user.Id, null), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadProblemAsync(response)).GetProperty("errors");
        Assert.Equal("El título de la tarea es obligatorio.", errors.GetProperty("title")[0].GetString());
    }

    [Fact]
    public async Task PostTasks_SinUsuario_Responde400ConErrorEnUserId()
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Tarea", null, 0, null), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadProblemAsync(response)).GetProperty("errors").TryGetProperty("userId", out _));
    }

    [Fact]
    public async Task PostTasks_UsuarioInexistente_Responde400ConErrorEnUserId()
    {
        var response = await Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Tarea", null, 999999, null), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadProblemAsync(response)).GetProperty("errors").TryGetProperty("userId", out _));
    }

    [Fact]
    public async Task PostTasks_PrioridadInvalidaEnJson_Responde400()
    {
        var user = await CreateUserAsync();
        using var content = new StringContent(
            $$$"""{"title":"Tarea","userId":{{{user.Id}}},"additionalInfo":{"priority":"Urgentisima"}}""",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync(new Uri("/api/tasks", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutStatus_PendingADone_Responde422()
    {
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(user.Id);

        var response = await Client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.Done), Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("invalid_status_transition", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PutStatus_PendingAInProgressADone_Responde200()
    {
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(user.Id);

        var toInProgress = await Client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.InProgress), Json);
        var toDone = await Client.PutAsJsonAsync($"/api/tasks/{task.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.Done), Json);

        Assert.Equal(HttpStatusCode.OK, toInProgress.StatusCode);
        Assert.Equal(HttpStatusCode.OK, toDone.StatusCode);
        var updated = (await toDone.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
        Assert.Equal(TaskItemStatus.Done, updated.Status);
        Assert.NotNull(updated.UpdatedAt);
        Assert.Equal([TaskItemStatus.InProgress], updated.AllowedTransitions);
    }

    [Fact]
    public async Task PutStatus_EstadoDesconocido_Responde400()
    {
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(user.Id);
        using var content = new StringContent("""{"status":"Archived"}""", System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PutAsync(new Uri($"/api/tasks/{task.Id}/status", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutStatus_SinEstado_Responde400ConErrorEnStatus()
    {
        var user = await CreateUserAsync();
        var task = await CreateTaskAsync(user.Id);
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        var response = await Client.PutAsync(new Uri($"/api/tasks/{task.Id}/status", UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadProblemAsync(response)).GetProperty("errors").TryGetProperty("status", out _));
    }

    [Fact]
    public async Task PutStatus_TareaInexistente_Responde404()
    {
        var response = await Client.PutAsJsonAsync("/api/tasks/999999/status", new UpdateTaskStatusRequest(TaskItemStatus.InProgress), Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutAssignee_UsuarioValido_ReasignaLaTarea()
    {
        var owner = await CreateUserAsync();
        var newOwner = await CreateUserAsync();
        var task = await CreateTaskAsync(owner.Id);

        var response = await Client.PutAsJsonAsync($"/api/tasks/{task.Id}/assignee", new AssignTaskRequest(newOwner.Id), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(newOwner.Id, (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!.AssignedUser.Id);
    }

    [Fact]
    public async Task GetTasks_FiltroPorUsuarioYEstado_DevuelveSoloCoincidencias()
    {
        var user = await CreateUserAsync();
        var pending = await CreateTaskAsync(user.Id);
        var inProgress = await CreateTaskAsync(user.Id);
        await Client.PutAsJsonAsync($"/api/tasks/{inProgress.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.InProgress), Json);

        var result = await Client.GetFromJsonAsync<PagedResult<TaskResponse>>($"/api/tasks?userId={user.Id}&status=Pending", Json);

        Assert.Equal(1, result!.TotalCount);
        Assert.Equal(pending.Id, Assert.Single(result.Items).Id);
    }

    [Fact]
    public async Task GetTasks_FiltroPorPrioridadYEtiquetaDelJson_DevuelveSoloCoincidencias()
    {
        var user = await CreateUserAsync();
        var match = await CreateTaskAsync(user.Id, new TaskAdditionalInfoDto(TaskPriority.High, null, ["urgente-integracion"], null));
        await CreateTaskAsync(user.Id, new TaskAdditionalInfoDto(TaskPriority.Low, null, ["urgente-integracion"], null));

        var result = await Client.GetFromJsonAsync<PagedResult<TaskResponse>>(
            $"/api/tasks?userId={user.Id}&priority=High&tag=urgente-integracion", Json);

        Assert.Equal(match.Id, Assert.Single(result!.Items).Id);
    }

    [Fact]
    public async Task GetTasks_OrdenAscendente_OrdenaPorFechaDeCreacion()
    {
        var user = await CreateUserAsync();
        var first = await CreateTaskAsync(user.Id);
        var second = await CreateTaskAsync(user.Id);

        var desc = await Client.GetFromJsonAsync<PagedResult<TaskResponse>>($"/api/tasks?userId={user.Id}", Json);
        var asc = await Client.GetFromJsonAsync<PagedResult<TaskResponse>>($"/api/tasks?userId={user.Id}&sort=Asc", Json);

        Assert.Equal([second.Id, first.Id], desc!.Items.Select(task => task.Id));
        Assert.Equal([second.Id, first.Id], asc!.Items.Select(task => task.Id).Reverse());
    }

    [Fact]
    public async Task GetTasks_TamanoDePaginaInvalido_Responde400()
    {
        var response = await Client.GetAsync(new Uri("/api/tasks?pageSize=500", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadProblemAsync(response)).GetProperty("errors").TryGetProperty("pageSize", out _));
    }
}
