using System.Net;
using System.Net.Http.Json;
using TaskManager.Api.IntegrationTests.Infrastructure;
using TaskManager.Application.Users.Dtos;

namespace TaskManager.Api.IntegrationTests.Endpoints;

public sealed class UsersEndpointsTests(TaskManagerApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task PostUsers_DatosValidos_Responde201ConLocation()
    {
        var response = await Client.PostAsJsonAsync("/api/users", new CreateUserRequest("Ana Gómez", "Ana.Nueva@Empresa.com"), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(Json);
        Assert.Equal("ana.nueva@empresa.com", user!.Email);
        Assert.Equal($"/api/users/{user.Id}", response.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task PostUsers_CorreoInvalido_Responde400ConErrorEnCampoEmail()
    {
        var response = await Client.PostAsJsonAsync("/api/users", new CreateUserRequest("Ana", "no-es-correo"), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal("validation_error", problem.GetProperty("code").GetString());
        Assert.True(problem.GetProperty("errors").TryGetProperty("email", out _));
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task PostUsers_CorreoDuplicado_Responde409()
    {
        await CreateUserAsync("duplicado@empresa.com");

        var response = await Client.PostAsJsonAsync("/api/users", new CreateUserRequest("Otro", "DUPLICADO@empresa.com"), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("conflict", (await ReadProblemAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetUsers_Responde200ConListado()
    {
        var created = await CreateUserAsync();

        var users = await Client.GetFromJsonAsync<List<UserResponse>>("/api/users", Json);

        Assert.Contains(users!, user => user.Id == created.Id);
    }

    [Fact]
    public async Task GetUserById_Inexistente_Responde404()
    {
        var response = await Client.GetAsync(new Uri("/api/users/999999", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("not_found", (await ReadProblemAsync(response)).GetProperty("code").GetString());
    }
}
