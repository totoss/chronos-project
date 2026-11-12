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

    [Fact]
    public void Renommer_remplace_le_nom_sans_espaces_superflus()
    {
        var client = new Client("Banque du Littoral");

        client.Renommer("  Banque de l'Ouest ");

        Assert.Equal("Banque de l'Ouest", client.Nom);
    }

    [Fact]
    public void Renommer_avec_un_nom_vide_est_refuse()
    {
        var client = new Client("Banque du Littoral");

        Assert.Throws<ArgumentException>(() => client.Renommer("  "));
    }
}
