namespace Chronos.Domain.Tests;

public class PeriodeTests
{
    private static Periode EnMai(int debut, int fin) =>
        new(new DateOnly(2026, 5, debut), new DateOnly(2026, 5, fin));

    [Theory]
    [InlineData(4, 8, 11, 15, false)]
    [InlineData(4, 8, 8, 12, true)]
    [InlineData(4, 15, 6, 7, true)]
    [InlineData(11, 15, 4, 8, false)]
    public void Deux_periodes_se_chevauchent_si_elles_ont_un_jour_commun(
        int debutA, int finA, int debutB, int finB, bool attendu)
    {
        var a = EnMai(debutA, finA);
        var b = EnMai(debutB, finB);

        Assert.Equal(attendu, a.Chevauche(b));
        Assert.Equal(attendu, b.Chevauche(a));
    }

    [Fact]
    public void Deux_periodes_de_memes_bornes_sont_egales()
    {
        Assert.Equal(EnMai(4, 8), EnMai(4, 8));
    }

    [Fact]
    public void Une_periode_a_l_envers_est_refusee()
    {
        Assert.Throws<ArgumentException>(() => EnMai(8, 4));
    }

    [Theory]
    [InlineData(4, 4, 1)]
    [InlineData(4, 8, 5)]
    [InlineData(1, 31, 31)]
    public void Le_nombre_de_jours_compte_les_deux_bornes(int debut, int fin, int attendu)
    {
        Assert.Equal(attendu, EnMai(debut, fin).NombreDeJours);
    }

    [Theory]
    [InlineData(4, 15, 6, 10, 6, 10)]
    [InlineData(4, 8, 6, 12, 6, 8)]
    [InlineData(6, 12, 4, 8, 6, 8)]
    [InlineData(4, 8, 8, 12, 8, 8)]
    [InlineData(4, 8, 4, 8, 4, 8)]
    public void L_intersection_de_deux_periodes_est_leur_partie_commune(
        int debutA, int finA, int debutB, int finB, int debutAttendu, int finAttendue)
    {
        var intersection = EnMai(debutA, finA).Intersection(EnMai(debutB, finB));

        Assert.Equal(EnMai(debutAttendu, finAttendue), intersection);
    }

    [Theory]
    [InlineData(4, 8, 9, 12)]
    [InlineData(9, 12, 4, 8)]
    public void L_intersection_de_deux_periodes_disjointes_est_absente(
        int debutA, int finA, int debutB, int finB)
    {
        var intersection = EnMai(debutA, finA).Intersection(EnMai(debutB, finB));

        Assert.Null(intersection);
    }

    [Fact]
    public void Une_periode_contient_ses_deux_bornes_et_pas_le_jour_suivant()
    {
        var periode = EnMai(4, 8);

        Assert.True(periode.Contient(new DateOnly(2026, 5, 4)));
        Assert.True(periode.Contient(new DateOnly(2026, 5, 8)));
        Assert.False(periode.Contient(new DateOnly(2026, 5, 3)));
        Assert.False(periode.Contient(new DateOnly(2026, 5, 9)));
    }
}
