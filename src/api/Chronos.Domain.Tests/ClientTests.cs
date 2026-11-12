namespace Chronos.Domain.Tests;

public class ClientTests
{
    [Fact]
    public void Un_client_porte_son_nom_sans_espaces_superflus()
    {
        var client = new Client("  Banque du Littoral ");

        Assert.Equal("Banque du Littoral", client.Nom);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_client_sans_nom_est_refuse(string nom)
    {
        Assert.Throws<ArgumentException>(() => new Client(nom));
    }
}
