using System.Net;
using System.Text.Json;
using Chronos.Api.Securite;

namespace Chronos.Api.Tests;

/// <summary>La gestion des comptes par l'administrateur : lister, rattacher une fiche, changer les rôles, verrouiller.</summary>
public class ComptesTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    private const string MotDePasseInitial = "Initial-Chronos-1!";

    private static async Task<int> CreerCompteAsync(HttpClient admin, string email, int? collaborateurId, params string[] roles)
    {
        var reponse = await admin.PostAsJsonAsync("/api/comptes",
            new { email, motDePasse = MotDePasseInitial, collaborateurId, roles }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.LireAsync()).GetProperty("id").GetInt32();
    }

    private static Task<HttpResponseMessage> ModifierAsync(
        HttpClient admin, int id, int? collaborateurId, string[] roles, bool? verrouille = false) =>
        admin.PutAsJsonAsync($"/api/comptes/{id}", new { collaborateurId, roles, verrouille }, TestContext.Current.CancellationToken);

    [Fact]
    public async Task La_liste_donne_pour_chaque_compte_l_adresse_les_roles_la_fiche_et_le_verrouillage_sans_aucun_secret()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        var personne = await rh.CreerCollaborateurAsync("LST-1");
        using var admin = await factory.ClientAdministrateurAsync();
        await CreerCompteAsync(admin, personne.Email, personne.Id, Roles.Collaborateur, Roles.Manager);

        var reponse = await admin.GetAsync("/api/comptes", ct);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var comptes = (await reponse.LireAsync()).EnumerateArray().ToList();
        var compte = comptes.Single(c => c.GetProperty("email").GetString() == personne.Email);
        Assert.Equal(personne.Id, compte.GetProperty("collaborateurId").GetInt32());
        Assert.Equal(["Collaborateur", "Manager"], compte.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray());
        Assert.False(compte.GetProperty("verrouille").GetBoolean());
        Assert.Equal(["collaborateurId", "email", "id", "roles", "verrouille"],
            compte.EnumerateObject().Select(p => p.Name).Order().ToArray());

        var administrateur = comptes.Single(c => c.GetProperty("email").GetString() == "admin@chronos.example");
        Assert.Equal(JsonValueKind.Null, administrateur.GetProperty("collaborateurId").ValueKind);
    }

    [Fact]
    public async Task Seul_l_administrateur_liste_et_modifie_les_comptes()
    {
        var ct = TestContext.Current.CancellationToken;
        foreach (var client in new[] { await factory.ClientRhAsync(), await factory.ClientSophieAsync(), await factory.ClientCamilleAsync() })
        {
            using (client)
            {
                Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/comptes", ct)).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await ModifierAsync(client, 1, null, [Roles.Collaborateur])).StatusCode);
            }
        }

        using var anonyme = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonyme.GetAsync("/api/comptes", ct)).StatusCode);
    }

    [Fact]
    public async Task Une_modification_sans_jeton_anti_CSRF_est_refusee()
    {
        using var admin = await factory.ClientAdministrateurAsync();
        var id = await CreerCompteAsync(admin, "csrf@chronos.example", null, Roles.Collaborateur);
        admin.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var reponse = await ModifierAsync(admin, id, null, [Roles.Manager]);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Le_gestionnaire_RH_sans_fiche_obtient_la_sienne_et_peut_alors_saisir()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.ClientAdministrateurAsync();
        var idCompte = await CreerCompteAsync(admin, "rh.sans.fiche@chronos.example", null, Roles.Collaborateur, Roles.GestionnaireRh);

        // Sans fiche, son compte ne peut pas ouvrir de feuille de temps.
        using (var avant = await factory.SeConnecterAsync("rh.sans.fiche@chronos.example", MotDePasseInitial))
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await avant.GetAsync("/api/feuilles-de-temps?semaine=2026-05-04", ct)).StatusCode);

            // Il crée sa propre fiche (c'est sa fonction), puis l'administrateur la rattache à son compte.
            var fiche = await avant.CreerCollaborateurAsync("RH-NEUF");
            var rattachement = await ModifierAsync(admin, idCompte, fiche.Id, [Roles.Collaborateur, Roles.GestionnaireRh]);
            Assert.Equal(HttpStatusCode.OK, rattachement.StatusCode);
            Assert.Equal(fiche.Id, (await rattachement.LireAsync()).GetProperty("collaborateurId").GetInt32());

            // Les rôles ou la fiche ont changé : la session ouverte avant est refusée à la prochaine validation d'Identity.
            // Une nouvelle connexion porte déjà la fiche.
            using var apres = await factory.SeConnecterAsync("rh.sans.fiche@chronos.example", MotDePasseInitial);
            var moi = await apres.GetFromJsonAsync<JsonElement>("/api/compte/moi", ct);
            Assert.Equal(fiche.Id, moi.GetProperty("collaborateurId").GetInt32());
            Assert.Equal(HttpStatusCode.OK, (await apres.GetAsync("/api/feuilles-de-temps?semaine=2026-05-04", ct)).StatusCode);
        }
    }

    [Fact]
    public async Task Les_roles_sont_remplaces_et_la_fiche_peut_etre_detachee()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        var personne = await rh.CreerCollaborateurAsync("ROL-1");
        using var admin = await factory.ClientAdministrateurAsync();
        var id = await CreerCompteAsync(admin, personne.Email, personne.Id, Roles.Collaborateur);

        var promotion = await ModifierAsync(admin, id, personne.Id, [Roles.Manager, Roles.Collaborateur, Roles.Manager]);
        Assert.Equal(HttpStatusCode.OK, promotion.StatusCode);
        Assert.Equal(["Collaborateur", "Manager"],
            (await promotion.LireAsync()).GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray());

        var detachement = await ModifierAsync(admin, id, null, [Roles.Collaborateur]);
        Assert.Equal(HttpStatusCode.OK, detachement.StatusCode);

        using var client = await factory.SeConnecterAsync(personne.Email, MotDePasseInitial);
        var moi = await client.GetFromJsonAsync<JsonElement>("/api/compte/moi", ct);
        Assert.Equal(JsonValueKind.Null, moi.GetProperty("collaborateurId").ValueKind);
        Assert.Equal(["Collaborateur"], moi.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray());
    }

    [Fact]
    public async Task Une_fiche_ne_peut_appartenir_qu_a_un_seul_compte()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        var personne = await rh.CreerCollaborateurAsync("FIC-1");
        using var admin = await factory.ClientAdministrateurAsync();
        await CreerCompteAsync(admin, personne.Email, personne.Id, Roles.Collaborateur);
        var autre = await CreerCompteAsync(admin, "intrus.fiche@chronos.example", null, Roles.Collaborateur);

        var reponse = await ModifierAsync(admin, autre, personne.Id, [Roles.Collaborateur]);

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
        Assert.Equal("application/problem+json", reponse.Content.Headers.ContentType?.MediaType);
        var liste = await admin.GetFromJsonAsync<JsonElement>("/api/comptes", ct);
        var compte = liste.EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == autre);
        Assert.Equal(JsonValueKind.Null, compte.GetProperty("collaborateurId").ValueKind);
    }

    [Fact]
    public async Task Les_demandes_invalides_sont_refusees_avec_les_cles_habituelles()
    {
        using var admin = await factory.ClientAdministrateurAsync();
        var id = await CreerCompteAsync(admin, "invalide@chronos.example", null, Roles.Collaborateur);

        var roleInconnu = await ModifierAsync(admin, id, null, ["Pirate"]);
        var ficheInconnue = await ModifierAsync(admin, id, 987654, [Roles.Collaborateur]);
        var sansRole = await ModifierAsync(admin, id, null, []);
        var sansVerrouillage = await ModifierAsync(admin, id, null, [Roles.Collaborateur], verrouille: null);
        var inconnu = await ModifierAsync(admin, 987654, null, [Roles.Collaborateur]);

        Assert.Equal(HttpStatusCode.BadRequest, roleInconnu.StatusCode);
        Assert.True((await roleInconnu.LireAsync()).GetProperty("errors").TryGetProperty("Roles", out _));
        Assert.Equal(HttpStatusCode.BadRequest, ficheInconnue.StatusCode);
        Assert.True((await ficheInconnue.LireAsync()).GetProperty("errors").TryGetProperty("CollaborateurId", out _));
        Assert.Equal(HttpStatusCode.BadRequest, sansRole.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sansVerrouillage.StatusCode);
        Assert.True((await sansVerrouillage.LireAsync()).GetProperty("errors").TryGetProperty("Verrouille", out _));
        Assert.Equal(HttpStatusCode.NotFound, inconnu.StatusCode);
    }

    [Fact]
    public async Task Un_compte_verrouille_ne_se_connecte_plus_et_se_deverrouille()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.ClientAdministrateurAsync();
        var id = await CreerCompteAsync(admin, "verrou@chronos.example", null, Roles.Collaborateur);

        var verrouillage = await ModifierAsync(admin, id, null, [Roles.Collaborateur], verrouille: true);
        Assert.Equal(HttpStatusCode.OK, verrouillage.StatusCode);
        Assert.True((await verrouillage.LireAsync()).GetProperty("verrouille").GetBoolean());

        using var client = factory.CreateClient();
        var refus = await client.PostAsJsonAsync("/api/compte/login?useSessionCookies=true",
            new { email = "verrou@chronos.example", password = MotDePasseInitial }, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, refus.StatusCode);

        var liste = await admin.GetFromJsonAsync<JsonElement>("/api/comptes", ct);
        Assert.True(liste.EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == id).GetProperty("verrouille").GetBoolean());

        Assert.Equal(HttpStatusCode.OK, (await ModifierAsync(admin, id, null, [Roles.Collaborateur], verrouille: false)).StatusCode);
        var accepte = await client.PostAsJsonAsync("/api/compte/login?useSessionCookies=true",
            new { email = "verrou@chronos.example", password = MotDePasseInitial }, ct);
        Assert.Equal(HttpStatusCode.OK, accepte.StatusCode);
    }

    [Fact]
    public async Task Un_administrateur_ne_se_verrouille_pas_et_ne_se_retire_pas_son_role()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.ClientAdministrateurAsync();
        var moi = int.Parse(await factory.IdentifiantDuCompteAsync("admin@chronos.example"), System.Globalization.CultureInfo.InvariantCulture);

        var verrouillage = await ModifierAsync(admin, moi, null, [Roles.Administrateur], verrouille: true);
        var retrait = await ModifierAsync(admin, moi, null, [Roles.Collaborateur], verrouille: false);

        Assert.Equal(HttpStatusCode.Conflict, verrouillage.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, retrait.StatusCode);
        Assert.Equal("application/problem+json", retrait.Content.Headers.ContentType?.MediaType);

        // Rien n'a changé : il peut toujours administrer.
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/comptes", ct)).StatusCode);

        // Un autre administrateur, en revanche, peut être modifié par lui.
        var autre = await CreerCompteAsync(admin, "autre.admin@chronos.example", null, Roles.Administrateur);
        Assert.Equal(HttpStatusCode.OK, (await ModifierAsync(admin, autre, null, [Roles.Collaborateur], verrouille: true)).StatusCode);
    }

    [Fact]
    public async Task La_session_ouverte_d_un_compte_verrouille_est_refusee_apres_l_intervalle_de_validation()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.ClientAdministrateurAsync();
        var id = await CreerCompteAsync(admin, "session.verrou@chronos.example", null, Roles.Collaborateur);
        using var session = await factory.SeConnecterAsync("session.verrou@chronos.example", MotDePasseInitial);
        Assert.Equal(HttpStatusCode.OK, (await session.GetAsync("/api/compte/moi", ct)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await ModifierAsync(admin, id, null, [Roles.Collaborateur], verrouille: true)).StatusCode);
        factory.Horloge.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("/api/compte/moi", ct)).StatusCode);
    }

    [Fact]
    public async Task La_session_ouverte_avant_un_changement_de_roles_est_refusee_apres_l_intervalle_de_validation()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.ClientAdministrateurAsync();
        var id = await CreerCompteAsync(admin, "session.roles@chronos.example", null, Roles.Collaborateur);
        using var session = await factory.SeConnecterAsync("session.roles@chronos.example", MotDePasseInitial);

        Assert.Equal(HttpStatusCode.OK, (await ModifierAsync(admin, id, null, [Roles.Collaborateur, Roles.Manager])).StatusCode);
        factory.Horloge.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal(HttpStatusCode.Unauthorized, (await session.GetAsync("/api/compte/moi", ct)).StatusCode);
    }
}
