using TaskManager.Domain.Common;
using TaskManager.Domain.Users;

namespace TaskManager.UnitTests.Domain;

public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_NormalizaNombreYCorreo()
    {
        var user = User.Create("  Ana Gómez ", "  Ana.Gomez@Empresa.COM ", Now);

        Assert.Equal("Ana Gómez", user.Name);
        Assert.Equal("ana.gomez@empresa.com", user.Email);
        Assert.Equal(Now, user.CreatedAt);
    }

    [Theory]
    [InlineData("", "ana@empresa.com")]
    [InlineData("Ana", "")]
    [InlineData("Ana", "sin-arroba")]
    public void Create_DatosInvalidos_LanzaDomainException(string name, string email)
    {
        Assert.Throws<DomainException>(() => User.Create(name, email, Now));
    }
}
