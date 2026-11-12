namespace Chronos.Domain.Tests;

public class CalendrierDeTravailTests
{
    private static readonly CalendrierDeTravail Calendrier = new(
    [
        new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 8), new DateOnly(2026, 5, 14),
    ]);

    public static IEnumerable<TheoryDataRow<DateOnly, DateOnly, int>> Periodes =>
    [
        new(new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 10), 4),   // semaine avec un férié
        new(new DateOnly(2026, 5, 18), new DateOnly(2026, 5, 24), 5),  // semaine complète
        new(new DateOnly(2026, 5, 9), new DateOnly(2026, 5, 10), 0),   // week-end
        new(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), 18),  // mai 2026
    ];

    [Theory]
    [MemberData(nameof(Periodes))]
    public void Nombre_de_jours_ouvres(DateOnly debut, DateOnly fin, int attendu)
    {
        Assert.Equal(attendu, Calendrier.NombreDeJoursOuvres(new Periode(debut, fin)));
    }
}
