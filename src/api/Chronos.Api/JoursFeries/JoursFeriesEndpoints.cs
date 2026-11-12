using Chronos.Api.Commun;
using Chronos.Api.Securite;
using Chronos.Domain;
using Chronos.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.JoursFeries;

public static class JoursFeriesEndpoints
{
    public static RouteGroupBuilder MapJoursFeries(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/jours-feries").WithTags("Jours fériés");

        groupe.MapGet("/", Lister);
        // Le calendrier est tenu par le gestionnaire RH (cahier des charges § 2).
        groupe.MapPost("/", Creer).RequireAuthorization(Politiques.GestionRh);
        groupe.MapDelete("/{date}", Supprimer).RequireAuthorization(Politiques.GestionRh);

        return groupe;
    }

    private static async Task<Ok<List<JourFerieDto>>> Lister(ChronosDbContext db, CancellationToken cancellationToken)
    {
        var joursFeries = await db.JoursFeries
            .OrderBy(j => j.Date)
            .Select(j => new JourFerieDto(j.Date, j.Libelle))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(joursFeries);
    }

    private static async Task<Results<Created<JourFerieDto>, ProblemHttpResult>> Creer(
        CreerJourFerieRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var date = requete.Date!.Value;

        // La clé primaire protège aussi de deux demandes simultanées : le ConflitExceptionHandler répond 409.
        if (await db.JoursFeries.AnyAsync(j => j.Date == date, cancellationToken))
        {
            return Conflit($"Le {date:dd/MM/yyyy} est déjà un jour férié.");
        }

        var jourFerie = new JourFerie(date, requete.Libelle.Trim());
        db.JoursFeries.Add(jourFerie);
        await db.SaveChangesAsync(cancellationToken);

        // La date, au format ISO, est la clé de la ressource dans l'URL : /api/jours-feries/2026-05-08.
        return TypedResults.Created($"/api/jours-feries/{date:yyyy-MM-dd}", new JourFerieDto(date, jourFerie.Libelle));
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> Supprimer(
        string date, ChronosDbContext db, CancellationToken cancellationToken)
    {
        if (!DatesIso.TryLire(date, out var jour))
        {
            return Invalide("Date", "Format attendu : aaaa-mm-jj.");
        }

        var jourFerie = await db.JoursFeries.FirstOrDefaultAsync(j => j.Date == jour, cancellationToken);
        if (jourFerie is null)
        {
            return TypedResults.NotFound();
        }

        db.JoursFeries.Remove(jourFerie);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
