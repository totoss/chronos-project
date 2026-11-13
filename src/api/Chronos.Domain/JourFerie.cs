namespace Chronos.Domain;

public class JourFerie(DateOnly date, string libelle) : ITracable
{
    public DateOnly Date { get; private set; } = date;
    public string Libelle { get; private set; } = libelle;

    public DateTimeOffset CreeLe { get; private set; }
    public string CreePar { get; private set; } = string.Empty;
    public DateTimeOffset? ModifieLe { get; private set; }
    public string? ModifiePar { get; private set; }
}
