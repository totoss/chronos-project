namespace Chronos.Domain;

public class JourFerie(DateOnly date, string libelle)
{
    public DateOnly Date { get; private set; } = date;
    public string Libelle { get; private set; } = libelle;
}
