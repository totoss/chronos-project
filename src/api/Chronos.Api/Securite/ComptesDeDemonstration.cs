using Chronos.Infrastructure;
using Chronos.Infrastructure.Identite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Securite;

/// <summary>
/// Les comptes du développement : un par rôle, reliés aux fiches des données initiales par l'adresse électronique.
/// Le mot de passe vient de la configuration (secrets de développement), jamais d'un fichier versionné.
/// </summary>
public static class ComptesDeDemonstration
{
    private sealed record CompteDemo(string Email, bool AUneFiche, string[] Roles);

    private static readonly CompteDemo[] Comptes =
    [
        new("camille.durand@chronos.example", AUneFiche: true, [Roles.Collaborateur]),
        new("sophie.martin@chronos.example", AUneFiche: true, [Roles.Collaborateur, Roles.Manager]),
        new("hugo.bernard@chronos.example", AUneFiche: true, [Roles.Collaborateur, Roles.GestionnaireRh]),
        new("admin@chronos.example", AUneFiche: false, [Roles.Administrateur]),
    ];

    public static async Task CreerAsync(IServiceProvider services, string motDePasse)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var utilisateurs = scope.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();

        foreach (var role in Roles.Tous)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                Verifier(await roles.CreateAsync(new IdentityRole<int>(role)));
            }
        }

        foreach (var demo in Comptes)
        {
            if (await utilisateurs.FindByEmailAsync(demo.Email) is not null)
            {
                continue;
            }

            int? collaborateurId = null;
            if (demo.AUneFiche)
            {
                collaborateurId = await db.Collaborateurs
                    .Where(c => c.Email == demo.Email)
                    .Select(c => (int?)c.Id)
                    .FirstOrDefaultAsync();
                if (collaborateurId is null)
                {
                    throw new InvalidOperationException(
                        $"Aucune fiche ne porte l'adresse {demo.Email} : appliquez les migrations pour créer les données initiales.");
                }
            }

            var compte = new Utilisateur { UserName = demo.Email, Email = demo.Email, CollaborateurId = collaborateurId };
            Verifier(await utilisateurs.CreateAsync(compte, motDePasse));
            Verifier(await utilisateurs.AddToRolesAsync(compte, demo.Roles));
        }
    }

    private static void Verifier(IdentityResult resultat)
    {
        if (!resultat.Succeeded)
        {
            throw new InvalidOperationException(
                "Création d'un compte de démonstration refusée : " +
                string.Join(" ", resultat.Errors.Select(e => e.Description)));
        }
    }
}
