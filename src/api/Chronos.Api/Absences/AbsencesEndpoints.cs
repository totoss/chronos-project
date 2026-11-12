using Chronos.Domain;
using Chronos.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.Absences;

public static class AbsencesEndpoints
{
    public static RouteGroupBuilder MapAbsences(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/absences").WithTags("Absences");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);
        groupe.MapPost("/", Demander);
        groupe.MapPost("/{id:int}/approbation", Approuver);
        groupe.MapPost("/{id:int}/refus", Refuser);
        groupe.MapPost("/{id:int}/annulation", Annuler);

        return groupe;
    }

    // Une liste simple, comme celle des affectations : l'écran la demande pour un collaborateur.
    private static async Task<Ok<List<AbsenceDto>>> Lister(
        int? collaborateurId, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var absences = await db.Absences
            .AsNoTracking()
            .Where(a => collaborateurId == null || a.CollaborateurId == collaborateurId)
            .OrderBy(a => a.DateDebut)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(absences.ConvertAll(AbsenceDto.Depuis));
    }

    private static async Task<Results<Ok<AbsenceDto>, NotFound>> Obtenir(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var absence = await db.Absences.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return absence is null ? TypedResults.NotFound() : TypedResults.Ok(AbsenceDto.Depuis(absence));
    }

    // RG-41 lit les absences existantes pour décider d'en ajouter une : la lecture et l'écriture forment un tout,
    // dans une transaction sérialisable que la stratégie d'exécution rejoue si la base la fait échouer.
    private static async Task<Results<Created<AbsenceDto>, ValidationProblem>> Demander(
        CreerAbsenceRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Collaborateurs.AnyAsync(c => c.Id == requete.CollaborateurId, cancellationToken))
        {
            return Invalide("CollaborateurId", "Aucun collaborateur ne porte cet identifiant.");
        }
        if (!await db.TypesAbsence.AnyAsync(t => t.Id == requete.TypeAbsenceId, cancellationToken))
        {
            return Invalide("TypeAbsenceId", "Aucun type d'absence ne porte cet identifiant.");
        }

        var absence = await db.EnTransactionSerialisableAsync(async () =>
        {
            var demande = new Absence(requete.CollaborateurId!.Value, requete.TypeAbsenceId!.Value,
                requete.DateDebut!.Value, requete.DateFin!.Value, requete.DebuteApresMidi, requete.FinitLeMatin);

            // RG-42 : une absence doit couvrir au moins une demi-journée ouvrée.
            var joursFeries = await db.JoursFeries
                .Where(j => j.Date >= demande.DateDebut && j.Date <= demande.DateFin)
                .Select(j => j.Date)
                .ToListAsync(cancellationToken);
            demande.VerifierDuree(new CalendrierDeTravail(joursFeries));

            // Les absences voisines sont chargées par leurs dates ; la demi-journée se juge dans le domaine.
            var voisines = await db.Absences
                .AsNoTracking()
                .Where(a => a.CollaborateurId == demande.CollaborateurId
                    && (a.Statut == StatutAbsence.Demandee || a.Statut == StatutAbsence.Approuvee)
                    && a.DateDebut <= demande.DateFin && a.DateFin >= demande.DateDebut)
                .ToListAsync(cancellationToken);
            if (voisines.Any(demande.Chevauche))
            {
                throw new RegleMetierException("RG-41", "Cette absence en chevauche une autre du même collaborateur.");
            }

            db.Absences.Add(demande);
            await db.SaveChangesAsync(cancellationToken);
            return demande;
        }, cancellationToken);

        return TypedResults.Created($"/api/absences/{absence.Id}", AbsenceDto.Depuis(absence));
    }

    // RG-43 lit les feuilles de temps pour décider du sort d'une absence : même schéma que la demande.
    private static async Task<Results<Ok<AbsenceDto>, NotFound>> Approuver(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var absence = await db.EnTransactionSerialisableAsync(async () =>
        {
            var absence = await db.Absences.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (absence is null)
            {
                return null;
            }

            absence.Approuver();

            var saisiesValidees = await db.FeuillesDeTemps
                .Where(f => f.CollaborateurId == absence.CollaborateurId && f.Statut == StatutFeuilleDeTemps.Validee)
                .SelectMany(f => f.Saisies)
                .Where(s => s.Jour >= absence.DateDebut && s.Jour <= absence.DateFin)
                .Select(s => new { s.Jour, s.Duree })
                .ToListAsync(cancellationToken);
            var tempsValide = saisiesValidees
                .GroupBy(s => s.Jour)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Duree));

            // Toutes les absences approuvées du jour comptent, celle-ci comprise.
            var autres = await db.Absences
                .AsNoTracking()
                .Where(a => a.CollaborateurId == absence.CollaborateurId && a.Statut == StatutAbsence.Approuvee
                    && a.Id != absence.Id && a.DateDebut <= absence.DateFin && a.DateFin >= absence.DateDebut)
                .ToListAsync(cancellationToken);
            Absence.VerifierTempsValide(tempsValide, Absence.PartsAbsentes([.. autres, absence], absence.Periode));

            await db.SaveChangesAsync(cancellationToken);
            return absence;
        }, cancellationToken);

        return absence is null ? TypedResults.NotFound() : TypedResults.Ok(AbsenceDto.Depuis(absence));
    }

    private static async Task<Results<Ok<AbsenceDto>, NotFound>> Refuser(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var absence = await db.Absences.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (absence is null)
        {
            return TypedResults.NotFound();
        }

        absence.Refuser();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(AbsenceDto.Depuis(absence));
    }

    private static async Task<Results<Ok<AbsenceDto>, NotFound>> Annuler(
        int id, ChronosDbContext db, TimeProvider horloge, CancellationToken cancellationToken)
    {
        var absence = await db.Absences.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (absence is null)
        {
            return TypedResults.NotFound();
        }

        absence.Annuler(horloge.Aujourdhui());
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(AbsenceDto.Depuis(absence));
    }
}
