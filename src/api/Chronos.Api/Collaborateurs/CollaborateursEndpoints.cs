using System.Linq.Expressions;
using Chronos.Api.Commun;
using Chronos.Domain;
using Chronos.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Collaborateurs;

public static class CollaborateursEndpoints
{
    public static RouteGroupBuilder MapCollaborateurs(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/collaborateurs").WithTags("Collaborateurs");

        groupe.MapGet("/", Lister);
        groupe.MapDelete("/{id:int}", Archiver);

        return groupe;
    }

    private static readonly Expression<Func<Collaborateur, CollaborateurDto>> VersDto = c => new CollaborateurDto(
        c.Id, c.Matricule, c.Nom, c.Prenom, c.Email,
        c.Categorie, c.Poste,
        c.ManagerId, c.Manager == null ? null : c.Manager.Prenom + " " + c.Manager.Nom,
        c.DateEntree, c.DateSortie,
        c.Contrats
            .Where(k => k.DateFin == null)
            .Select(k => k.Quotite)
            .FirstOrDefault());

    private static async Task<Ok<PageDto<CollaborateurDto>>> Lister(
        ChronosDbContext db, CancellationToken cancellationToken,
        int page = 1, int taille = 20, string? recherche = null)
    {
        var requete = db.Collaborateurs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(recherche))
        {
            // Minuscules des deux côtés : la seule écriture qui donne le même résultat sur les deux SGBD.
            var terme = recherche.Trim().ToLowerInvariant();
            requete = requete.Where(c => c.Nom.ToLower().Contains(terme) || c.Prenom.ToLower().Contains(terme));
        }

        var total = await requete.CountAsync(cancellationToken);

        var elements = await requete
            .OrderBy(c => c.Nom)
            .ThenBy(c => c.Prenom)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * taille)
            .Take(taille)
            .Select(VersDto)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PageDto<CollaborateurDto>(elements, page, taille, total));
    }

    private static async Task<Results<NoContent, NotFound>> Archiver(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var collaborateur = await db.Collaborateurs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (collaborateur is null)
        {
            return TypedResults.NotFound();
        }

        collaborateur.Archiver();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
