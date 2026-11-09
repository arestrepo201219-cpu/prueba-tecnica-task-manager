using System.Net.Http.Json;
using System.Text.Json;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Application.Users.Dtos;

namespace TaskManager.Api.IntegrationTests.Endpoints;

public abstract class ApiTestBase : IClassFixture<TaskManagerApiFactory>
{
    protected ApiTestBase(TaskManagerApiFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Client = factory.CreateClient();
    }

    protected HttpClient Client { get; }

    protected static JsonSerializerOptions Json => TaskManagerApiFactory.JsonOptions;

    protected async Task<UserResponse> CreateUserAsync(string? email = null)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest("Usuario de prueba", email ?? $"user-{Guid.NewGuid():N}@empresa.com"),
            Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserResponse>(Json))!;
    }

    protected async Task<TaskResponse> CreateTaskAsync(int userId, TaskAdditionalInfoDto? info = null)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("Tarea de prueba", null, userId, info),
            Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }

    protected static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
