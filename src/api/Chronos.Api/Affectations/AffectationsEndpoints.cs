using System.Linq.Expressions;
using Chronos.Domain;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.Affectations;

public static class AffectationsEndpoints
{
    public static RouteGroupBuilder MapAffectations(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/affectations").WithTags("Affectations");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);
        groupe.MapPost("/", Creer);
        groupe.MapPut("/{id:int}", Modifier);
        groupe.MapDelete("/{id:int}", Supprimer);

        return groupe;
    }

    private static readonly Expression<Func<Affectation, AffectationDto>> VersDto = a => new AffectationDto(
        a.Id, a.CollaborateurId, a.ProjetId, a.DateDebut, a.DateFin, a.TauxAllocation);

    // Une liste simple, comme celle des clients : l'écran la demande pour un collaborateur ou pour un projet.
    private static async Task<Ok<List<AffectationDto>>> Lister(
        int? collaborateurId, int? projetId, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var affectations = await db.Affectations
            .Where(a => collaborateurId == null || a.CollaborateurId == collaborateurId)
            .Where(a => projetId == null || a.ProjetId == projetId)
            .OrderBy(a => a.DateDebut)
            .ThenBy(a => a.Id)
            .Select(VersDto)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(affectations);
    }

    private static async Task<Results<Ok<AffectationDto>, NotFound>> Obtenir(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var affectation = await LireDto(db, id, cancellationToken);

        return affectation is null ? TypedResults.NotFound() : TypedResults.Ok(affectation);
    }

    private static async Task<Results<Created<AffectationDto>, ValidationProblem>> Creer(
        CreerAffectationRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        // Un collaborateur archivé n'est plus proposé : pour cette lecture, le filtre d'archivage s'applique.
        var collaborateur = await db.Collaborateurs
            .FirstOrDefaultAsync(c => c.Id == requete.CollaborateurId, cancellationToken);
        if (collaborateur is null)
        {
            return Invalide("CollaborateurId", "Aucun collaborateur ne porte cet identifiant.");
        }

        var projet = await db.Projets.FirstOrDefaultAsync(p => p.Id == requete.ProjetId, cancellationToken);
        if (projet is null)
        {
            return Invalide("ProjetId", "Aucun projet ne porte cet identifiant.");
        }

        var affectation = Affectation.Creer(collaborateur, projet,
            requete.DateDebut!.Value, requete.DateFin!.Value, requete.TauxAllocation!.Value);
        await VerifierSurcharge(db, collaborateur, projet, affectation, requete.SurchargeConfirmee, cancellationToken);

        db.Affectations.Add(affectation);
        await db.SaveChangesAsync(cancellationToken);

        var dto = await LireDto(db, affectation.Id, cancellationToken);
        return TypedResults.Created($"/api/affectations/{affectation.Id}", dto);
    }

    private static async Task<Results<Ok<AffectationDto>, NotFound>> Modifier(
        int id, ModifierAffectationRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var affectation = await db.Affectations.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (affectation is null)
        {
            return TypedResults.NotFound();
        }

        // Le collaborateur peut avoir été archivé depuis : son affectation reste modifiable.
        var collaborateur = await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .FirstAsync(c => c.Id == affectation.CollaborateurId, cancellationToken);
        var projet = await db.Projets.FirstAsync(p => p.Id == affectation.ProjetId, cancellationToken);

        affectation.Modifier(collaborateur, projet,
            requete.DateDebut!.Value, requete.DateFin!.Value, requete.TauxAllocation!.Value);
        await VerifierSurcharge(db, collaborateur, projet, affectation, requete.SurchargeConfirmee, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(await LireDto(db, id, cancellationToken));
    }

    private static async Task<Results<NoContent, NotFound>> Supprimer(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var affectation = await db.Affectations.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (affectation is null)
        {
            return TypedResults.NotFound();
        }

        db.Affectations.Remove(affectation);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    // RG-24 : une surcharge est autorisée, mais elle doit être confirmée. Le calcul est celui du plan de charge,
    // appliqué à un seul collaborateur, sur la période de l'affectation, affectation candidate comprise.
    // Les absences n'existent pas encore (module 10) : le dossier n'en contient pas.
    private static async Task VerifierSurcharge(
        ChronosDbContext db, Collaborateur collaborateur, Projet projet, Affectation affectation,
        bool surchargeConfirmee, CancellationToken cancellationToken)
    {
        if (surchargeConfirmee)
        {
            return;
        }

        var periode = new Periode(affectation.DateDebut, affectation.DateFin);

        var contrats = await db.Contrats
            .Where(k => k.CollaborateurId == collaborateur.Id)
            .Select(k => new { k.DateDebut, k.DateFin, k.Quotite })
            .ToListAsync(cancellationToken);

        // Les autres affectations du collaborateur : celle qu'on modifie est exclue, elle est la candidate.
        var autres = await db.Affectations
            .Where(a => a.CollaborateurId == collaborateur.Id && a.Id != affectation.Id
                && a.DateDebut <= periode.Fin && a.DateFin >= periode.Debut)
            .Select(a => new { a.ProjetId, a.DateDebut, a.DateFin, a.TauxAllocation, a.Projet!.Nature })
            .ToListAsync(cancellationToken);

        var joursFeries = await db.JoursFeries
            .Where(j => j.Date >= periode.Debut && j.Date <= periode.Fin)
            .Select(j => j.Date)
            .ToListAsync(cancellationToken);

        var dossier = new DossierDeCharge(
            Presence: new Periode(collaborateur.DateEntree, collaborateur.DateSortie ?? DateOnly.MaxValue),
            EstConsultant: collaborateur.Categorie == CategorieCollaborateur.Consultant,
            Quotites: [.. contrats.Select(k =>
                new QuotiteContractuelle(new Periode(k.DateDebut, k.DateFin ?? DateOnly.MaxValue), k.Quotite))],
            Affectations: [.. autres.Select(a =>
                new AffectationPlanifiee(a.ProjetId, new Periode(a.DateDebut, a.DateFin), a.TauxAllocation,
                    a.Nature == NatureProjet.Facturable))],
            Absences: new Dictionary<DateOnly, decimal>());

        var candidate = new AffectationPlanifiee(
            projet.Id, periode, affectation.TauxAllocation, projet.Nature == NatureProjet.Facturable);

        if (new CalculateurDeCharge(new CalendrierDeTravail(joursFeries)).MetEnSurcharge(dossier, candidate))
        {
            throw new RegleMetierException("RG-24",
                $"Cette affectation met {collaborateur.Prenom} {collaborateur.Nom} en surcharge sur au moins une " +
                "semaine : elle doit être confirmée (surchargeConfirmee).");
        }
    }

    private static Task<AffectationDto?> LireDto(ChronosDbContext db, int id, CancellationToken cancellationToken) =>
        db.Affectations
            .Where(a => a.Id == id)
            .Select(VersDto)
            .FirstOrDefaultAsync(cancellationToken);
}
