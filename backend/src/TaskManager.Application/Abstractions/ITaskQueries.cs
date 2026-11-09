using TaskManager.Application.Common.Models;
using TaskManager.Application.Tasks.Dtos;

namespace TaskManager.Application.Abstractions;

/// <summary>
/// Gestiona las consultas de tareas y devuelve los DTOs preparados para el consumo de la API,
/// evitando exponer directamente las entidades del dominio
/// Las consultas se apoyan en el procedimiento almacenado dbo.usp_Tasks_Search
/// </summary>
public interface ITaskQueries
{
    Task<PagedResult<TaskResponse>> SearchAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<TaskResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
