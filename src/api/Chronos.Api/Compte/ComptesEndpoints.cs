using System.ComponentModel.DataAnnotations;
using Chronos.Api.Securite;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Identite;
using Chronos.Infrastructure.Tracabilite;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Chronos.Api.Commun.Reponses;

namespace Chronos.Api.Compte;

public record CreerCompteRequete(
    [Required(ErrorMessage = "L'adresse électronique est obligatoire."),
     EmailAddress(ErrorMessage = "L'adresse électronique n'est pas valide."),
     StringLength(254, ErrorMessage = "L'adresse électronique ne dépasse pas {1} caractères.")] string Email,
    [Required(ErrorMessage = "Le mot de passe initial est obligatoire.")] string MotDePasse,
    int? CollaborateurId,
    [Required(ErrorMessage = "Les rôles sont obligatoires."),
     MinLength(1, ErrorMessage = "Au moins un rôle est obligatoire.")] string[] Roles);

/// <summary>
/// Remplace l'état modifiable d'un compte (PUT) : la fiche rattachée (absente = détachée), tous les rôles, le verrouillage.
/// L'adresse et le mot de passe ne changent pas ici.
/// </summary>
public record ModifierCompteRequete(
    int? CollaborateurId,
    [Required(ErrorMessage = "Les rôles sont obligatoires."),
     MinLength(1, ErrorMessage = "Au moins un rôle est obligatoire.")] string[] Roles,
    [Required(ErrorMessage = "Indiquez si le compte est verrouillé.")] bool? Verrouille);

/// <summary>Un compte, tel que l'administrateur le voit : jamais de mot de passe, d'empreinte ni de jeton.</summary>
public record CompteDto(int Id, string Email, int? CollaborateurId, IReadOnlyList<string> Roles, bool Verrouille);

public static class ComptesEndpoints
{
    public static RouteGroupBuilder MapComptes(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/comptes").WithTags("Comptes");

        groupe.MapGet("/", Lister).RequireAuthorization(Politiques.Administration);
        groupe.MapPost("/", Creer).RequireAuthorization(Politiques.Administration);
        groupe.MapPut("/{id:int}", Modifier).RequireAuthorization(Politiques.Administration);

        return groupe;
    }

    // Une liste simple, comme celle des absences : une organisation a quelques dizaines de comptes, pas des milliers.
    private static async Task<Ok<List<CompteDto>>> Lister(
        ChronosDbContext db, TimeProvider horloge, CancellationToken cancellationToken)
    {
        var maintenant = horloge.GetUtcNow();
        var comptes = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.CollaborateurId,
                Verrouille = u.LockoutEnabled && u.LockoutEnd > maintenant,
            })
            .ToListAsync(cancellationToken);

        var rolesParCompte = (await (
                from lien in db.UserRoles
                join role in db.Roles on lien.RoleId equals role.Id
                select new { lien.UserId, role.Name })
            .ToListAsync(cancellationToken))
            .ToLookup(l => l.UserId, l => l.Name!);

        return TypedResults.Ok(comptes.ConvertAll(c =>
            new CompteDto(c.Id, c.Email ?? "", c.CollaborateurId, [.. rolesParCompte[c.Id].Order()], c.Verrouille)));
    }

    // L'inscription libre est neutralisée : c'est l'administrateur qui crée les comptes. Il choisit le mot de passe
    // initial, que Chronos ne renvoie jamais ; Identity en vérifie la robustesse.
    private static async Task<Results<Created<CompteDto>, ValidationProblem, ProblemHttpResult>> Creer(
        CreerCompteRequete requete, UserManager<Utilisateur> utilisateurs, ChronosDbContext db,
        IUtilisateurCourant courant, ILogger<Program> logger, CancellationToken cancellationToken)
    {
        if (RolesInconnus(requete.Roles) is { } rolesInconnus)
        {
            return Invalide("Roles", rolesInconnus);
        }

        if (requete.CollaborateurId is { } collaborateurId)
        {
            if (!await db.Collaborateurs.AnyAsync(c => c.Id == collaborateurId, cancellationToken))
            {
                return Invalide("CollaborateurId", "Aucun collaborateur ne porte cet identifiant.");
            }

            if (await db.Users.AnyAsync(u => u.CollaborateurId == collaborateurId, cancellationToken))
            {
                return Conflit("Ce collaborateur a déjà un compte.");
            }
        }

        var compte = new Utilisateur
        {
            UserName = requete.Email,
            Email = requete.Email,
            CollaborateurId = requete.CollaborateurId,
        };

        var creation = await utilisateurs.CreateAsync(compte, requete.MotDePasse);
        if (!creation.Succeeded)
        {
            if (creation.Errors.Any(e => e.Code is nameof(IdentityErrorDescriber.DuplicateEmail)
                    or nameof(IdentityErrorDescriber.DuplicateUserName)))
            {
                return Conflit("Un compte porte déjà cette adresse électronique.");
            }

            return TypedResults.ValidationProblem(creation.Errors
                .GroupBy(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "MotDePasse" : "Email")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        var attribution = await utilisateurs.AddToRolesAsync(compte, requete.Roles);
        if (!attribution.Succeeded)
        {
            // Un compte ne reste jamais sans ses rôles : on le retire et on signale l'erreur.
            await utilisateurs.DeleteAsync(compte);
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Roles"] = [.. attribution.Errors.Select(e => e.Description)],
            });
        }

        JournalComptes.CompteCree(logger, compte.Id, courant.Identifiant);
        return TypedResults.Created((string?)null,
            new CompteDto(compte.Id, requete.Email, requete.CollaborateurId, [.. requete.Roles.Order()], Verrouille: false));
    }

    // Rattacher ou détacher une fiche, remplacer les rôles, verrouiller ou déverrouiller : l'administrateur ne
    // peut pas se couper lui-même de l'administration. Ce garde-fou garantit aussi qu'il reste toujours au moins
    // un administrateur actif : celui qui agit.
    //
    // Les refus suivent les conventions du dépôt : 404 si le compte n'existe pas, 400 pour une valeur que
    // l'API ne reconnaît pas (rôle, fiche), 409 quand la demande est correcte mais que l'état ne la permet pas
    // (fiche déjà prise, administrateur qui se retire ses droits). Le 422 est réservé aux règles de gestion
    // numérotées du cahier des charges (champ « regle ») ; celle-ci n'en est pas une.
    private static async Task<Results<Ok<CompteDto>, NotFound, ValidationProblem, ProblemHttpResult>> Modifier(
        int id, ModifierCompteRequete requete, UserManager<Utilisateur> utilisateurs, ChronosDbContext db,
        IUtilisateurCourant courant, ILogger<Program> logger, TimeProvider horloge, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id, cancellationToken))
        {
            return TypedResults.NotFound();
        }
        if (RolesInconnus(requete.Roles) is { } rolesInconnus)
        {
            return Invalide("Roles", rolesInconnus);
        }

        var roles = requete.Roles.Distinct().ToList();
        var verrouille = requete.Verrouille!.Value;

        if (id.ToString(System.Globalization.CultureInfo.InvariantCulture) == courant.Identifiant)
        {
            if (!roles.Contains(Roles.Administrateur))
            {
                JournalComptes.ModificationRefusee(logger, id, courant.Identifiant, "retrait du rôle Administrateur à soi-même");
                return Conflit("Un administrateur ne peut pas se retirer le rôle Administrateur.");
            }
            if (verrouille)
            {
                JournalComptes.ModificationRefusee(logger, id, courant.Identifiant, "verrouillage de son propre compte");
                return Conflit("Un administrateur ne peut pas verrouiller son propre compte.");
            }
        }

        if (requete.CollaborateurId is { } collaborateurId)
        {
            if (!await db.Collaborateurs.AnyAsync(c => c.Id == collaborateurId, cancellationToken))
            {
                return Invalide("CollaborateurId", "Aucun collaborateur ne porte cet identifiant.");
            }
            if (await db.Users.AnyAsync(u => u.CollaborateurId == collaborateurId && u.Id != id, cancellationToken))
            {
                return Conflit("Cette fiche est déjà rattachée à un autre compte.");
            }
        }

        // Les écritures forment un tout : un compte ne reste pas à moitié modifié. La fonction est rejouée en cas
        // d'échec passager, et recharge donc le compte à chaque passage.
        var compte = await db.EnTransactionSerialisableAsync(async () =>
        {
            var cible = await utilisateurs.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture))
                ?? throw new InvalidOperationException("Le compte a disparu pendant sa modification.");

            var ancienRoles = await utilisateurs.GetRolesAsync(cible);
            var maintenant = horloge.GetUtcNow();
            var etaitVerrouille = cible.LockoutEnabled && cible.LockoutEnd > maintenant;

            var change = cible.CollaborateurId != requete.CollaborateurId
                || etaitVerrouille != verrouille
                || !ancienRoles.Order().SequenceEqual(roles.Order());

            cible.CollaborateurId = requete.CollaborateurId;
            if (verrouille)
            {
                cible.LockoutEnabled = true;
                cible.LockoutEnd = DateTimeOffset.MaxValue;
            }
            else
            {
                cible.LockoutEnd = null;
                cible.AccessFailedCount = 0;
            }

            Verifier(await utilisateurs.UpdateAsync(cible));
            Verifier(await utilisateurs.RemoveFromRolesAsync(cible, ancienRoles.Except(roles)));
            Verifier(await utilisateurs.AddToRolesAsync(cible, roles.Except(ancienRoles)));

            if (change)
            {
                // Les sessions ouvertes portent les anciennes données (rôles, fiche) : le nouveau tampon de sécurité
                // les fait refuser à la prochaine validation d'Identity, et un compte verrouillé n'a plus de session.
                Verifier(await utilisateurs.UpdateSecurityStampAsync(cible));
            }

            return new CompteDto(cible.Id, cible.Email ?? "", cible.CollaborateurId, [.. roles.Order()], verrouille);
        }, cancellationToken);

        JournalComptes.CompteModifie(logger, id, courant.Identifiant, compte.CollaborateurId, compte.Verrouille);
        return TypedResults.Ok(compte);
    }

    private static string? RolesInconnus(IEnumerable<string> demandes)
    {
        var inconnus = demandes.Except(Roles.Tous).ToList();
        return inconnus.Count == 0
            ? null
            : $"Rôles inconnus : {string.Join(", ", inconnus)}. Valeurs acceptées : {string.Join(", ", Roles.Tous)}.";
    }

    // Une erreur d'Identity ici est une anomalie (les données ont été vérifiées plus haut) : exception, donc 500
    // et retour arrière de la transaction.
    private static void Verifier(IdentityResult resultat)
    {
        if (!resultat.Succeeded)
        {
            throw new InvalidOperationException(
                "Modification du compte refusée par Identity : " + string.Join(" ", resultat.Errors.Select(e => e.Code)));
        }
    }
}
