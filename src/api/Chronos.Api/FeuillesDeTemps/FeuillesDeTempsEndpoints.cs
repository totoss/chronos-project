using Chronos.Api.Commun;
using Chronos.Api.Securite;
using Chronos.Domain;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Tracabilite;
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
        groupe.MapGet("/soumises", ListerSoumises).RequireAuthorization(Politiques.Validation);
        groupe.MapPut("/{id:int}/saisies", RemplacerSaisies);
        groupe.MapPost("/{id:int}/soumission", Soumettre);
        groupe.MapPost("/{id:int}/validation", Valider).RequireAuthorization(Politiques.Validation);
        groupe.MapPost("/{id:int}/rejet", Rejeter).RequireAuthorization(Politiques.Validation);

        return groupe;
    }

    // Un GET qui écrit n'est pas orthodoxe : l'écran de saisie demande « la feuille de la semaine » sans savoir
    // si elle existe. La feuille absente est créée à l'état de brouillon, vide.
    //
    // Sans collaborateurId, c'est la feuille de l'utilisateur connecté. Pour un autre, il faut l'avoir dans son
    // périmètre (un manager consulte la feuille de son équipe) : sinon 404.
    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound, ValidationProblem>> ObtenirOuCreer(
        int? collaborateurId, string? semaine, ChronosDbContext db, IUtilisateurCourant utilisateur,
        PerimetreUtilisateur perimetre, CancellationToken cancellationToken)
    {
        collaborateurId ??= utilisateur.CollaborateurId;
        if (collaborateurId is null)
        {
            return Invalide("CollaborateurId", "Le collaborateur est obligatoire : ce compte n'est rattaché à aucune fiche.");
        }
        if (!await perimetre.PeutVoirAsync(collaborateurId.Value, cancellationToken))
        {
            return TypedResults.NotFound();
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

    // Les feuilles que l'appelant peut valider (RG-36) : mêmes règles que PeutValiderPourAsync. Le gestionnaire RH
    // en voit toutes sauf la sienne ; un manager, celles de son équipe directe. Un GET qui ne crée rien ; la route
    // littérale l'emporte sur « /{id} » (qui est de toute façon un entier).
    private static async Task<Ok<List<FeuilleDeTempsDto>>> ListerSoumises(
        ChronosDbContext db, IUtilisateurCourant utilisateur, PerimetreUtilisateur perimetre,
        CancellationToken cancellationToken)
    {
        if (utilisateur.CollaborateurId is not { } moi)
        {
            return TypedResults.Ok(new List<FeuilleDeTempsDto>());
        }

        var requete = db.FeuillesDeTemps
            .AsNoTracking()
            .Include(f => f.Saisies)
            .Where(f => f.Statut == StatutFeuilleDeTemps.Soumise && f.CollaborateurId != moi);

        if (!perimetre.VoitTout)
        {
            var equipe = await db.Collaborateurs
                .Where(c => c.ManagerId == moi)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            requete = requete.Where(f => equipe.Contains(f.CollaborateurId));
        }

        var feuilles = await requete
            .OrderBy(f => f.Semaine).ThenBy(f => f.CollaborateurId)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(feuilles.ConvertAll(FeuilleDeTempsDto.Depuis));
    }

    // Chacun saisit ses propres temps : une feuille du périmètre mais qui n'est pas la sienne se lit, elle ne s'écrit pas (403).
    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound, ForbidHttpResult, ValidationProblem>> RemplacerSaisies(
        int id, RemplacerSaisiesRequete requete, ChronosDbContext db, PerimetreUtilisateur perimetre,
        CancellationToken cancellationToken)
    {
        var feuille = await db.FeuillesDeTemps
            .Include(f => f.Saisies)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (feuille is null || !await perimetre.PeutVoirAsync(feuille.CollaborateurId, cancellationToken))
        {
            return TypedResults.NotFound();
        }
        if (!perimetre.EstMoi(feuille.CollaborateurId))
        {
            return TypedResults.Forbid();
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

    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound, ForbidHttpResult>> Soumettre(
        int id, ChronosDbContext db, PerimetreUtilisateur perimetre, CancellationToken cancellationToken)
    {
        var feuille = await db.FeuillesDeTemps
            .Include(f => f.Saisies)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (feuille is null || !await perimetre.PeutVoirAsync(feuille.CollaborateurId, cancellationToken))
        {
            return TypedResults.NotFound();
        }
        if (!perimetre.EstMoi(feuille.CollaborateurId))
        {
            return TypedResults.Forbid();
        }

        feuille.Soumettre();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
    }

    // RG-43, vue du côté de la feuille : valider du temps sur lequel une absence a été approuvée entre-temps
    // serait aussi une violation. La validation LIT donc les absences approuvées, dans une transaction
    // sérialisable : si une approbation s'enregistre en même temps, la base fait échouer l'une des deux.
    //
    // RG-36 sur trois niveaux : la politique écarte ceux qui n'ont pas la fonction (403), le périmètre cache les
    // feuilles des autres équipes (404), puis la relation de validation refuse sa propre feuille ou celle d'un
    // collaborateur dont on n'est pas le manager direct (403). Le valideur est l'utilisateur connecté.
    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound, ForbidHttpResult>> Valider(
        int id, ChronosDbContext db, IUtilisateurCourant utilisateur, PerimetreUtilisateur perimetre,
        ILogger<Program> logger, MetriquesFeuillesDeTemps metriques, CancellationToken cancellationToken)
    {
        var proprietaire = await ProprietaireDeLaFeuille(db, id, cancellationToken);
        if (proprietaire is not { } collaborateurId || !await perimetre.PeutVoirAsync(collaborateurId, cancellationToken))
        {
            return TypedResults.NotFound();
        }
        if (!await perimetre.PeutValiderPourAsync(collaborateurId, cancellationToken))
        {
            JournalFeuillesDeTemps.ValidationRefusee(logger, id, "RG-36");
            return TypedResults.Forbid();
        }

        var valideurId = utilisateur.CollaborateurId!.Value;
        var feuille = await db.EnTransactionSerialisableAsync(async () =>
        {
            var feuille = await db.FeuillesDeTemps
                .Include(f => f.Saisies)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (feuille is null)
            {
                return null;
            }

            feuille.Valider(valideurId);

            var absences = await db.Absences
                .Where(a => a.CollaborateurId == feuille.CollaborateurId && a.Statut == StatutAbsence.Approuvee
                    && a.DateDebut <= feuille.Periode.Fin && a.DateFin >= feuille.Periode.Debut)
                .ToListAsync(cancellationToken);
            Absence.VerifierTempsValide(feuille.TempsParJour(), Absence.PartsAbsentes(absences, feuille.Periode));

            await db.SaveChangesAsync(cancellationToken);
            return feuille;
        }, cancellationToken);

        if (feuille is null)
        {
            return TypedResults.NotFound();
        }

        JournalFeuillesDeTemps.FeuilleValidee(logger, feuille.Id, utilisateur.Identifiant);
        metriques.Validee();
        return TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
    }

    // Même protection que la validation (RG-36).
    private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound, ForbidHttpResult>> Rejeter(
        int id, RejeterFeuilleRequete requete, ChronosDbContext db, IUtilisateurCourant utilisateur,
        PerimetreUtilisateur perimetre, ILogger<Program> logger, MetriquesFeuillesDeTemps metriques,
        CancellationToken cancellationToken)
    {
        var proprietaire = await ProprietaireDeLaFeuille(db, id, cancellationToken);
        if (proprietaire is not { } collaborateurId || !await perimetre.PeutVoirAsync(collaborateurId, cancellationToken))
        {
            return TypedResults.NotFound();
        }
        if (!await perimetre.PeutValiderPourAsync(collaborateurId, cancellationToken))
        {
            JournalFeuillesDeTemps.ValidationRefusee(logger, id, "RG-36");
            return TypedResults.Forbid();
        }

        var feuille = await db.FeuillesDeTemps
            .Include(f => f.Saisies)
            .FirstAsync(f => f.Id == id, cancellationToken);

        feuille.Rejeter(utilisateur.CollaborateurId!.Value, requete.Motif);
        await db.SaveChangesAsync(cancellationToken);

        JournalFeuillesDeTemps.FeuilleRejetee(logger, feuille.Id, utilisateur.Identifiant);
        metriques.Rejetee();

        return TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
    }

    private static Task<int?> ProprietaireDeLaFeuille(ChronosDbContext db, int id, CancellationToken cancellationToken) =>
        db.FeuillesDeTemps
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => (int?)f.CollaborateurId)
            .FirstOrDefaultAsync(cancellationToken);

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
