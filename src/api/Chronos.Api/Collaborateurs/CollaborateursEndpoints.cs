using System.Linq.Expressions;
using Chronos.Api.Commun;
using Chronos.Domain;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.Collaborateurs;

public static class CollaborateursEndpoints
{
    public static RouteGroupBuilder MapCollaborateurs(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/collaborateurs").WithTags("Collaborateurs");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);
        groupe.MapPost("/", Creer);
        groupe.MapPut("/{id:int}", Modifier);
        groupe.MapDelete("/{id:int}", Supprimer);

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
            .FirstOrDefault(),
        c.Version);

    private static async Task<Results<Ok<PageDto<CollaborateurDto>>, ValidationProblem>> Lister(
        [AsParameters] FiltreCollaborateurs filtre, ChronosDbContext db, TimeProvider horloge,
        CancellationToken cancellationToken)
    {
        var requete = db.Collaborateurs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtre.Recherche))
        {
            // Minuscules des deux côtés : la seule écriture qui donne le même résultat sur les deux SGBD.
            var recherche = filtre.Recherche.Trim().ToLowerInvariant();
            requete = requete.Where(c =>
                c.Nom.ToLower().Contains(recherche) ||
                c.Prenom.ToLower().Contains(recherche) ||
                c.Matricule.ToLower().Contains(recherche));
        }

        var aujourdhui = horloge.Aujourdhui();
        switch (filtre.Statut)
        {
            case null or "":
                break;
            case "actif":
                requete = requete.Where(c => c.DateSortie == null || c.DateSortie >= aujourdhui);
                break;
            case "sorti":
                requete = requete.Where(c => c.DateSortie < aujourdhui);
                break;
            default:
                return Invalide("Statut", "Valeurs acceptées : actif, sorti.");
        }

        IOrderedQueryable<Collaborateur> triee;
        switch (filtre.Tri)
        {
            case null or "" or "nom":
                triee = requete.OrderBy(c => c.Nom).ThenBy(c => c.Prenom);
                break;
            case "-nom":
                triee = requete.OrderByDescending(c => c.Nom).ThenByDescending(c => c.Prenom);
                break;
            case "matricule":
                triee = requete.OrderBy(c => c.Matricule);
                break;
            default:
                return Invalide("Tri", "Valeurs acceptées : nom, -nom, matricule.");
        }

        var total = await requete.CountAsync(cancellationToken);

        var elements = await triee
            .ThenBy(c => c.Id)
            .Skip((filtre.Page - 1) * filtre.Taille)
            .Take(filtre.Taille)
            .Select(VersDto)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PageDto<CollaborateurDto>(elements, filtre.Page, filtre.Taille, total));
    }

    private static async Task<Results<Ok<CollaborateurDto>, NotFound>> Obtenir(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var collaborateur = await db.Collaborateurs
            .Where(c => c.Id == id)
            .Select(VersDto)
            .FirstOrDefaultAsync(cancellationToken);

        return collaborateur is null ? TypedResults.NotFound() : TypedResults.Ok(collaborateur);
    }

    private static async Task<Results<Created<CollaborateurDto>, ProblemHttpResult, ValidationProblem>> Creer(
        CreerCollaborateurRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        if (!await ManagerExiste(db, requete.ManagerId, cancellationToken))
        {
            return Invalide("ManagerId", "Aucun collaborateur ne porte cet identifiant.");
        }

        var collaborateur = new Collaborateur(requete.Matricule, requete.Nom, requete.Prenom, requete.Email,
            requete.Categorie!.Value, requete.Poste, requete.DateEntree!.Value);
        collaborateur.AjouterContrat(requete.TypeContrat!.Value, requete.Quotite!.Value, requete.DateEntree.Value);
        collaborateur.RattacherA(requete.ManagerId);

        // Le domaine a normalisé le matricule (majuscules) et l'adresse (minuscules) : ce sont ces formes
        // qu'on compare, pour que PostgreSQL et SQL Server donnent la même réponse.
        var dejaPris = await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .AnyAsync(c => c.Matricule == collaborateur.Matricule || c.Email == collaborateur.Email, cancellationToken);
        if (dejaPris)
        {
            return Conflit("Un collaborateur porte déjà ce matricule ou cette adresse électronique (RG-01).");
        }

        db.Collaborateurs.Add(collaborateur);
        await db.SaveChangesAsync(cancellationToken);

        var dto = await db.Collaborateurs
            .Where(c => c.Id == collaborateur.Id).Select(VersDto).SingleAsync(cancellationToken);
        return TypedResults.Created($"/api/collaborateurs/{dto.Id}", dto);
    }

    private static async Task<Results<Ok<CollaborateurDto>, NotFound, ProblemHttpResult, ValidationProblem>> Modifier(
        int id, ModifierCollaborateurRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var collaborateur = await db.Collaborateurs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (collaborateur is null)
        {
            return TypedResults.NotFound();
        }

        if (!await ManagerExiste(db, requete.ManagerId, cancellationToken))
        {
            return Invalide("ManagerId", "Aucun collaborateur ne porte cet identifiant.");
        }

        await VerifierChaineDeManagers(db, id, requete.ManagerId, cancellationToken);

        // Le jeton que le client avait sous les yeux remplace celui qu'EF Core vient de lire :
        // c'est la base qui compare, dans la clause WHERE de l'UPDATE.
        db.Entry(collaborateur).Property(c => c.Version).OriginalValue = requete.Version!.Value;

        collaborateur.Modifier(requete.Nom, requete.Prenom, requete.Email, requete.Poste,
            requete.DateSortie, requete.ManagerId);

        // Le domaine a normalisé l'adresse (minuscules) : c'est cette forme qu'on compare, hors cette fiche.
        var adresseDejaPrise = await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .AnyAsync(c => c.Id != id && c.Email == collaborateur.Email, cancellationToken);
        if (adresseDejaPrise)
        {
            return Conflit("Un collaborateur porte déjà cette adresse électronique (RG-01).");
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(await db.Collaborateurs
            .Where(c => c.Id == id).Select(VersDto).SingleAsync(cancellationToken));
    }

    // Un manager archivé existe toujours : la clé étrangère l'accepte, la vérification aussi.
    private static async Task<bool> ManagerExiste(ChronosDbContext db, int? managerId, CancellationToken cancellationToken) =>
        managerId is null
        || await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .AnyAsync(c => c.Id == managerId, cancellationToken);

    private static async Task VerifierChaineDeManagers(
        ChronosDbContext db, int collaborateurId, int? managerId, CancellationToken cancellationToken)
    {
        var dejaVus = new HashSet<int>();
        var courant = managerId;

        while (courant is not null && dejaVus.Add(courant.Value))
        {
            if (courant == collaborateurId)
            {
                throw new RegleMetierException("RG-04",
                    "Un collaborateur ne peut pas être son propre manager, ni directement ni par une chaîne de managers.");
            }

            courant = await db.Collaborateurs
                .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
                .Where(c => c.Id == courant)
                .Select(c => c.ManagerId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private static async Task<Results<NoContent, NotFound>> Supprimer(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var collaborateur = await db.Collaborateurs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (collaborateur is null)
        {
            return TypedResults.NotFound();
        }

        // RG-05 : qui possède des données liées est archivé ; les autres sont réellement supprimés.
        var aDesDonneesLiees =
            await db.Collaborateurs
                .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
                .AnyAsync(c => c.ManagerId == id, cancellationToken)
            || await db.Projets.AnyAsync(p => p.ResponsableId == id, cancellationToken)
            || await db.Affectations.AnyAsync(a => a.CollaborateurId == id, cancellationToken)
            || await db.Absences.AnyAsync(a => a.CollaborateurId == id, cancellationToken)
            || await db.FeuillesDeTemps.AnyAsync(f => f.CollaborateurId == id, cancellationToken);

        if (aDesDonneesLiees)
        {
            collaborateur.Archiver();
        }
        else
        {
            db.Collaborateurs.Remove(collaborateur);
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }
}
