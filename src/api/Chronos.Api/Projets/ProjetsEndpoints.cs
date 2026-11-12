using Microsoft.AspNetCore.Http.HttpResults;

namespace Chronos.Api.Projets;

public static class ProjetsEndpoints
{
    public static RouteGroupBuilder MapProjets(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/projets").WithTags("Projets");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);
        groupe.MapPost("/", Creer);
        groupe.MapDelete("/{id:int}", Supprimer);

        return groupe;
    }

    private static Ok<IReadOnlyList<ProjetDto>> Lister(ProjetsEnMemoire projets, StatutProjet? statut = null) =>
        TypedResults.Ok(projets.Lister(statut));

    private static Results<Ok<ProjetDto>, NotFound> Obtenir(int id, ProjetsEnMemoire projets) =>
        projets.Trouver(id) is { } projet
            ? TypedResults.Ok(projet)
            : TypedResults.NotFound();

    private static Created<ProjetDto> Creer(CreerProjetRequete requete, ProjetsEnMemoire projets)
    {
        var projet = projets.Ajouter(requete);
        return TypedResults.Created($"/api/projets/{projet.Id}", projet);
    }

    private static Results<NoContent, NotFound> Supprimer(int id, ProjetsEnMemoire projets) =>
        projets.Supprimer(id)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
}
