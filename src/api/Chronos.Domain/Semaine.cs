namespace Chronos.Domain;

public static class Semaine
{
    public static Periode Contenant(DateOnly jour)
    {
        var joursDepuisLundi = ((int)jour.DayOfWeek + 6) % 7;
        var lundi = jour.AddDays(-joursDepuisLundi);
        return new Periode(lundi, lundi.AddDays(6));
    }
}

public static class Horloge
{
    public static DateOnly Aujourdhui(this TimeProvider horloge) =>
        DateOnly.FromDateTime(horloge.GetLocalNow().DateTime);
}

public static class Decoupage
{
    public static IEnumerable<Periode> ParSemaine(Periode periode) =>
        Decouper(periode, debut => Semaine.Contenant(debut).Fin);

    public static IEnumerable<Periode> ParMois(Periode periode) =>
        Decouper(periode, debut => new DateOnly(debut.Year, debut.Month, DateTime.DaysInMonth(debut.Year, debut.Month)));

    private static IEnumerable<Periode> Decouper(Periode periode, Func<DateOnly, DateOnly> finDeTranche)
    {
        var debut = periode.Debut;
        while (debut <= periode.Fin)
        {
            var fin = finDeTranche(debut);
            if (fin > periode.Fin)
            {
                fin = periode.Fin;
            }

            yield return new Periode(debut, fin);
            debut = fin.AddDays(1);
        }
    }
}
