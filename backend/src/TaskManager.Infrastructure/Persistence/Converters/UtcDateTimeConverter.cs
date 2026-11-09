using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskManager.Infrastructure.Persistence.Converters;

/// <summary>
/// Las fechas se guardan en UTC, pero DATETIME2 no guarda la zona horaria. Al leerlas se
/// marcan como UTC para que el JSON las envíe con la "Z" al final y el navegador las muestre
/// en la hora local
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
