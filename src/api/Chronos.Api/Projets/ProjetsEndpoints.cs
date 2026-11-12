using System.Linq.Expressions;
using Chronos.Api.Commun;
using Chronos.Domain;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Configurations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.Projets;

public static class ProjetsEndpoints
{
    public static RouteGroupBuilder MapProjets(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/projets").WithTags("Projets");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);
        groupe.MapPost("/", Creer);
        groupe.MapPut("/{id:int}", Modifier);
        groupe.MapPost("/{id:int}/demarrage", Demarrer);
        groupe.MapPost("/{id:int}/achevement", Terminer);
        groupe.MapPost("/{id:int}/annulation", Annuler);

        return groupe;
    }

    private static readonly Expression<Func<Projet, ProjetDto>> VersDto = p => new ProjetDto(
        p.Id, p.Code, p.Libelle, p.Nature,
        p.ClientId, p.Client!.Nom,
        p.DateDebut, p.DateFin, p.BudgetJours,
        p.ResponsableId, p.Responsable!.Prenom + " " + p.Responsable.Nom, p.Statut,
        p.Version);

    private static async Task<Results<Ok<PageDto<ProjetDto>>, ValidationProblem>> Lister(
        [AsParameters] FiltreProjets filtre, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var requete = db.Projets.IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives]);

        if (!string.IsNullOrWhiteSpace(filtre.Recherche))
        {
            var recherche = filtre.Recherche.Trim().ToLowerInvariant();
            requete = requete.Where(p =>
                p.Code.ToLower().Contains(recherche) || p.Libelle.ToLower().Contains(recherche));
        }

        // Le texte est comparé aux noms de l'énumération : « 1 » (un rang) est refusé, comme dans le JSON.
        if (!string.IsNullOrEmpty(filtre.Statut))
        {
            var nom = Enum.GetNames<StatutProjet>()
                .FirstOrDefault(n => string.Equals(n, filtre.Statut, StringComparison.OrdinalIgnoreCase));
            if (nom is null)
            {
                return Invalide("Statut", $"Valeurs acceptées : {string.Join(", ", Enum.GetNames<StatutProjet>())}.");
            }

            var statut = Enum.Parse<StatutProjet>(nom);
            requete = requete.Where(p => p.Statut == statut);
        }

        IOrderedQueryable<Projet> triee;
        switch (filtre.Tri)
        {
            case null or "" or "code":
                triee = requete.OrderBy(p => p.Code);
                break;
            case "-code":
                triee = requete.OrderByDescending(p => p.Code);
                break;
            case "libelle":
                triee = requete.OrderBy(p => p.Libelle);
                break;
            case "dateDebut":
                triee = requete.OrderBy(p => p.DateDebut);
                break;
            case "-dateDebut":
                triee = requete.OrderByDescending(p => p.DateDebut);
                break;
            default:
                return Invalide("Tri", "Valeurs acceptées : code, -code, libelle, dateDebut, -dateDebut.");
        }

        var total = await requete.CountAsync(cancellationToken);

        var elements = await triee
            .ThenBy(p => p.Id)
            .Skip((filtre.Page - 1) * filtre.Taille)
            .Take(filtre.Taille)
            .Select(VersDto)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PageDto<ProjetDto>(elements, filtre.Page, filtre.Taille, total));
    }

    private static async Task<Results<Ok<ProjetDto>, NotFound>> Obtenir(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var projet = await LireDto(db, id, cancellationToken);

        return projet is null ? TypedResults.NotFound() : TypedResults.Ok(projet);
    }

    private static async Task<Results<Created<ProjetDto>, ProblemHttpResult, ValidationProblem>> Creer(
        CreerProjetRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var projet = new Projet(requete.Code, requete.Libelle, requete.Nature!.Value, requete.ClientId,
            requete.DateDebut!.Value, requete.DateFin, requete.BudgetJours!.Value, requete.ResponsableId!.Value);

        // Le domaine a normalisé le code (majuscules) : c'est cette forme qu'on compare, sur les deux SGBD.
        if (await db.Projets.AnyAsync(p => p.Code == projet.Code, cancellationToken))
        {
            return Conflit("Un projet porte déjà ce code (RG-10).");
        }

        var inconnu = await ReferenceInconnue(db, projet, cancellationToken);
        if (inconnu is not null)
        {
            return inconnu;
        }

        db.Projets.Add(projet);
        await db.SaveChangesAsync(cancellationToken);

        var dto = await LireDto(db, projet.Id, cancellationToken);
        return TypedResults.Created($"/api/projets/{projet.Id}", dto);
    }

    private static async Task<Results<Ok<ProjetDto>, NotFound, ValidationProblem>> Modifier(
        int id, ModifierProjetRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var projet = await db.Projets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (projet is null)
        {
            return TypedResults.NotFound();
        }

        // Le jeton que le client avait sous les yeux remplace celui qu'EF Core vient de lire.
        db.Entry(projet).Property(p => p.Version).OriginalValue = requete.Version!.Value;

        projet.Modifier(requete.Libelle, requete.Nature!.Value, requete.ClientId,
            requete.DateDebut!.Value, requete.DateFin, requete.BudgetJours!.Value, requete.ResponsableId!.Value);

        var inconnu = await ReferenceInconnue(db, projet, cancellationToken);
        if (inconnu is not null)
        {
            return inconnu;
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(await LireDto(db, id, cancellationToken));
    }

    // Une transition impossible est un conflit avec l'état du projet (409), et non une règle numérotée.
    // On interroge le domaine avant d'agir : aucune InvalidOperationException n'est attrapée ni traduite.
    private static async Task<Results<Ok<ProjetDto>, NotFound, ProblemHttpResult>> Demarrer(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var projet = await db.Projets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (projet is null)
        {
            return TypedResults.NotFound();
        }

        if (!projet.PeutDemarrer)
        {
            return Conflit($"Un projet {Libelle(projet.Statut)} ne peut pas être démarré.");
        }

        projet.Demarrer();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(await LireDto(db, id, cancellationToken));
    }

    private static async Task<Results<Ok<ProjetDto>, NotFound, ProblemHttpResult>> Terminer(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var projet = await db.Projets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (projet is null)
        {
            return TypedResults.NotFound();
        }

        if (!projet.PeutTerminer)
        {
            return Conflit($"Un projet {Libelle(projet.Statut)} ne peut pas être terminé.");
        }

        projet.Terminer();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(await LireDto(db, id, cancellationToken));
    }

    private static async Task<Results<Ok<ProjetDto>, NotFound, ProblemHttpResult>> Annuler(
        int id, ChronosDbContext db, CancellationToken cancellationToken)
    {
        var projet = await db.Projets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (projet is null)
        {
            return TypedResults.NotFound();
        }

        if (!projet.PeutAnnuler)
        {
            return Conflit($"Un projet {Libelle(projet.Statut)} ne peut pas être annulé.");
        }

        projet.Annuler();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(await LireDto(db, id, cancellationToken));
    }

    private static string Libelle(StatutProjet statut) => statut switch
    {
        StatutProjet.EnPreparation => "en préparation",
        StatutProjet.EnCours => "en cours",
        StatutProjet.Termine => "terminé",
        StatutProjet.Annule => "annulé",
        _ => statut.ToString(),
    };

    private static Task<ProjetDto?> LireDto(ChronosDbContext db, int id, CancellationToken cancellationToken) =>
        db.Projets
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .Where(p => p.Id == id)
            .Select(VersDto)
            .FirstOrDefaultAsync(cancellationToken);

    // Un client ou un responsable inconnu est une erreur du client : 400, et non la 500 de la clé étrangère.
    private static async Task<ValidationProblem?> ReferenceInconnue(
        ChronosDbContext db, Projet projet, CancellationToken cancellationToken)
    {
        if (projet.ClientId is not null
            && !await db.Clients.AnyAsync(c => c.Id == projet.ClientId, cancellationToken))
        {
            return Invalide("ClientId", "Aucun client ne porte cet identifiant.");
        }

        var responsableExiste = await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .AnyAsync(c => c.Id == projet.ResponsableId, cancellationToken);

        return responsableExiste ? null : Invalide("ResponsableId", "Aucun collaborateur ne porte cet identifiant.");
    }
}
