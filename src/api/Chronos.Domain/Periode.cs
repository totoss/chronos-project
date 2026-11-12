namespace Chronos.Domain;

public readonly record struct Periode
{
    public Periode(DateOnly debut, DateOnly fin)
    {
        if (fin < debut)
        {
            throw new ArgumentException("La fin d'une période ne précède pas son début.", nameof(fin));
        }

        Debut = debut;
        Fin = fin;
    }

    public DateOnly Debut { get; }
    public DateOnly Fin { get; }

    public int NombreDeJours => Fin.DayNumber - Debut.DayNumber + 1;

    public bool Contient(DateOnly jour) => Debut <= jour && jour <= Fin;

    public bool Chevauche(Periode autre) => Debut <= autre.Fin && autre.Debut <= Fin;

    public Periode? Intersection(Periode autre) =>
        Chevauche(autre)
            ? new Periode(Debut > autre.Debut ? Debut : autre.Debut, Fin < autre.Fin ? Fin : autre.Fin)
            : null;
}
