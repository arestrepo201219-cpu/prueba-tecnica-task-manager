using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Tasks;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Guarda los cambios. Si SQL Server devuelve un error conocido (duplicado, llave foránea,
/// el trigger de estados), lo convierte en una excepción de la aplicación para responder
/// con el código HTTP correcto
/// </summary>
internal sealed class UnitOfWork(TaskManagerDbContext dbContext) : IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const int ForeignKeyOrCheckViolation = 547;
    private const int InvalidStatusTransition = 50001;   // el error que lanza el trigger de estados

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (SqlErrorNumber(exception) is UniqueIndexViolation or UniqueConstraintViolation)
        {
            throw new ConflictException("Ya existe un registro con los mismos datos únicos.", exception);
        }
        catch (DbUpdateException exception) when (SqlErrorNumber(exception) is ForeignKeyOrCheckViolation)
        {
            throw new ConflictException("La operación viola una restricción de integridad de la base de datos.", exception);
        }
        catch (DbUpdateException exception) when (SqlErrorNumber(exception) is InvalidStatusTransition)
        {
            throw new InvalidStatusTransitionException(exception.InnerException!.Message, exception);
        }
    }

    private static int? SqlErrorNumber(DbUpdateException exception) =>
        (exception.InnerException as SqlException)?.Number;
}
