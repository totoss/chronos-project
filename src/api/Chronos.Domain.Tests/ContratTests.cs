namespace Chronos.Domain.Tests;

public class ContratTests
{
    private static readonly DateOnly Debut = new(2026, 1, 5);

    [Theory]
    [InlineData(0.10)]
    [InlineData(0.80)]
    [InlineData(1)]
    public void Une_quotite_comprise_entre_10_et_100_pour_cent_est_acceptee(decimal quotite)
    {
        var contrat = new Contrat(TypeContrat.Cdi, quotite, Debut);

        Assert.Equal(quotite, contrat.Quotite);
    }

    [Theory]
    [InlineData(0.09)]
    [InlineData(0)]
    [InlineData(1.01)]
    [InlineData(-0.5)]
    public void Une_quotite_hors_des_bornes_est_refusee(decimal quotite)
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            new Contrat(TypeContrat.Cdi, quotite, Debut));

        Assert.Equal("RG-03", exception.CodeRegle);
    }

    [Fact]
    public void Un_nouveau_contrat_n_a_pas_de_date_de_fin()
    {
        var contrat = new Contrat(TypeContrat.Cdd, 1m, Debut);

        Assert.Null(contrat.DateFin);
    }

    [Fact]
    public void Un_contrat_est_actif_de_son_debut_jusqu_a_sa_fin_comprise()
    {
        var collaborateur = new Collaborateur("M0002", "Durand", "Camille", "camille.durand@chronos.example",
            CategorieCollaborateur.Consultant, "Développeuse", Debut);
        collaborateur.AjouterContrat(TypeContrat.Cdi, 1m, Debut);
        collaborateur.AjouterContrat(TypeContrat.Cdi, 0.8m, new DateOnly(2026, 7, 1));
        var premier = collaborateur.Contrats.First();

        Assert.False(premier.EstActifAu(Debut.AddDays(-1)));
        Assert.True(premier.EstActifAu(Debut));
        Assert.True(premier.EstActifAu(new DateOnly(2026, 6, 30)));
        Assert.False(premier.EstActifAu(new DateOnly(2026, 7, 1)));
    }
}
