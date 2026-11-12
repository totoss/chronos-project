namespace Chronos.Domain;

public sealed class CalendrierDeTravail(IEnumerable<DateOnly> joursFeries)
{
    private readonly HashSet<DateOnly> _joursFeries = [.. joursFeries];

    public bool EstOuvre(DateOnly jour) =>
        jour.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_joursFeries.Contains(jour);

    public IEnumerable<DateOnly> JoursOuvres(Periode periode)
    {
        for (var jour = periode.Debut; jour <= periode.Fin; jour = jour.AddDays(1))
        {
            if (EstOuvre(jour))
            {
                yield return jour;
            }
        }
    }

    public int NombreDeJoursOuvres(Periode periode) => JoursOuvres(periode).Count();
}
