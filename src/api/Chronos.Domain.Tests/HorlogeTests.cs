using Microsoft.Extensions.Time.Testing;

namespace Chronos.Domain.Tests;

public class HorlogeTests
{
    [Fact]
    public void La_semaine_en_cours_depend_de_l_horloge()
    {
        var horloge = new FakeTimeProvider(new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 5, 4), Semaine.Contenant(horloge.Aujourdhui()).Debut);

        horloge.Advance(TimeSpan.FromDays(7));

        Assert.Equal(new DateOnly(2026, 5, 11), Semaine.Contenant(horloge.Aujourdhui()).Debut);
    }

    [Fact]
    public void Aujourdhui_est_la_date_locale_et_non_la_date_UTC()
    {
        // Dimanche 10 mai 2026, 22 h 30 UTC : il est déjà minuit trente à Paris, donc lundi 11.
        var horloge = new FakeTimeProvider(new DateTimeOffset(2026, 5, 10, 22, 30, 0, TimeSpan.Zero));
        horloge.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));

        Assert.Equal(new DateOnly(2026, 5, 11), horloge.Aujourdhui());
    }

    [Theory]
    [InlineData(2026, 5, 4, 2026, 5, 4)]   // lundi
    [InlineData(2026, 5, 10, 2026, 5, 4)]  // dimanche
    [InlineData(2026, 5, 11, 2026, 5, 11)] // lundi suivant
    public void La_semaine_commence_le_lundi_et_finit_le_dimanche(
        int annee, int mois, int jour, int anneeLundi, int moisLundi, int jourLundi)
    {
        var semaine = Semaine.Contenant(new DateOnly(annee, mois, jour));

        Assert.Equal(new DateOnly(anneeLundi, moisLundi, jourLundi), semaine.Debut);
        Assert.Equal(new DateOnly(anneeLundi, moisLundi, jourLundi).AddDays(6), semaine.Fin);
    }
}
