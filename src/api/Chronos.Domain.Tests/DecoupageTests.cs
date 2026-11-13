namespace Chronos.Domain.Tests;

public class DecoupageTests
{
    [Fact]
    public void Du_6_au_19_mai_le_decoupage_par_semaine_rend_trois_tranches()
    {
        var tranches = Decoupage.ParSemaine(new Periode(new DateOnly(2026, 5, 6), new DateOnly(2026, 5, 19)));

        Assert.Equal(
        [
            new Periode(new DateOnly(2026, 5, 6), new DateOnly(2026, 5, 10)),
            new Periode(new DateOnly(2026, 5, 11), new DateOnly(2026, 5, 17)),
            new Periode(new DateOnly(2026, 5, 18), new DateOnly(2026, 5, 19)),
        ], tranches);
    }

    [Fact]
    public void Le_decoupage_par_mois_traverse_fevrier()
    {
        var tranches = Decoupage.ParMois(new Periode(new DateOnly(2028, 1, 20), new DateOnly(2028, 3, 5)));

        Assert.Equal(
        [
            new Periode(new DateOnly(2028, 1, 20), new DateOnly(2028, 1, 31)),
            new Periode(new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29)),   // 2028 est bissextile
            new Periode(new DateOnly(2028, 3, 1), new DateOnly(2028, 3, 5)),
        ], tranches);
    }

    [Fact]
    public void Une_periode_dans_une_seule_tranche_n_est_pas_decoupee()
    {
        var periode = new Periode(new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 8));

        Assert.Equal([periode], Decoupage.ParSemaine(periode));
    }
}
