using Chronos.Infrastructure.Identite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Securite;

/// <summary>
/// Le premier administrateur d'une installation neuve. Sans lui, personne ne pourrait se connecter ni créer
/// de compte : l'inscription libre est neutralisée et les comptes de démonstration n'existent qu'en développement.
///
/// Il n'est créé que si <c>Chronos:Admin:Email</c> et <c>Chronos:Admin:MotDePasse</c> sont <em>tous deux</em>
/// configurés (en conteneur, le mot de passe vient d'un secret), et seulement si aucun compte n'existe encore :
/// un redémarrage, ou un réglage oublié dans la configuration, ne recrée ni ne réinitialise jamais rien.
/// Aucune valeur par défaut : une adresse ou un mot de passe connus d'avance seraient une porte ouverte.
/// </summary>
public static partial class AdministrateurInitial
{
    public static async Task CreerAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["Chronos:Admin:Email"];
        var motDePasse = configuration["Chronos:Admin:MotDePasse"];
        var aEmail = !string.IsNullOrWhiteSpace(email);
        var aMotDePasse = !string.IsNullOrWhiteSpace(motDePasse);

        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdministrateurInitial));

        if (!aEmail && !aMotDePasse)
        {
            return;
        }
        if (!aEmail || !aMotDePasse)
        {
            ReglageIncomplet(logger);
            return;
        }

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var utilisateurs = scope.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();

        // Les rôles d'abord : ils doivent exister pour que l'administrateur puisse en attribuer ensuite.
        foreach (var role in Roles.Tous)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                Verifier(await roles.CreateAsync(new IdentityRole<int>(role)), $"la création du rôle {role}");
                RoleCree(logger, role);
            }
        }

        if (await utilisateurs.Users.AnyAsync())
        {
            CompteDejaPresent(logger);
            return;
        }

        var administrateur = new Utilisateur { UserName = email, Email = email };
        Verifier(await utilisateurs.CreateAsync(administrateur, motDePasse!), "la création du premier administrateur");

        var attribution = await utilisateurs.AddToRoleAsync(administrateur, Roles.Administrateur);
        if (!attribution.Succeeded)
        {
            // Un compte ne reste jamais sans son rôle : on le retire avant de signaler l'échec.
            await utilisateurs.DeleteAsync(administrateur);
            Verifier(attribution, "l'attribution du rôle Administrateur");
        }

        AdministrateurCree(logger, administrateur.Id);
    }

    // Le message cite les codes d'erreur, et la description seulement pour le mot de passe : les autres
    // descriptions d'Identity répètent l'adresse électronique, qui n'a pas sa place dans un journal.
    private static void Verifier(IdentityResult resultat, string operation)
    {
        if (resultat.Succeeded)
        {
            return;
        }

        var details = resultat.Errors.Select(e =>
            e.Code.StartsWith("Password", StringComparison.Ordinal) ? e.Description : e.Code);
        throw new InvalidOperationException(
            $"Démarrage impossible : {operation} a été refusée par Identity ({string.Join(" ", details)}). " +
            "Vérifiez Chronos:Admin:Email et Chronos:Admin:MotDePasse.");
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Warning,
        Message = "Chronos:Admin est incomplet : l'adresse et le mot de passe doivent être configurés ensemble. Aucun administrateur n'est créé.")]
    private static partial void ReglageIncomplet(ILogger logger);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Rôle {Role} créé")]
    private static partial void RoleCree(ILogger logger, string role);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information,
        Message = "Des comptes existent déjà : le premier administrateur n'est pas créé")]
    private static partial void CompteDejaPresent(ILogger logger);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Information, Message = "Premier administrateur créé (compte {CompteId})")]
    private static partial void AdministrateurCree(ILogger logger, int compteId);
}
