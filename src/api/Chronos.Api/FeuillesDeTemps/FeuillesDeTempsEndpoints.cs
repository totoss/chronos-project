using Chronos.Api.Commun;
using Chronos.Domain;
using Chronos.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.FeuillesDeTemps;

public static class FeuillesDeTempsEndpoints
{
    public static RouteGroupBuilder MapFeuillesDeTemps(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/feuilles-de-temps").WithTags("Feuilles de temps");

        groupe.MapGet("/", ObtenirOuCreer);
        groupe.MapGet("/soumises", ListerSoumises);
        groupe.MapPut("/{id:int}/saisies", RemplacerSaisies);
        groupe.MapPost("/{id:int}/soumission", Soumettre);
        groupe.MapPost("/{id:int}/validation", Valider);
        groupe.MapPost("/{id:int}/rejet", Rejeter);

        return groupe;
    }

    // Un GET qui écrit n'est pas orthodoxe : l'écran de saisie demande « la feuille de la semaine » sans savoir
    // si elle existe. La feuille absente est créée à l'état de brouillon, vide.
    private static async Task<Results<Ok<FeuilleDeTempsDto>, ValidationProblem>> ObtenirOuCreer(
        int? collaborateurId, string? semaine, ChronosDbContext db, CancellationToken cancellationToken)
    {
        if (collaborateurId is null)
        {
            return Invalide("CollaborateurId", "Le collaborateur est obligatoire.");
        }
        if (!DatesIso.TryLire(semaine, out var lundi))
        {
            return Invalide("Semaine", "Format attendu : aaaa-mm-jj.");
        }
        if (lundi.DayOfWeek != DayOfWeek.Monday)
        {
            return Invalide("Semaine", "Une feuille de temps commence un lundi.");
        }
        if (!await db.Collaborateurs.AnyAsync(c => c.Id == collaborateurId, cancellationToken))
        {
            return Invalide("CollaborateurId", "Aucun collaborateur ne porte cet identifiant.");
        }

        var feuille = await db.FeuillesDeTemps
            .Include(f => f.Saisies)
            .FirstOrDefaultAsync(f => f.CollaborateurId == collaborateurId && f.Semaine == lundi, cancellationToken);
        if (feuille is null)
        {
            feuille = new FeuilleDeTemps(collaborateurId.Value, lundi);
            db.FeuillesDeTemps.Add(feuille);
            await db.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
    }

    // Les feuilles à valider. Un GET qui ne crée rien ; la route littérale l'emporte sur « /{id} » (qui est de toute façon un entier).
    private static async Task<Ok<List<FeuilleDeTempsDto>>> ListerSoumises(
        ChronosDbContext db, CancellationToken cancellationToken)
    {
        var feuilles = await db.FeuillesDeTemps
            .AsNoTracking()
            .Include(f => f.Saisies)
            .Where(f => f.Statut == StatutFeuilleDeTemps.Soumise)
            .OrderBy(f => f.Semaine).ThenBy(f => f.CollaborateurId)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(feuilles.ConvertAll(FeuilleDeTempsDto.Depuis));
    }

    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound, ValidationProblem>> RemplacerSaisies(
        int id, RemplacerSaisiesRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var feuille = await db.FeuillesDeTemps
            .Include(f => f.Saisies)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (feuille is null)
        {
            return TypedResults.NotFound();
        }

        var demandes = requete.Saisies
            .Select(s => new SaisieDemandee(s.ProjetId!.Value, s.Jour!.Value, s.Duree!.Value))
            .ToList();

        if (demandes.Any(d => !feuille.Periode.Contient(d.Jour)))
        {
            return Invalide("Saisies", "Toutes les saisies doivent porter sur la semaine de la feuille.");
        }

        var idsProjets = demandes.Select(d => d.ProjetId).Distinct().ToList();
        var projets = await db.Projets.Where(p => idsProjets.Contains(p.Id)).ToListAsync(cancellationToken);
        if (projets.Count != idsProjets.Count)
        {
            return Invalide("Saisies", "Au moins une saisie porte sur un projet inconnu.");
        }

        // RG-13 : pas de saisie sur un projet terminé ou annulé.
        foreach (var projet in projets)
        {
            projet.VerifierOuvert();
        }

        var contexte = await ChargerContexte(db, feuille.CollaborateurId, feuille.Periode, cancellationToken);

        feuille.RemplacerSaisies(demandes, contexte);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
    }

    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound>> Soumettre(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var feuille = await db.FeuillesDeTemps
            .Include(f => f.Saisies)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (feuille is null)
        {
            return TypedResults.NotFound();
        }

        feuille.Soumettre();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
    }

    // RG-43, vue du côté de la feuille : valider du temps sur lequel une absence a été approuvée entre-temps
    // serait aussi une violation. La validation LIT donc les absences approuvées, dans une transaction
    // sérialisable : si une approbation s'enregistre en même temps, la base fait échouer l'une des deux.
    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound, ValidationProblem>> Valider(
        int id, ValiderFeuilleRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        if (!await ValideurExiste(db, requete.ValideurId!.Value, cancellationToken))
        {
            return Invalide("ValideurId", "Aucun collaborateur ne porte cet identifiant.");
        }

        var feuille = await db.EnTransactionSerialisableAsync(async () =>
        {
            var feuille = await db.FeuillesDeTemps
                .Include(f => f.Saisies)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (feuille is null)
            {
                return null;
            }

            feuille.Valider(requete.ValideurId!.Value);

            var absences = await db.Absences
                .Where(a => a.CollaborateurId == feuille.CollaborateurId && a.Statut == StatutAbsence.Approuvee
                    && a.DateDebut <= feuille.Periode.Fin && a.DateFin >= feuille.Periode.Debut)
                .ToListAsync(cancellationToken);
            Absence.VerifierTempsValide(feuille.TempsParJour(), Absence.PartsAbsentes(absences, feuille.Periode));

            await db.SaveChangesAsync(cancellationToken);
            return feuille;
        }, cancellationToken);

        return feuille is null ? TypedResults.NotFound() : TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
    }

    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound, ValidationProblem>> Rejeter(
        int id, RejeterFeuilleRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        if (!await ValideurExiste(db, requete.ValideurId!.Value, cancellationToken))
        {
            return Invalide("ValideurId", "Aucun collaborateur ne porte cet identifiant.");
        }

        var feuille = await db.FeuillesDeTemps
            .Include(f => f.Saisies)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (feuille is null)
        {
            return TypedResults.NotFound();
        }

        feuille.Rejeter(requete.ValideurId!.Value, requete.Motif);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
    }

    private static Task<bool> ValideurExiste(ChronosDbContext db, int valideurId, CancellationToken cancellationToken) =>
        db.Collaborateurs.AnyAsync(c => c.Id == valideurId, cancellationToken);

    // Ce que la feuille ne sait pas d'elle-même : le calendrier, les affectations et les absences approuvées
    // du collaborateur, bornés à la semaine.
    private static async Task<ContexteDeSaisie> ChargerContexte(
        ChronosDbContext db, int collaborateurId, Periode periode, CancellationToken cancellationToken)
    {
        var joursFeries = await db.JoursFeries
            .Where(j => j.Date >= periode.Debut && j.Date <= periode.Fin)
            .Select(j => j.Date)
            .ToListAsync(cancellationToken);

        var affectations = await db.Affectations
            .Where(a => a.CollaborateurId == collaborateurId && a.DateDebut <= periode.Fin && a.DateFin >= periode.Debut)
            .Select(a => new { a.ProjetId, a.DateDebut, a.DateFin, a.TauxAllocation, a.Projet!.Nature })
            .ToListAsync(cancellationToken);

        var absences = await db.Absences
            .AsNoTracking()
            .Where(a => a.CollaborateurId == collaborateurId && a.Statut == StatutAbsence.Approuvee
                && a.DateDebut <= periode.Fin && a.DateFin >= periode.Debut)
            .ToListAsync(cancellationToken);

        return new ContexteDeSaisie(
            new CalendrierDeTravail(joursFeries),
            Absence.PartsAbsentes(absences, periode),
            [.. affectations.Select(a =>
                new AffectationPlanifiee(a.ProjetId, new Periode(a.DateDebut, a.DateFin), a.TauxAllocation,
                    a.Nature == NatureProjet.Facturable))]);
    }
}
