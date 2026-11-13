using System.Net;
using System.Text.Json;
using Chronos.Api.Securite;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Identite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Tests;

/// <summary>Le premier administrateur d'une installation neuve (voir <see cref="AdministrateurInitial"/>).</summary>
public class AdministrateurInitialTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    private const string Email = "premier.admin@chronos.example";
    private const string MotDePasse = "Premier-Admin-2026!";

    private static IConfiguration Reglages(string? email, string? motDePasse) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Chronos:Admin:Email"] = email,
                ["Chronos:Admin:MotDePasse"] = motDePasse,
            })
            .Build();

    // La fabrique a déjà créé les rôles et les données de démonstration : on revient à une base d'installation neuve.
    private async Task VoirUneInstallationNeuveAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var ct = TestContext.Current.CancellationToken;
        await db.Users.ExecuteDeleteAsync(ct);
        await db.Roles.ExecuteDeleteAsync(ct);
    }

    private async Task<(int Comptes, string[] Roles)> EtatAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var ct = TestContext.Current.CancellationToken;
        return (await db.Users.CountAsync(ct), await db.Roles.Select(r => r.Name!).OrderBy(n => n).ToArrayAsync(ct));
    }

    [Fact]
    public async Task Sans_reglage_ou_avec_un_reglage_incomplet_rien_n_est_cree()
    {
        await VoirUneInstallationNeuveAsync();

        await AdministrateurInitial.CreerAsync(factory.Services, Reglages(null, null));
        await AdministrateurInitial.CreerAsync(factory.Services, Reglages(Email, null));
        await AdministrateurInitial.CreerAsync(factory.Services, Reglages("", MotDePasse));

        var (comptes, roles) = await EtatAsync();
        Assert.Equal(0, comptes);
        Assert.Empty(roles);
    }

    [Fact]
    public async Task Les_roles_et_l_administrateur_sans_fiche_sont_crees_et_il_se_connecte()
    {
        var ct = TestContext.Current.CancellationToken;
        await VoirUneInstallationNeuveAsync();

        await AdministrateurInitial.CreerAsync(factory.Services, Reglages(Email, MotDePasse));

        var (comptes, roles) = await EtatAsync();
        Assert.Equal(1, comptes);
        Assert.Equal(Roles.Tous.Order(), roles);

        using var admin = await factory.SeConnecterAsync(Email, MotDePasse);
        var moi = await admin.GetFromJsonAsync<JsonElement>("/api/compte/moi", ct);
        Assert.Equal(JsonValueKind.Null, moi.GetProperty("collaborateurId").ValueKind);
        Assert.Equal(["Administrateur"], moi.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray());
    }

    [Fact]
    public async Task Les_roles_existent_quand_l_administrateur_cree_ensuite_les_comptes_des_autres()
    {
        await VoirUneInstallationNeuveAsync();
        await AdministrateurInitial.CreerAsync(factory.Services, Reglages(Email, MotDePasse));
        using var admin = await factory.SeConnecterAsync(Email, MotDePasse);

        foreach (var role in Roles.Tous)
        {
            var creation = await admin.PostAsJsonAsync("/api/comptes", new
            {
                email = $"{role.ToLowerInvariant()}@chronos.example",
                motDePasse = "Initial-Chronos-1!",
                roles = new[] { role },
            }, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        }
    }

    [Fact]
    public async Task Un_redemarrage_ne_cree_rien_de_plus_et_ne_reinitialise_pas_le_mot_de_passe()
    {
        await VoirUneInstallationNeuveAsync();
        await AdministrateurInitial.CreerAsync(factory.Services, Reglages(Email, MotDePasse));

        await AdministrateurInitial.CreerAsync(factory.Services, Reglages(Email, MotDePasse));
        await AdministrateurInitial.CreerAsync(factory.Services, Reglages("autre.admin@chronos.example", "Autre-Admin-2026!"));

        Assert.Equal(1, (await EtatAsync()).Comptes);
        using var admin = await factory.SeConnecterAsync(Email, MotDePasse);
        Assert.NotNull(admin);
    }

    [Fact]
    public async Task Quand_un_compte_existe_les_roles_manquants_sont_recrees_mais_aucun_administrateur_n_est_ajoute()
    {
        await VoirUneInstallationNeuveAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var comptes = scope.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();
            var existant = await comptes.CreateAsync(new Utilisateur { UserName = "existant@chronos.example", Email = "existant@chronos.example" }, "Existant-2026!");
            Assert.True(existant.Succeeded);
        }

        await AdministrateurInitial.CreerAsync(factory.Services, Reglages(Email, MotDePasse));

        var (comptes2, roles) = await EtatAsync();
        Assert.Equal(1, comptes2);
        Assert.Equal(Roles.Tous.Order(), roles);
    }

    [Fact]
    public async Task Un_mot_de_passe_refuse_par_Identity_arrete_le_demarrage_avec_un_message_clair()
    {
        await VoirUneInstallationNeuveAsync();

        var echec = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AdministrateurInitial.CreerAsync(factory.Services, Reglages(Email, "abc")));

        Assert.Contains("Chronos:Admin:MotDePasse", echec.Message, StringComparison.Ordinal);
        Assert.Contains("Passwords must be at least", echec.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(Email, echec.Message, StringComparison.Ordinal);
        Assert.Equal(0, (await EtatAsync()).Comptes);
    }
}
