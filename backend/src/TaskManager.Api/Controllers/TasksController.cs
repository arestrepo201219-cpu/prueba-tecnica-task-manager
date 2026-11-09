using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Api.Controllers;

/// <summary>
/// Centraliza las operaciones disponibles para la gestión de tareas
/// </summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    /// <summary>
    /// Registra una nueva tarea y la crea inicialmente en estado Pending
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    /// <summary>
    /// Consulta las tareas registradas permitiendo aplicar filtros por usuario, estado,
    /// prioridad y etiqueta, además de definir el orden de los resultados según su fecha de creación
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TaskResponse>>> Search(
        [FromQuery] TaskQueryParameters parameters,
        CancellationToken cancellationToken) =>
        Ok(await taskService.SearchAsync(parameters, cancellationToken));

    /// <summary>
    /// Busca una tarea específica a partir de su identificador
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await taskService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Actualiza el estado de una tarea teniendo en cuenta las transiciones permitidas
    /// Si el cambio solicitado no cumple las reglas del flujo de trabajo, se rechaza la operación
    /// </summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskResponse>> ChangeStatus(
        int id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await taskService.ChangeStatusAsync(id, request, cancellationToken));

    /// <summary>
    /// Modifica el responsable de una tarea asignándola a otro usuario
    /// </summary>
    [HttpPut("{id:int}/assignee")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Assign(
        int id,
        AssignTaskRequest request,
        CancellationToken cancellationToken) =>
        Ok(await taskService.AssignAsync(id, request, cancellationToken));
}
