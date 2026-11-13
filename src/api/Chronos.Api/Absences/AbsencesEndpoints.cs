using Chronos.Api.Securite;
using Chronos.Domain;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Tracabilite;
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
        groupe.MapPost("/{id:int}/approbation", Approuver).RequireAuthorization(Politiques.Validation);
        groupe.MapPost("/{id:int}/refus", Refuser).RequireAuthorization(Politiques.Validation);
        groupe.MapPost("/{id:int}/annulation", Annuler);

        return groupe;
    }

    // Une liste simple, comme celle des affectations : l'écran la demande pour un collaborateur.
    // Hors périmètre, un collaborateur demandé « n'existe pas » : 404. Sans collaborateurId, la liste du périmètre.
    private static async Task<Results<Ok<List<AbsenceDto>>, NotFound>> Lister(
        int? collaborateurId, ChronosDbContext db, PerimetreUtilisateur perimetre, CancellationToken cancellationToken)
    {
        if (collaborateurId is { } demande && !await perimetre.PeutVoirAsync(demande, cancellationToken))
        {
            return TypedResults.NotFound();
        }

        var requete = db.Absences
            .AsNoTracking()
            .Where(a => collaborateurId == null || a.CollaborateurId == collaborateurId);

        if (!perimetre.VoitTout)
        {
            var visibles = await perimetre.IdentifiantsVisiblesAsync(cancellationToken);
            requete = requete.Where(a => visibles.Contains(a.CollaborateurId));
        }

        var absences = await requete
            .OrderBy(a => a.DateDebut)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(absences.ConvertAll(AbsenceDto.Depuis));
    }

    private static async Task<Results<Ok<AbsenceDto>, NotFound>> Obtenir(
        int id, ChronosDbContext db, PerimetreUtilisateur perimetre, CancellationToken cancellationToken)
    {
        var absence = await db.Absences.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return absence is null || !await perimetre.PeutVoirAsync(absence.CollaborateurId, cancellationToken)
            ? TypedResults.NotFound()
            : TypedResults.Ok(AbsenceDto.Depuis(absence));
    }

    // Le demandeur est l'utilisateur connecté : un compte sans fiche de collaborateur ne pose pas d'absence (403).
    //
    // RG-41 lit les absences existantes pour décider d'en ajouter une : la lecture et l'écriture forment un tout,
    // dans une transaction sérialisable que la stratégie d'exécution rejoue si la base la fait échouer.
    private static async Task<Results<Created<AbsenceDto>, ForbidHttpResult, ValidationProblem>> Demander(
        CreerAbsenceRequete requete, ChronosDbContext db, IUtilisateurCourant utilisateur,
        CancellationToken cancellationToken)
    {
        if (utilisateur.CollaborateurId is not { } collaborateurId)
        {
            return TypedResults.Forbid();
        }
        if (!await db.TypesAbsence.AnyAsync(t => t.Id == requete.TypeAbsenceId, cancellationToken))
        {
            return Invalide("TypeAbsenceId", "Aucun type d'absence ne porte cet identifiant.");
        }

        var absence = await db.EnTransactionSerialisableAsync(async () =>
        {
            var demande = new Absence(collaborateurId, requete.TypeAbsenceId!.Value,
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

    // Même protection que la validation d'une feuille (RG-36) : le manager direct du demandeur ou un gestionnaire RH.
    //
    // RG-43 lit les feuilles de temps pour décider du sort d'une absence : même schéma que la demande.
    private static async Task<Results<Ok<AbsenceDto>, NotFound, ForbidHttpResult>> Approuver(
        int id, ChronosDbContext db, PerimetreUtilisateur perimetre, CancellationToken cancellationToken)
    {
        switch (await ControlerValidation(db, perimetre, id, cancellationToken))
        {
            case Controle.Inconnue:
                return TypedResults.NotFound();
            case Controle.Interdite:
                return TypedResults.Forbid();
        }

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

    private static async Task<Results<Ok<AbsenceDto>, NotFound, ForbidHttpResult>> Refuser(
        int id, ChronosDbContext db, PerimetreUtilisateur perimetre, CancellationToken cancellationToken)
    {
        switch (await ControlerValidation(db, perimetre, id, cancellationToken))
        {
            case Controle.Inconnue:
                return TypedResults.NotFound();
            case Controle.Interdite:
                return TypedResults.Forbid();
        }

        var absence = await db.Absences.FirstAsync(a => a.Id == id, cancellationToken);

        absence.Refuser();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(AbsenceDto.Depuis(absence));
    }

    // RG-40 : l'absence est annulée par le demandeur. Une absence du périmètre qui n'est pas la sienne se lit,
    // elle ne s'annule pas (403).
    private static async Task<Results<Ok<AbsenceDto>, NotFound, ForbidHttpResult>> Annuler(
        int id, ChronosDbContext db, TimeProvider horloge, PerimetreUtilisateur perimetre,
        CancellationToken cancellationToken)
    {
        var absence = await db.Absences.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (absence is null || !await perimetre.PeutVoirAsync(absence.CollaborateurId, cancellationToken))
        {
            return TypedResults.NotFound();
        }
        if (!perimetre.EstMoi(absence.CollaborateurId))
        {
            return TypedResults.Forbid();
        }

        absence.Annuler(horloge.Aujourdhui());
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(AbsenceDto.Depuis(absence));
    }

    private enum Controle { Autorise, Inconnue, Interdite }

    // Le périmètre cache les absences des autres équipes (404), puis la relation de validation refuse les siennes (403).
    private static async Task<Controle> ControlerValidation(
        ChronosDbContext db, PerimetreUtilisateur perimetre, int id, CancellationToken cancellationToken)
    {
        var demandeur = await db.Absences
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => (int?)a.CollaborateurId)
            .FirstOrDefaultAsync(cancellationToken);

        if (demandeur is not { } collaborateurId || !await perimetre.PeutVoirAsync(collaborateurId, cancellationToken))
        {
            return Controle.Inconnue;
        }

        return await perimetre.PeutValiderPourAsync(collaborateurId, cancellationToken)
            ? Controle.Autorise
            : Controle.Interdite;
    }
}
