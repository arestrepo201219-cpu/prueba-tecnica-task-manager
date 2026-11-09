using TaskManager.Domain.Tasks;
using TaskManager.Infrastructure.Persistence.Converters;

namespace TaskManager.Api.IntegrationTests.Persistence;

/// <summary>
/// El procedimiento de búsqueda, la columna Priority y los CHECK de la tabla leen el JSON por
/// nombre ($.priority, $.tags...). Estas pruebas avisan si alguien cambia el formato y rompe eso
/// </summary>
public sealed class TaskAdditionalInfoJsonContractTests
{
    [Fact]
    public void Serialize_GeneraElFormatoQueConsultaSqlServer()
    {
        var info = TaskAdditionalInfo.Create(
            TaskPriority.High,
            new DateOnly(2026, 12, 1),
            ["api", "backend"],
            new Dictionary<string, string> { ["sprint"] = "S-24" });

        var json = TaskAdditionalInfoJson.Serialize(info);

        Assert.Equal(
            """{"priority":"High","dueDate":"2026-12-01","tags":["api","backend"],"metadata":{"sprint":"S-24"}}""",
            json);
    }

    [Fact]
    public void Serialize_SinDatos_DevuelveNull()
    {
        Assert.Null(TaskAdditionalInfoJson.Serialize(TaskAdditionalInfo.Create(null, null, null, null)));
    }

    [Fact]
    public void Deserialize_EsTolerante_ConPropiedadesDesconocidasYTiposInesperados()
    {
        const string json = """{"priority":"high","tags":["a",1,"b"],"metadata":{"points":5,"owner":"Ana"},"extra":true}""";

        var info = TaskAdditionalInfoJson.Deserialize(json)!;

        Assert.Equal(TaskPriority.High, info.Priority);
        Assert.Equal(["a", "b"], info.Tags);
        Assert.Equal("5", info.Metadata["points"]);
        Assert.Equal("Ana", info.Metadata["owner"]);
    }

    [Fact]
    public void SerializeYDeserialize_SonSimetricos()
    {
        var original = TaskAdditionalInfo.Create(TaskPriority.Low, new DateOnly(2027, 1, 15), ["x"], null);

        var roundTrip = TaskAdditionalInfoJson.Deserialize(TaskAdditionalInfoJson.Serialize(original));

        Assert.Equal(TaskAdditionalInfoJson.Serialize(original), TaskAdditionalInfoJson.Serialize(roundTrip));
    }
}
