using Chronos.Api.Securite;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Identite;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Chronos.Api.Tests;

public sealed class ChronosApiFactory(ServeurDeBase serveur) : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string MotDePasse = "Chronos-Test-2026!";

    // Les fiches des données initiales : Sophie manage Camille, Hugo est gestionnaire RH.
    public const int SophieId = 1;
    public const int CamilleId = 2;
    public const int HugoId = 3;

    private readonly string _chaineDeConnexion = serveur.ChaineDeConnexion($"chronos_{Guid.NewGuid():N}");

    public FakeTimeProvider Horloge { get; } = new(new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Chronos", _chaineDeConnexion);
        builder.UseSetting("Chronos:Fournisseur", serveur.Fournisseur);

        // Les comptes de démonstration sont créés avant que la base soit migrée : jamais dans les tests,
        // même si un mot de passe de démonstration est configuré sur le poste.
        builder.UseSetting("Chronos:Demo:MotDePasse", "");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Horloge);
        });
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var role in Roles.Tous)
        {
            var creation = await roles.CreateAsync(new IdentityRole<int>(role));
            Assert.True(creation.Succeeded, string.Join(", ", creation.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Un client qui a fait ce que fera l'interface : il s'est connecté par le vrai endpoint, a conservé le cookie,
    /// a appelé <c>/moi</c> pour obtenir le jeton anti-CSRF et le renvoie dans l'en-tête de chaque écriture.
    /// Le compte est créé au premier appel pour cette adresse.
    /// </summary>
    public async Task<HttpClient> ClientConnecteAsync(string email, int? collaborateurId, params string[] roles)
    {
        using (var scope = Services.CreateScope())
        {
            var comptes = scope.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();
            if (await comptes.FindByEmailAsync(email) is null)
            {
                var compte = new Utilisateur { UserName = email, Email = email, CollaborateurId = collaborateurId };
                var creation = await comptes.CreateAsync(compte, MotDePasse);
                Assert.True(creation.Succeeded, string.Join(", ", creation.Errors.Select(e => e.Description)));
                await comptes.AddToRolesAsync(compte, roles);
            }
        }

        return await SeConnecterAsync(email, MotDePasse);
    }

    /// <summary>Se connecte par le vrai endpoint à un compte qui existe déjà, et prépare le jeton anti-CSRF.</summary>
    public async Task<HttpClient> SeConnecterAsync(string email, string motDePasse)
    {
        var client = CreateClient();
        // useSessionCookies : l'horloge pilotée date le cookie persistant du 4 mai 2026, déjà périmé pour le client.
        var connexion = await client.PostAsJsonAsync("/api/compte/login?useSessionCookies=true",
            new { email, password = motDePasse });
        connexion.EnsureSuccessStatusCode();

        var moi = await client.GetAsync("/api/compte/moi");
        moi.EnsureSuccessStatusCode();
        var jeton = moi.Headers.GetValues("Set-Cookie")
            .Select(cookie => cookie.Split(';')[0])
            .Single(cookie => cookie.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split('=', 2)[1];
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", Uri.UnescapeDataString(jeton));

        return client;
    }

    public Task<HttpClient> ClientRhAsync() =>
        ClientConnecteAsync("hugo.bernard@chronos.example", HugoId, Roles.Collaborateur, Roles.GestionnaireRh);

    public Task<HttpClient> ClientSophieAsync() =>
        ClientConnecteAsync("sophie.martin@chronos.example", SophieId, Roles.Collaborateur, Roles.Manager);

    public Task<HttpClient> ClientCamilleAsync() =>
        ClientConnecteAsync("camille.durand@chronos.example", CamilleId, Roles.Collaborateur);

    public Task<HttpClient> ClientAdministrateurAsync() =>
        ClientConnecteAsync("admin@chronos.example", null, Roles.Administrateur);

    /// <summary>L'identifiant du compte (celui que la trace enregistre) pour une adresse.</summary>
    public async Task<string> IdentifiantDuCompteAsync(string email)
    {
        using var scope = Services.CreateScope();
        var comptes = scope.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();
        var compte = await comptes.FindByEmailAsync(email);
        return compte!.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
