namespace Chronos.Domain;

public class Client
{
    public Client(string nom)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nom);

        Nom = nom.Trim();
    }

    public int Id { get; private set; }
    public string Nom { get; private set; }
}
