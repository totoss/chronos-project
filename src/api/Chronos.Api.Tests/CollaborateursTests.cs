using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chronos.Domain;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chronos.Api.Tests;

public class CollaborateursTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync() => _client = await factory.ClientRhAsync();

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    private static object NouveauCollaborateur(string matricule) => new
    {
        matricule,
        nom = "Martin",
        prenom = "Camille",
        email = $"{matricule.ToLowerInvariant()}@chronos.example",
        categorie = "Consultant",
        poste = "Développeuse",
        dateEntree = "2024-09-02",
        typeContrat = "Cdi",
        quotite = 1,
    };

    [Fact]
    public async Task Creer_puis_relire_un_collaborateur()
    {
        var ct = TestContext.Current.CancellationToken;

        var creation = await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0001"), ct);

        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        Assert.NotNull(creation.Headers.Location);

        var lecture = await _client.GetAsync(creation.Headers.Location, ct);
        Assert.Equal(HttpStatusCode.OK, lecture.StatusCode);

        using var json = JsonDocument.Parse(await lecture.Content.ReadAsStringAsync(ct));
        Assert.Equal("C-0001", json.RootElement.GetProperty("matricule").GetString());
        Assert.Equal("Consultant", json.RootElement.GetProperty("categorie").GetString());
        Assert.Equal("2024-09-02", json.RootElement.GetProperty("dateEntree").GetString());
    }

    [Fact]
    public async Task Une_requete_invalide_renvoie_400_avec_les_erreurs_par_champ()
    {
        var ct = TestContext.Current.CancellationToken;
        var requete = new { matricule = "", nom = "Martin", prenom = "Camille", email = "pas-une-adresse",
            categorie = "Consultant", poste = "Développeuse", dateEntree = "2024-09-02",
            typeContrat = "Cdi", quotite = 1 };

        var reponse = await _client.PostAsJsonAsync("/api/collaborateurs", requete, ct);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal("application/problem+json", reponse.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(ct));
        var erreurs = json.RootElement.GetProperty("errors");
        Assert.True(erreurs.TryGetProperty("Matricule", out _));
        Assert.True(erreurs.TryGetProperty("Email", out _));
    }

    [Fact]
    public async Task Un_matricule_deja_utilise_renvoie_409()
    {
        var ct = TestContext.Current.CancellationToken;
        var premier = await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0002"), ct);
        Assert.Equal(HttpStatusCode.Created, premier.StatusCode);

        var doublon = await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0002"), ct);

        Assert.Equal(HttpStatusCode.Conflict, doublon.StatusCode);
        var probleme = await doublon.LireAsync();
        Assert.Equal("RG-01", probleme.GetProperty("regle").GetString());
        Assert.False(probleme.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task Une_adresse_deja_utilisee_a_la_modification_renvoie_409_avec_la_regle_RG_01()
    {
        var ct = TestContext.Current.CancellationToken;
        var premier = await _client.CreerCollaborateurAsync("C-0020");
        var second = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/collaborateurs/{(await _client.CreerCollaborateurAsync("C-0021")).Id}", ct);

        var modification = await _client.PutAsJsonAsync($"/api/collaborateurs/{second.GetProperty("id").GetInt32()}",
            new { nom = "Martin", prenom = "Camille", email = premier.Email, poste = "Développeuse",
                version = second.GetProperty("version").GetGuid() }, ct);

        Assert.Equal(HttpStatusCode.Conflict, modification.StatusCode);
        var probleme = await modification.LireAsync();
        Assert.Equal("RG-01", probleme.GetProperty("regle").GetString());
        Assert.False(probleme.TryGetProperty("code", out _));
    }

    [Fact]
    public async Task Une_modification_faite_avec_une_version_perimee_renvoie_409_avec_le_code_concurrence()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = (await _client.CreerCollaborateurAsync("C-0022")).Id;
        var fiche = await _client.GetFromJsonAsync<JsonElement>($"/api/collaborateurs/{id}", ct);
        var modification = new { nom = "Martin", prenom = "Camille", email = "c-0022@chronos.example", poste = "Lead",
            version = fiche.GetProperty("version").GetGuid() };

        var premiere = await _client.PutAsJsonAsync($"/api/collaborateurs/{id}", modification, ct);
        var seconde = await _client.PutAsJsonAsync($"/api/collaborateurs/{id}", modification, ct);

        Assert.Equal(HttpStatusCode.OK, premiere.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, seconde.StatusCode);
        var probleme = await seconde.LireAsync();
        Assert.Equal("concurrence", probleme.GetProperty("code").GetString());
        Assert.False(probleme.TryGetProperty("regle", out _));
    }

    [Fact]
    public async Task Un_champ_obligatoire_manquant_a_la_modification_donne_un_message_en_francais()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = (await _client.CreerCollaborateurAsync("C-0023")).Id;

        var reponse = await _client.PutAsJsonAsync($"/api/collaborateurs/{id}",
            new { prenom = "Camille", email = "c-0023@chronos.example", poste = "Lead", version = Guid.NewGuid() }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        var erreurs = (await reponse.LireAsync()).GetProperty("errors");
        Assert.Equal("Le nom est obligatoire.", erreurs.GetProperty("Nom")[0].GetString());
    }

    [Fact]
    public async Task L_index_unique_refuse_un_doublon_meme_sans_passer_par_l_API()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var entree = new DateOnly(2024, 9, 2);
        db.Collaborateurs.Add(new Collaborateur("C-0003", "A", "A", "a@chronos.example", CategorieCollaborateur.Interne, "RH", entree));
        db.Collaborateurs.Add(new Collaborateur("C-0003", "B", "B", "b@chronos.example", CategorieCollaborateur.Interne, "RH", entree));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(ct));
    }

    [Fact]
    public async Task Le_filtre_sorti_depend_de_la_date_du_jour()
    {
        var ct = TestContext.Current.CancellationToken;
        var creation = await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0004"), ct);
        var cree = await creation.Content.ReadFromJsonAsync<JsonElement>(ct);

        // La création n'accepte pas de date de sortie : on la renseigne par une modification.
        var modification = await _client.PutAsJsonAsync($"/api/collaborateurs/{cree.GetProperty("id").GetInt32()}",
            new { nom = "Martin", prenom = "Camille", email = "c-0004@chronos.example", poste = "Développeuse",
                dateSortie = "2026-05-15", version = cree.GetProperty("version").GetGuid() }, ct);
        Assert.Equal(HttpStatusCode.OK, modification.StatusCode);

        var avant = await _client.GetFromJsonAsync<JsonElement>("/api/collaborateurs?statut=sorti", ct);
        Assert.DoesNotContain(avant.GetProperty("elements").EnumerateArray(),
            c => c.GetProperty("matricule").GetString() == "C-0004");

        factory.Horloge.Advance(TimeSpan.FromDays(30));
        // L'horloge pilote aussi la session, qui dure huit heures : on se reconnecte.
        _client = await factory.ClientRhAsync();

        var apres = await _client.GetFromJsonAsync<JsonElement>("/api/collaborateurs?statut=sorti", ct);
        Assert.Contains(apres.GetProperty("elements").EnumerateArray(),
            c => c.GetProperty("matricule").GetString() == "C-0004");
    }

    // Crée un collaborateur de l'équipe de Sophie, avec la date de sortie donnée (null : aucune), et renvoie son id.
    // La création n'accepte pas de date de sortie : on la renseigne par une modification.
    private async Task<int> CreerAvecSortieAsync(string matricule, int decalageDeLaSortieEnJours, bool avecSortie)
    {
        var ct = TestContext.Current.CancellationToken;
        var creation = await _client.PostAsJsonAsync("/api/collaborateurs",
            new { matricule, nom = "Martin", prenom = "Camille", email = $"{matricule.ToLowerInvariant()}@chronos.example",
                categorie = "Consultant", poste = "Développeuse", dateEntree = "2024-09-02",
                typeContrat = "Cdi", quotite = 1, managerId = ChronosApiFactory.SophieId }, ct);
        var cree = await creation.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = cree.GetProperty("id").GetInt32();

        if (avecSortie)
        {
            var aujourdhui = DateOnly.FromDateTime(factory.Horloge.GetLocalNow().DateTime);
            var sortie = aujourdhui.AddDays(decalageDeLaSortieEnJours);
            var modification = await _client.PutAsJsonAsync($"/api/collaborateurs/{id}",
                new { nom = "Martin", prenom = "Camille", email = $"{matricule.ToLowerInvariant()}@chronos.example",
                    poste = "Développeuse", managerId = ChronosApiFactory.SophieId,
                    dateSortie = sortie.ToString("yyyy-MM-dd"), version = cree.GetProperty("version").GetGuid() }, ct);
            Assert.Equal(HttpStatusCode.OK, modification.StatusCode);
        }

        return id;
    }

    [Fact]
    public async Task Un_collaborateur_dont_la_sortie_est_passee_est_marque_sorti_dans_la_liste_et_dans_la_fiche()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreerAvecSortieAsync("C-0007", -10, avecSortie: true);

        var liste = await _client.GetFromJsonAsync<JsonElement>("/api/collaborateurs?statut=sorti&recherche=C-0007", ct);
        var fiche = await _client.GetFromJsonAsync<JsonElement>($"/api/collaborateurs/{id}", ct);

        var element = Assert.Single(liste.GetProperty("elements").EnumerateArray());
        Assert.True(element.GetProperty("estSorti").GetBoolean());
        Assert.True(fiche.GetProperty("estSorti").GetBoolean());
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(true, 10)]
    public async Task Sans_sortie_ou_avec_une_sortie_aujourd_hui_ou_a_venir_le_collaborateur_n_est_pas_sorti(
        bool avecSortie, int decalageEnJours)
    {
        var ct = TestContext.Current.CancellationToken;
        var matricule = $"C-08{(avecSortie ? 1 : 0)}{decalageEnJours:00}";
        var id = await CreerAvecSortieAsync(matricule, decalageEnJours, avecSortie);

        var liste = await _client.GetFromJsonAsync<JsonElement>($"/api/collaborateurs?recherche={matricule}", ct);
        var fiche = await _client.GetFromJsonAsync<JsonElement>($"/api/collaborateurs/{id}", ct);

        var element = Assert.Single(liste.GetProperty("elements").EnumerateArray());
        Assert.False(element.GetProperty("estSorti").GetBoolean());
        Assert.False(fiche.GetProperty("estSorti").GetBoolean());
    }

    [Fact]
    public async Task La_fiche_reduite_d_un_collaborateur_sorti_porte_estSorti_sans_aucune_date()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = await CreerAvecSortieAsync("C-0009", -10, avecSortie: true);
        using var sophie = await factory.ClientSophieAsync();

        var fiche = await sophie.GetFromJsonAsync<JsonElement>($"/api/collaborateurs/{id}", ct);

        Assert.True(fiche.GetProperty("estSorti").GetBoolean());
        Assert.False(fiche.TryGetProperty("dateEntree", out _));
        Assert.False(fiche.TryGetProperty("dateSortie", out _));
        Assert.False(fiche.TryGetProperty("quotite", out _));
    }

    [Fact]
    public async Task Supprimer_un_collaborateur_qui_a_une_affectation_l_archive()
    {
        var ct = TestContext.Current.CancellationToken;
        var creation = await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0005"), ct);
        var cree = await creation.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = cree.GetProperty("id").GetInt32();
        // Le projet 1 (BDL-REFONTE) vient des données initiales.
        var affectation = await _client.PostAsJsonAsync("/api/affectations",
            new { collaborateurId = id, projetId = 1, dateDebut = "2026-05-04", dateFin = "2026-05-08", tauxAllocation = 0.5 }, ct);
        Assert.Equal(HttpStatusCode.Created, affectation.StatusCode);

        var suppression = await _client.DeleteAsync($"/api/collaborateurs/{id}", ct);

        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/collaborateurs/{id}", ct)).StatusCode);
        var liste = await _client.GetFromJsonAsync<JsonElement>("/api/collaborateurs?recherche=C-0005", ct);
        Assert.Equal(0, liste.GetProperty("total").GetInt32());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var ligne = await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .SingleAsync(c => c.Id == id, ct);
        Assert.True(ligne.EstArchive);
    }

    [Fact]
    public async Task Supprimer_un_collaborateur_sans_donnees_liees_le_supprime_vraiment()
    {
        var ct = TestContext.Current.CancellationToken;
        var creation = await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0006"), ct);
        var cree = await creation.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = cree.GetProperty("id").GetInt32();

        var suppression = await _client.DeleteAsync($"/api/collaborateurs/{id}", ct);

        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        Assert.False(await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .AnyAsync(c => c.Id == id, ct));
    }

    private const string Semaine = "2026-05-11";

    // Un collaborateur de l'équipe de Sophie dont la feuille de la semaine contient une saisie. L'affectation qui a
    // permis la saisie est retirée ensuite : la feuille est alors la seule donnée liée au collaborateur.
    private async Task<(int Id, string Email)> CreerAvecUneSaisieAsync(string matricule)
    {
        var ct = TestContext.Current.CancellationToken;
        var personne = await _client.CreerCollaborateurAsync(matricule, ChronosApiFactory.SophieId);
        var affectation = await _client.PostAsJsonAsync("/api/affectations",
            new { collaborateurId = personne.Id, projetId = 1, dateDebut = Semaine, dateFin = "2026-05-15", tauxAllocation = 0.5 }, ct);
        Assert.Equal(HttpStatusCode.Created, affectation.StatusCode);

        // Seul le propriétaire saisit : on se connecte avec le compte de la fiche.
        using var proprietaire = await factory.ConnecterAsync(personne);
        var feuille = await proprietaire.GetFromJsonAsync<JsonElement>($"/api/feuilles-de-temps?semaine={Semaine}", ct);
        var saisie = await proprietaire.PutAsJsonAsync($"/api/feuilles-de-temps/{feuille.GetProperty("id").GetInt32()}/saisies",
            new { saisies = new[] { new { projetId = 1, jour = "2026-05-12", duree = 0.5 } } }, ct);
        Assert.Equal(HttpStatusCode.OK, saisie.StatusCode);

        var idAffectation = (await affectation.LireAsync()).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/affectations/{idAffectation}", ct)).StatusCode);
        return personne;
    }

    private async Task<bool> ADesFeuillesEnBaseAsync(int collaborateurId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        return await db.FeuillesDeTemps.AnyAsync(f => f.CollaborateurId == collaborateurId, TestContext.Current.CancellationToken);
    }

    private async Task<bool> EstArchiveAsync(int collaborateurId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var ligne = await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .SingleAsync(c => c.Id == collaborateurId, TestContext.Current.CancellationToken);
        return ligne.EstArchive;
    }

    [Fact]
    public async Task Supprimer_un_collaborateur_dont_les_seules_traces_sont_des_feuilles_vides_le_supprime_vraiment()
    {
        var ct = TestContext.Current.CancellationToken;
        var personne = await _client.CreerCollaborateurAsync("C-0030");
        // Le gestionnaire RH lit deux semaines : le GET crée deux feuilles, jamais remplies.
        foreach (var semaine in new[] { Semaine, "2026-05-18" })
        {
            var reponse = await _client.GetAsync($"/api/feuilles-de-temps?collaborateurId={personne.Id}&semaine={semaine}", ct);
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        }
        Assert.True(await ADesFeuillesEnBaseAsync(personne.Id));

        var suppression = await _client.DeleteAsync($"/api/collaborateurs/{personne.Id}", ct);

        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/collaborateurs/{personne.Id}", ct)).StatusCode);
        Assert.False(await ADesFeuillesEnBaseAsync(personne.Id));
    }

    [Fact]
    public async Task Supprimer_un_collaborateur_qui_a_une_feuille_avec_une_saisie_l_archive()
    {
        var ct = TestContext.Current.CancellationToken;
        var personne = await CreerAvecUneSaisieAsync("C-0031");

        var suppression = await _client.DeleteAsync($"/api/collaborateurs/{personne.Id}", ct);

        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);
        Assert.True(await EstArchiveAsync(personne.Id));
        Assert.True(await ADesFeuillesEnBaseAsync(personne.Id));
    }

    [Fact]
    public async Task Supprimer_un_collaborateur_qui_a_une_feuille_soumise_l_archive()
    {
        var ct = TestContext.Current.CancellationToken;
        var personne = await _client.CreerCollaborateurAsync("C-0032");
        using var proprietaire = await factory.ConnecterAsync(personne);
        var feuille = await proprietaire.GetFromJsonAsync<JsonElement>($"/api/feuilles-de-temps?semaine={Semaine}", ct);
        var soumission = await proprietaire.PostAsync(
            $"/api/feuilles-de-temps/{feuille.GetProperty("id").GetInt32()}/soumission", null, ct);
        Assert.Equal(HttpStatusCode.OK, soumission.StatusCode);

        var suppression = await _client.DeleteAsync($"/api/collaborateurs/{personne.Id}", ct);

        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);
        Assert.True(await EstArchiveAsync(personne.Id));
    }

    [Fact]
    public async Task Supprimer_un_collaborateur_qui_a_une_feuille_rejetee_l_archive()
    {
        var ct = TestContext.Current.CancellationToken;
        var personne = await _client.CreerCollaborateurAsync("C-0033", ChronosApiFactory.SophieId);
        using var proprietaire = await factory.ConnecterAsync(personne);
        var feuille = await proprietaire.GetFromJsonAsync<JsonElement>($"/api/feuilles-de-temps?semaine={Semaine}", ct);
        var id = feuille.GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await proprietaire.PostAsync($"/api/feuilles-de-temps/{id}/soumission", null, ct)).StatusCode);
        // La feuille revient au statut brouillon, avec son motif : elle n'est plus vide.
        using var sophie = await factory.ClientSophieAsync();
        var rejet = await sophie.PostAsJsonAsync($"/api/feuilles-de-temps/{id}/rejet", new { motif = "Semaine incomplète." }, ct);
        Assert.Equal(HttpStatusCode.OK, rejet.StatusCode);

        var suppression = await _client.DeleteAsync($"/api/collaborateurs/{personne.Id}", ct);

        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);
        Assert.True(await EstArchiveAsync(personne.Id));
    }
}
