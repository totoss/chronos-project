using System.Linq.Expressions;
using Chronos.Domain;
using Chronos.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.Clients;

public static class ClientsEndpoints
{
    public static RouteGroupBuilder MapClients(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/clients").WithTags("Clients");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);
        groupe.MapPost("/", Creer);
        groupe.MapPut("/{id:int}", Modifier);
        groupe.MapDelete("/{id:int}", Supprimer);

        return groupe;
    }

    private static Expression<Func<Client, ClientDto>> VersDto(ChronosDbContext db) => c => new ClientDto(
        c.Id, c.Nom,
        db.Projets.Count(p => p.ClientId == c.Id && p.Statut == StatutProjet.EnCours));

    private static async Task<Ok<List<ClientDto>>> Lister(ChronosDbContext db, CancellationToken cancellationToken)
    {
        var clients = await db.Clients
            .OrderBy(c => c.Nom)
            .ThenBy(c => c.Id)
            .Select(VersDto(db))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(clients);
    }

    private static async Task<Results<Ok<ClientDto>, NotFound>> Obtenir(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var client = await db.Clients
            .Where(c => c.Id == id)
            .Select(VersDto(db))
            .FirstOrDefaultAsync(cancellationToken);

        return client is null ? TypedResults.NotFound() : TypedResults.Ok(client);
    }

    private static async Task<Created<ClientDto>> Creer(
        CreerClientRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var client = new Client(requete.Nom);

        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken);

        var dto = new ClientDto(client.Id, client.Nom, ProjetsEnCours: 0);
        return TypedResults.Created($"/api/clients/{dto.Id}", dto);
    }

    private static async Task<Results<Ok<ClientDto>, NotFound>> Modifier(
        int id, ModifierClientRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client is null)
        {
            return TypedResults.NotFound();
        }

        client.Renommer(requete.Nom);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(await db.Clients
            .Where(c => c.Id == id).Select(VersDto(db)).SingleAsync(cancellationToken));
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Supprimer(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client is null)
        {
            return TypedResults.NotFound();
        }

        // La clé étrangère refuserait de toute façon (RESTRICT) ; on répond avant, et par un 409 plutôt qu'une 500.
        if (await db.Projets.AnyAsync(p => p.ClientId == id, cancellationToken))
        {
            return Conflit("Ce client a des projets : il ne peut pas être supprimé.");
        }

        db.Clients.Remove(client);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
