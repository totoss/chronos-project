namespace Chronos.Api.Clients;

public class ClientsEnMemoire
{
    private readonly Lock _verrou = new();
    private readonly List<ClientDto> _clients =
    [
        new(1, "ACME"),
        new(2, "Globex"),
    ];
    private int _prochainId = 3;

    public IReadOnlyList<ClientDto> Lister()
    {
        lock (_verrou)
        {
            return [.. _clients];
        }
    }

    public ClientDto? Trouver(int id)
    {
        lock (_verrou)
        {
            return _clients.Find(c => c.Id == id);
        }
    }

    public ClientDto Ajouter(CreerClientRequete requete)
    {
        lock (_verrou)
        {
            var client = new ClientDto(_prochainId++, requete.Nom);
            _clients.Add(client);
            return client;
        }
    }
}
