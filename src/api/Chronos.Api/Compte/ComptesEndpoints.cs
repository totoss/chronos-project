using System.ComponentModel.DataAnnotations;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Identite;
using Chronos.Api.Securite;
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

public record CompteDto(int Id, string Email, int? CollaborateurId, IReadOnlyList<string> Roles);

public static class ComptesEndpoints
{
    public static RouteGroupBuilder MapComptes(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/comptes").WithTags("Comptes");

        groupe.MapPost("/", Creer).RequireAuthorization(Politiques.Administration);

        return groupe;
    }

    // L'inscription libre est neutralisée : c'est l'administrateur qui crée les comptes. Il choisit le mot de passe
    // initial, que Chronos ne renvoie jamais ; Identity en vérifie la robustesse.
    private static async Task<Results<Created<CompteDto>, ValidationProblem, ProblemHttpResult>> Creer(
        CreerCompteRequete requete, UserManager<Utilisateur> utilisateurs, ChronosDbContext db,
        CancellationToken cancellationToken)
    {
        var rolesInconnus = requete.Roles.Except(Roles.Tous).ToList();
        if (rolesInconnus.Count > 0)
        {
            return Invalide("Roles", $"Rôles inconnus : {string.Join(", ", rolesInconnus)}. Valeurs acceptées : {string.Join(", ", Roles.Tous)}.");
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

        return TypedResults.Created((string?)null, new CompteDto(compte.Id, requete.Email, requete.CollaborateurId, [.. requete.Roles.Order()]));
    }
}
