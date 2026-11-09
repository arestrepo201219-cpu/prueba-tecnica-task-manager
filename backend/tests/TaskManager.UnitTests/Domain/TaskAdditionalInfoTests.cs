using TaskManager.Domain.Common;
using TaskManager.Domain.Tasks;

namespace TaskManager.UnitTests.Domain;

public sealed class TaskAdditionalInfoTests
{
    [Fact]
    public void Create_NormalizaEtiquetas_SinVaciosNiDuplicados()
    {
        var info = TaskAdditionalInfo.Create(TaskPriority.High, null, [" api ", "API", "", "backend"], null);

        Assert.Equal(["api", "backend"], info.Tags);
    }

    [Fact]
    public void Create_MasEtiquetasDelMaximo_LanzaDomainException()
    {
        var tags = Enumerable.Range(1, TaskAdditionalInfo.MaxTags + 1).Select(i => $"tag{i}");

        Assert.Throws<DomainException>(() => TaskAdditionalInfo.Create(null, null, tags, null));
    }

    [Fact]
    public void Create_MetadatoConClaveVacia_LanzaDomainException()
    {
        var metadata = new Dictionary<string, string> { [" "] = "valor" };

        Assert.Throws<DomainException>(() => TaskAdditionalInfo.Create(null, null, null, metadata));
    }

    [Fact]
    public void IsEmpty_SinDatos_EsVerdadero()
    {
        Assert.True(TaskAdditionalInfo.Create(null, null, null, null).IsEmpty);
    }
}
