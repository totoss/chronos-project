namespace Chronos.Domain;

public class Client : ITracable
{
    public Client(string nom)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nom);

        Nom = nom.Trim();
    }

    public int Id { get; private set; }
    public string Nom { get; private set; }

    public DateTimeOffset CreeLe { get; private set; }
    public string CreePar { get; private set; } = string.Empty;
    public DateTimeOffset? ModifieLe { get; private set; }
    public string? ModifiePar { get; private set; }

    public void Renommer(string nom)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nom);

        Nom = nom.Trim();
    }
}
