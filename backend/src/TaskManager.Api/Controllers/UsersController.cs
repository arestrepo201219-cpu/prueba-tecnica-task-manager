using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Users;
using TaskManager.Application.Users.Dtos;

namespace TaskManager.Api.Controllers;

/// <summary>
/// Gestiona las operaciones relacionadas con los usuarios del sistema
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Registra un nuevo usuario para que pueda participar en la gestión y asignación de tareas
    /// </summary>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>
    /// Consulta los usuarios registrados en el sistema y devuelve el listado ordenado por nombre
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await userService.ListAsync(cancellationToken));

    /// <summary>
    /// Recupera la información de un usuario específico utilizando su identificador
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await userService.GetByIdAsync(id, cancellationToken));
}
