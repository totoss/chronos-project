using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chronos.Api.Securite;

namespace Chronos.Api.Tests;

/// <summary>Le rôle ouvre la fonction : « rôle insuffisant → 403 », ressource par ressource.</summary>
public class AutorisationTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    // L'autorisation précède la lecture du corps : un corps vide suffit à constater le 403.
    private static readonly (string Methode, string Route)[] EcrituresReservees =
    [
        ("POST", "/api/collaborateurs"),
        ("PUT", "/api/collaborateurs/1"),
        ("DELETE", "/api/collaborateurs/1"),
        ("POST", "/api/clients"),
        ("PUT", "/api/clients/1"),
        ("DELETE", "/api/clients/1"),
        ("POST", "/api/projets"),
        ("PUT", "/api/projets/1"),
        ("POST", "/api/projets/1/demarrage"),
        ("POST", "/api/projets/1/achevement"),
        ("POST", "/api/projets/1/annulation"),
        ("POST", "/api/affectations"),
        ("PUT", "/api/affectations/1"),
        ("DELETE", "/api/affectations/1"),
        ("POST", "/api/jours-feries"),
        ("DELETE", "/api/jours-feries/2026-05-08"),
        ("POST", "/api/feuilles-de-temps/1/validation"),
        ("POST", "/api/feuilles-de-temps/1/rejet"),
        ("POST", "/api/absences/1/approbation"),
        ("POST", "/api/absences/1/refus"),
        ("POST", "/api/comptes"),
    ];

    private static Task<HttpResponseMessage> EnvoyerAsync(HttpClient client, string methode, string route) =>
        client.SendAsync(new HttpRequestMessage(new HttpMethod(methode), route) { Content = JsonContent.Create(new { }) },
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Un_simple_collaborateur_ne_peut_ecrire_nulle_part_ailleurs_que_chez_lui()
    {
        using var camille = await factory.ClientCamilleAsync();

        foreach (var (methode, route) in EcrituresReservees)
        {
            var reponse = await EnvoyerAsync(camille, methode, route);

            Assert.True(reponse.StatusCode == HttpStatusCode.Forbidden, $"{methode} {route} : {(int)reponse.StatusCode}");
        }
    }

    [Theory]
    [InlineData("POST", "/api/collaborateurs")]
    [InlineData("PUT", "/api/collaborateurs/1")]
    [InlineData("DELETE", "/api/collaborateurs/1")]
    [InlineData("POST", "/api/jours-feries")]
    [InlineData("DELETE", "/api/jours-feries/2026-05-08")]
    [InlineData("POST", "/api/comptes")]
    public async Task Un_manager_ne_gere_ni_les_fiches_ni_le_calendrier_ni_les_comptes(string methode, string route)
    {
        using var sophie = await factory.ClientSophieAsync();

        var reponse = await EnvoyerAsync(sophie, methode, route);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/collaborateurs")]
    [InlineData("PUT", "/api/collaborateurs/1")]
    [InlineData("DELETE", "/api/collaborateurs/1")]
    [InlineData("POST", "/api/jours-feries")]
    [InlineData("POST", "/api/projets")]
    [InlineData("POST", "/api/affectations")]
    [InlineData("POST", "/api/feuilles-de-temps/1/validation")]
    [InlineData("POST", "/api/absences/1/approbation")]
    public async Task L_administrateur_gere_les_comptes_pas_les_donnees_des_collaborateurs(string methode, string route)
    {
        using var admin = await factory.ClientAdministrateurAsync();

        var reponse = await EnvoyerAsync(admin, methode, route);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task Le_gestionnaire_RH_ne_gere_pas_les_comptes()
    {
        using var rh = await factory.ClientRhAsync();

        var reponse = await EnvoyerAsync(rh, "POST", "/api/comptes");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task Les_feuilles_a_valider_sont_reservees_a_ceux_qui_valident()
    {
        var ct = TestContext.Current.CancellationToken;
        using var camille = await factory.ClientCamilleAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await camille.GetAsync("/api/feuilles-de-temps/soumises", ct)).StatusCode);
    }

    [Fact]
    public async Task Un_manager_cree_un_projet_un_client_et_une_affectation_pour_son_equipe()
    {
        var ct = TestContext.Current.CancellationToken;
        using var sophie = await factory.ClientSophieAsync();

        var client = await sophie.PostAsJsonAsync("/api/clients", new { nom = "Client de Sophie SA" }, ct);
        var projet = await sophie.PostAsJsonAsync("/api/projets", new
        {
            code = "SOPHIE-1", libelle = "Projet de Sophie", nature = "Interne", dateDebut = "2026-05-04",
            budgetJours = 10, responsableId = ChronosApiFactory.SophieId,
        }, ct);
        var affectation = await sophie.PostAsJsonAsync("/api/affectations", new
        {
            collaborateurId = ChronosApiFactory.CamilleId, projetId = 2, dateDebut = "2026-07-06", dateFin = "2026-07-10",
            tauxAllocation = 0.1,
        }, ct);

        Assert.Equal(HttpStatusCode.Created, client.StatusCode);
        Assert.Equal(HttpStatusCode.Created, projet.StatusCode);
        Assert.Equal(HttpStatusCode.Created, affectation.StatusCode);
    }

    [Fact]
    public async Task Le_gestionnaire_RH_cree_une_fiche_et_un_jour_ferie_pas_un_collaborateur()
    {
        var ct = TestContext.Current.CancellationToken;
        using var camille = await factory.ClientCamilleAsync();
        using var rh = await factory.ClientRhAsync();

        var refusee = await camille.PostAsJsonAsync("/api/jours-feries", new { date = "2026-11-11", libelle = "Armistice" }, ct);
        var acceptee = await rh.PostAsJsonAsync("/api/jours-feries", new { date = "2026-11-11", libelle = "Armistice" }, ct);
        var fiche = await rh.CreerCollaborateurAsync("AUT-1");

        Assert.Equal(HttpStatusCode.Forbidden, refusee.StatusCode);
        Assert.Equal(HttpStatusCode.Created, acceptee.StatusCode);
        Assert.True(fiche.Id > 0);
    }

    [Fact]
    public async Task Sans_le_role_RH_on_ne_modifie_pas_les_informations_contractuelles_RG_52()
    {
        var ct = TestContext.Current.CancellationToken;
        using var camille = await factory.ClientCamilleAsync();
        using var sophie = await factory.ClientSophieAsync();
        var requete = new { nom = "Durand", prenom = "Camille", email = "camille.durand@chronos.example", poste = "Développeuse",
            dateSortie = "2027-01-31", version = Guid.NewGuid() };

        // Camille, l'intéressée, et Sophie, sa manager : ni l'une ni l'autre ne fixe une date de sortie.
        Assert.Equal(HttpStatusCode.Forbidden, (await camille.PutAsJsonAsync($"/api/collaborateurs/{ChronosApiFactory.CamilleId}", requete, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sophie.PutAsJsonAsync($"/api/collaborateurs/{ChronosApiFactory.CamilleId}", requete, ct)).StatusCode);
    }

    [Fact]
    public async Task L_administrateur_cree_un_compte_pour_un_collaborateur()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        var personne = await rh.CreerCollaborateurAsync("CPT-1");
        using var admin = await factory.ClientAdministrateurAsync();

        var creation = await admin.PostAsJsonAsync("/api/comptes", new
        {
            email = personne.Email, motDePasse = "Initial-Chronos-1!", collaborateurId = personne.Id,
            roles = new[] { Roles.Collaborateur, Roles.Manager },
        }, ct);

        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var compte = await creation.LireAsync();
        Assert.Equal(personne.Id, compte.GetProperty("collaborateurId").GetInt32());
        Assert.False(compte.TryGetProperty("motDePasse", out _));

        // Le compte fonctionne : la personne se connecte avec le mot de passe initial et reçoit ses rôles.
        using var client = factory.CreateClient();
        var connexion = await client.PostAsJsonAsync("/api/compte/login?useSessionCookies=true",
            new { email = personne.Email, password = "Initial-Chronos-1!" }, ct);
        Assert.Equal(HttpStatusCode.OK, connexion.StatusCode);
        var moi = await client.GetFromJsonAsync<JsonElement>("/api/compte/moi", ct);
        Assert.Equal(["Collaborateur", "Manager"], moi.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray());
    }

    [Fact]
    public async Task La_creation_d_un_compte_refuse_les_demandes_invalides()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        var personne = await rh.CreerCollaborateurAsync("CPT-2");
        using var admin = await factory.ClientAdministrateurAsync();
        object Demande(string email, string motDePasse, int? collaborateurId, string[] roles) =>
            new { email, motDePasse, collaborateurId, roles };

        var faible = await admin.PostAsJsonAsync("/api/comptes", Demande("faible@chronos.example", "abc", null, [Roles.Collaborateur]), ct);
        var roleInconnu = await admin.PostAsJsonAsync("/api/comptes", Demande("role@chronos.example", "Initial-Chronos-1!", null, ["Pirate"]), ct);
        var ficheInconnue = await admin.PostAsJsonAsync("/api/comptes", Demande("fiche@chronos.example", "Initial-Chronos-1!", 987654, [Roles.Collaborateur]), ct);
        var premier = await admin.PostAsJsonAsync("/api/comptes", Demande(personne.Email, "Initial-Chronos-1!", personne.Id, [Roles.Collaborateur]), ct);
        var memeAdresse = await admin.PostAsJsonAsync("/api/comptes", Demande(personne.Email, "Initial-Chronos-1!", null, [Roles.Collaborateur]), ct);
        var memeFiche = await admin.PostAsJsonAsync("/api/comptes", Demande("autre@chronos.example", "Initial-Chronos-1!", personne.Id, [Roles.Collaborateur]), ct);

        Assert.Equal(HttpStatusCode.BadRequest, faible.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, roleInconnu.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, ficheInconnue.StatusCode);
        Assert.Equal(HttpStatusCode.Created, premier.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, memeAdresse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, memeFiche.StatusCode);
    }
}
