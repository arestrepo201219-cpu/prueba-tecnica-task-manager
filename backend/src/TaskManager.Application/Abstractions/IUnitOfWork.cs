namespace TaskManager.Application.Abstractions;

/// <summary>Guarda en la base de datos, en una sola transacción, todo lo que se cambió</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
