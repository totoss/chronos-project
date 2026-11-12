using Chronos.Api.Commun;
using Chronos.Domain;
using Chronos.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.PlanDeCharge;

public static class PlanDeChargeEndpoints
{
    public static RouteGroupBuilder MapPlanDeCharge(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/plan-de-charge").WithTags("Plan de charge");

        groupe.MapGet("/", Obtenir);

        return groupe;
    }

    private static ChargeDto VersDto(ChargeSurPeriode charge) => new(
        charge.Periode.Debut, charge.Periode.Fin, charge.Capacite, charge.ChargePlanifiee,
        charge.TauxDeCharge, charge.Surcharge, charge.Intercontrat);

    private static async Task<Results<Ok<PlanDeChargeDto>, ValidationProblem>> Obtenir(
        string? du, string? au, string? pas, int? managerId,
        ChronosDbContext db, TimeProvider horloge, CancellationToken cancellationToken)
    {
        // Les dates sont lues au seul format ISO : « 04/05/2026 » serait sinon lu comme le 5 avril.
        DateOnly dateDu = default, dateAu = default;
        if (du is not null && !DatesIso.TryLire(du, out dateDu))
        {
            return Invalide("Du", "Format attendu : aaaa-mm-jj.");
        }
        if (au is not null && !DatesIso.TryLire(au, out dateAu))
        {
            return Invalide("Au", "Format attendu : aaaa-mm-jj.");
        }

        var debut = du is null ? Semaine.Contenant(horloge.Aujourdhui()).Debut : dateDu;
        var fin = au is null ? Semaine.Contenant(debut).Fin : dateAu;

        if (fin < debut || fin.DayNumber - debut.DayNumber > 366)
        {
            return Invalide("Au", "La période demandée va de « du » à « au », sur un an au plus.");
        }
        if (pas is not (null or "semaine" or "mois"))
        {
            return Invalide("Pas", "Valeurs acceptées : semaine, mois.");
        }

        var periode = new Periode(debut, fin);
        var tranches = (pas == "mois" ? Decoupage.ParMois(periode) : Decoupage.ParSemaine(periode)).ToList();

        // Requête 1 : les collaborateurs présents sur la période, avec leurs contrats.
        var collaborateurs = await db.Collaborateurs
            .Where(c => managerId == null || c.ManagerId == managerId)
            .Where(c => c.DateEntree <= fin && (c.DateSortie == null || c.DateSortie >= debut))
            .OrderBy(c => c.Nom).ThenBy(c => c.Prenom).ThenBy(c => c.Id)
            .Select(c => new
            {
                c.Id, c.Nom, c.Prenom, c.Categorie, c.DateEntree, c.DateSortie,
                Contrats = c.Contrats.Select(k => new { k.DateDebut, k.DateFin, k.Quotite }).ToList(),
            })
            .ToListAsync(cancellationToken);

        var ids = collaborateurs.Select(c => c.Id).ToList();

        // Requête 2 : leurs affectations qui touchent la période, avec la nature du projet.
        var affectations = await db.Affectations
            .Where(a => ids.Contains(a.CollaborateurId) && a.DateDebut <= fin && a.DateFin >= debut)
            .Select(a => new { a.CollaborateurId, a.ProjetId, a.DateDebut, a.DateFin, a.TauxAllocation, a.Projet!.Nature })
            .ToListAsync(cancellationToken);

        // Requête 3 : les jours fériés de la période.
        var joursFeries = await db.JoursFeries
            .Where(j => j.Date >= debut && j.Date <= fin)
            .Select(j => j.Date)
            .ToListAsync(cancellationToken);

        var calculateur = new CalculateurDeCharge(new CalendrierDeTravail(joursFeries));
        var affectationsParCollaborateur = affectations.ToLookup(a => a.CollaborateurId);

        var lignes = collaborateurs.Select(c =>
        {
            var dossier = new DossierDeCharge(
                Presence: new Periode(c.DateEntree, c.DateSortie ?? DateOnly.MaxValue),
                EstConsultant: c.Categorie == CategorieCollaborateur.Consultant,
                Quotites: [.. c.Contrats.Select(k =>
                    new QuotiteContractuelle(new Periode(k.DateDebut, k.DateFin ?? DateOnly.MaxValue), k.Quotite))],
                Affectations: [.. affectationsParCollaborateur[c.Id].Select(a =>
                    new AffectationPlanifiee(a.ProjetId, new Periode(a.DateDebut, a.DateFin), a.TauxAllocation,
                        a.Nature == NatureProjet.Facturable))],
                Absences: new Dictionary<DateOnly, decimal>());

            return new LignePlanDeChargeDto(c.Id, c.Nom, c.Prenom,
                [.. tranches.Select(t => VersDto(calculateur.Calculer(dossier, t)))]);
        }).ToList();

        return TypedResults.Ok(new PlanDeChargeDto(debut, fin, pas ?? "semaine", lignes));
    }
}
