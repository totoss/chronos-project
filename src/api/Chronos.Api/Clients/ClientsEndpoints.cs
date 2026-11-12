using Microsoft.AspNetCore.Http.HttpResults;

namespace Chronos.Api.Clients;

public static class ClientsEndpoints
{
    public static RouteGroupBuilder MapClients(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/clients").WithTags("Clients");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);
        groupe.MapPost("/", Creer);

        return groupe;
    }

    private static Ok<IReadOnlyList<ClientDto>> Lister(ClientsEnMemoire clients) =>
        TypedResults.Ok(clients.Lister());

    private static Results<Ok<ClientDto>, NotFound> Obtenir(int id, ClientsEnMemoire clients) =>
        clients.Trouver(id) is { } client
            ? TypedResults.Ok(client)
            : TypedResults.NotFound();

    private static Created<ClientDto> Creer(CreerClientRequete requete, ClientsEnMemoire clients)
    {
        var client = clients.Ajouter(requete);
        return TypedResults.Created($"/api/clients/{client.Id}", client);
    }
}
