using System.Linq.Expressions;
using Chronos.Api.Commun;
using Chronos.Domain;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Projets;

public static class ProjetsEndpoints
{
    public static RouteGroupBuilder MapProjets(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/projets").WithTags("Projets");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);
        groupe.MapPost("/", Creer);

        return groupe;
    }

    private static readonly Expression<Func<Projet, ProjetDto>> VersDto = p => new ProjetDto(
        p.Id, p.Code, p.Libelle, p.Nature,
        p.ClientId, p.Client!.Nom,
        p.DateDebut, p.DateFin, p.BudgetJours,
        p.ResponsableId, p.Responsable!.Prenom + " " + p.Responsable.Nom, p.Statut);

    private static async Task<Ok<PageDto<ProjetDto>>> Lister(
        ChronosDbContext db, CancellationToken cancellationToken,
        int page = 1, int taille = 20, StatutProjet? statut = null)
    {
        var requete = db.Projets.IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives]);
        if (statut is not null)
        {
            requete = requete.Where(p => p.Statut == statut);
        }

        var total = await requete.CountAsync(cancellationToken);

        var elements = await requete
            .OrderBy(p => p.Code)
            .Skip((page - 1) * taille)
            .Take(taille)
            .Select(VersDto)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PageDto<ProjetDto>(elements, page, taille, total));
    }

    private static async Task<Results<Ok<ProjetDto>, NotFound>> Obtenir(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var projet = await db.Projets
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .Where(p => p.Id == id)
            .Select(VersDto)
            .FirstOrDefaultAsync(cancellationToken);

        return projet is null ? TypedResults.NotFound() : TypedResults.Ok(projet);
    }

    private static async Task<Results<Created<ProjetDto>, Conflict>> Creer(
        CreerProjetRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Projets.AnyAsync(p => p.Code == requete.Code, cancellationToken))
        {
            return TypedResults.Conflict();
        }

        var projet = new Projet(requete.Code, requete.Libelle, requete.Nature, requete.ClientId,
            requete.DateDebut, requete.DateFin, requete.BudgetJours, requete.ResponsableId);

        db.Projets.Add(projet);
        await db.SaveChangesAsync(cancellationToken);

        var dto = await db.Projets
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .Where(p => p.Id == projet.Id)
            .Select(VersDto)
            .SingleAsync(cancellationToken);
        return TypedResults.Created($"/api/projets/{dto.Id}", dto);
    }
}
