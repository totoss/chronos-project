using System.Net;
using System.Text.Json;
using Chronos.Api.Securite;
using Chronos.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Tests;

/// <summary>RG-50 et RG-51 : une hiérarchie A → B → C et une personne D sans lien avec elle.</summary>
public class PerimetreTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>, IAsyncLifetime
{
    private static int _sequence;

    private HttpClient _rh = null!;
    private string _prefixe = "";
    private (int Id, string Email) _a, _b, _c, _d;
    private HttpClient _clientA = null!, _clientB = null!, _clientC = null!;

    public async ValueTask InitializeAsync()
    {
        _rh = await factory.ClientRhAsync();
        // Les tests partagent une base : chacun crée ses propres personnes, aux matricules uniques.
        _prefixe = $"P{Interlocked.Increment(ref _sequence)}-";
        _a = await _rh.CreerCollaborateurAsync(_prefixe + "A");
        _b = await _rh.CreerCollaborateurAsync(_prefixe + "B", _a.Id);
        _c = await _rh.CreerCollaborateurAsync(_prefixe + "C", _b.Id);
        _d = await _rh.CreerCollaborateurAsync(_prefixe + "D");
        _clientA = await factory.ConnecterAsync(_a, Roles.Collaborateur, Roles.Manager);
        _clientB = await factory.ConnecterAsync(_b, Roles.Collaborateur, Roles.Manager);
        _clientC = await factory.ConnecterAsync(_c);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        _rh.Dispose();
        _clientA.Dispose();
        _clientB.Dispose();
        _clientC.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Un_manager_voit_son_equipe_directe_et_indirecte_et_personne_d_autre()
    {
        var visibles = await _clientA.IdentifiantsDeLaListeAsync();

        Assert.Equivalent(new[] { _a.Id, _b.Id, _c.Id }, visibles);
    }

    [Fact]
    public async Task Un_manager_intermediaire_ne_voit_que_son_sous_arbre()
    {
        var visibles = await _clientB.IdentifiantsDeLaListeAsync();

        Assert.Equivalent(new[] { _b.Id, _c.Id }, visibles);
    }

    [Fact]
    public async Task Un_collaborateur_ne_voit_que_sa_fiche()
    {
        var visibles = await _clientC.IdentifiantsDeLaListeAsync();

        Assert.Equal([_c.Id], visibles);
    }

    [Fact]
    public async Task Le_gestionnaire_RH_voit_tout_le_monde()
    {
        var visibles = await _rh.IdentifiantsDeLaListeAsync(_prefixe);

        Assert.Equivalent(new[] { _a.Id, _b.Id, _c.Id, _d.Id }, visibles);
    }

    [Fact]
    public async Task Hors_perimetre_la_lecture_d_une_fiche_repond_404()
    {
        var ct = TestContext.Current.CancellationToken;

        Assert.Equal(HttpStatusCode.NotFound, (await _clientA.GetAsync($"/api/collaborateurs/{_d.Id}", ct)).StatusCode);
        // Le manager de A n'existe pas pour C, ni A pour lui.
        Assert.Equal(HttpStatusCode.NotFound, (await _clientC.GetAsync($"/api/collaborateurs/{_a.Id}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientC.GetAsync($"/api/collaborateurs/{_b.Id}", ct)).StatusCode);
        // Un identifiant inconnu donne la même réponse : l'API ne révèle pas qu'une fiche existe.
        Assert.Equal(HttpStatusCode.NotFound, (await _clientA.GetAsync("/api/collaborateurs/987654", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _clientA.GetAsync($"/api/collaborateurs/{_c.Id}", ct)).StatusCode);
    }

    [Fact]
    public async Task Le_manager_lit_la_fiche_reduite_de_son_equipe_et_le_RH_la_fiche_complete()
    {
        var ct = TestContext.Current.CancellationToken;

        var vueDuManager = await _clientA.GetFromJsonAsync<JsonElement>($"/api/collaborateurs/{_c.Id}", ct);
        var vueDuRh = await _rh.GetFromJsonAsync<JsonElement>($"/api/collaborateurs/{_c.Id}", ct);

        Assert.Equal(_prefixe + "C", vueDuManager.GetProperty("matricule").GetString());
        Assert.True(vueDuManager.TryGetProperty("version", out _));
        Assert.False(vueDuManager.GetProperty("estSorti").GetBoolean());
        Assert.False(vueDuManager.TryGetProperty("dateEntree", out _));
        Assert.False(vueDuManager.TryGetProperty("dateSortie", out _));
        Assert.False(vueDuManager.TryGetProperty("quotite", out _));
        Assert.Equal("2024-09-02", vueDuRh.GetProperty("dateEntree").GetString());
        Assert.Equal(1m, vueDuRh.GetProperty("quotite").GetDecimal());
    }

    [Fact]
    public async Task L_interesse_lit_sa_propre_fiche_complete()
    {
        var ct = TestContext.Current.CancellationToken;

        var fiche = await _clientC.GetFromJsonAsync<JsonElement>($"/api/collaborateurs/{_c.Id}", ct);

        Assert.Equal("2024-09-02", fiche.GetProperty("dateEntree").GetString());
        Assert.True(fiche.TryGetProperty("quotite", out _));
    }

    [Fact]
    public async Task La_liste_ne_contient_jamais_les_champs_contractuels_meme_pour_le_RH()
    {
        var ct = TestContext.Current.CancellationToken;

        var page = await _rh.GetFromJsonAsync<JsonElement>("/api/collaborateurs?taille=100", ct);

        foreach (var element in page.GetProperty("elements").EnumerateArray())
        {
            Assert.False(element.TryGetProperty("dateEntree", out _));
            Assert.False(element.TryGetProperty("dateSortie", out _));
            Assert.False(element.TryGetProperty("quotite", out _));
        }
    }

    [Fact]
    public async Task L_administrateur_ne_voit_pas_les_fiches_des_collaborateurs()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.ClientAdministrateurAsync();

        Assert.Empty(await admin.IdentifiantsDeLaListeAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/collaborateurs/{_a.Id}", ct)).StatusCode);
        var plan = await admin.GetFromJsonAsync<JsonElement>("/api/plan-de-charge?du=2026-05-04&au=2026-05-08", ct);
        Assert.Equal(0, plan.GetProperty("lignes").GetArrayLength());
    }

    [Fact]
    public async Task Une_chaine_de_managers_circulaire_ne_fait_pas_boucler_le_calcul()
    {
        var ct = TestContext.Current.CancellationToken;
        var premier = await _rh.CreerCollaborateurAsync("CYCLE-1");
        var second = await _rh.CreerCollaborateurAsync("CYCLE-2", premier.Id);
        using var client = await factory.ConnecterAsync(premier, Roles.Collaborateur, Roles.Manager);

        // L'API refuse un cycle (RG-04) : on le fabrique en base, comme le ferait une donnée corrompue.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
            var premiereFiche = await db.Collaborateurs.SingleAsync(c => c.Id == premier.Id, ct);
            premiereFiche.RattacherA(second.Id);
            await db.SaveChangesAsync(ct);
        }

        var visibles = await client.IdentifiantsDeLaListeAsync();

        Assert.Equivalent(new[] { premier.Id, second.Id }, visibles);
    }

    [Fact]
    public async Task Le_plan_de_charge_se_limite_au_perimetre()
    {
        var ct = TestContext.Current.CancellationToken;

        var plan = await _clientA.GetFromJsonAsync<JsonElement>("/api/plan-de-charge?du=2026-05-04&au=2026-05-08", ct);

        var lignes = plan.GetProperty("lignes").EnumerateArray().Select(l => l.GetProperty("collaborateurId").GetInt32());
        Assert.Equivalent(new[] { _a.Id, _b.Id, _c.Id }, lignes);
    }

    [Fact]
    public async Task Un_manager_demande_le_plan_d_un_autre_manager_et_obtient_un_plan_vide()
    {
        var ct = TestContext.Current.CancellationToken;

        var plan = await _clientC.GetFromJsonAsync<JsonElement>(
            $"/api/plan-de-charge?du=2026-05-04&au=2026-05-08&managerId={_a.Id}", ct);

        Assert.Equal(0, plan.GetProperty("lignes").GetArrayLength());
    }

    [Fact]
    public async Task Les_affectations_se_limitent_au_perimetre()
    {
        var ct = TestContext.Current.CancellationToken;
        // Le projet 1 vient des données initiales.
        foreach (var (personne, taux) in new[] { (_c, 0.1), (_d, 0.1) })
        {
            var creation = await _rh.PostAsJsonAsync("/api/affectations",
                new { collaborateurId = personne.Id, projetId = 1, dateDebut = "2026-05-04", dateFin = "2026-05-08", tauxAllocation = taux }, ct);
            Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        }

        var liste = await _clientA.GetFromJsonAsync<JsonElement>("/api/affectations?projetId=1", ct);
        var concernes = liste.EnumerateArray().Select(a => a.GetProperty("collaborateurId").GetInt32()).ToList();

        Assert.Contains(_c.Id, concernes);
        Assert.DoesNotContain(_d.Id, concernes);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientA.GetAsync($"/api/affectations?collaborateurId={_d.Id}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _clientA.GetAsync($"/api/affectations?collaborateurId={_c.Id}", ct)).StatusCode);
    }

    [Fact]
    public async Task Un_manager_n_affecte_que_les_membres_de_son_equipe()
    {
        var ct = TestContext.Current.CancellationToken;
        object Affectation(int collaborateurId) => new
        {
            collaborateurId,
            projetId = 2,
            dateDebut = "2026-06-01",
            dateFin = "2026-06-05",
            tauxAllocation = 0.1,
        };

        var horsEquipe = await _clientA.PostAsJsonAsync("/api/affectations", Affectation(_d.Id), ct);
        var dansLEquipe = await _clientA.PostAsJsonAsync("/api/affectations", Affectation(_c.Id), ct);

        Assert.Equal(HttpStatusCode.BadRequest, horsEquipe.StatusCode);
        Assert.Equal(HttpStatusCode.Created, dansLEquipe.StatusCode);

        var creee = await dansLEquipe.LireAsync();
        var id = creee.GetProperty("id").GetInt32();
        using var clientD = await factory.ConnecterAsync(_d, Roles.Collaborateur, Roles.Manager);
        Assert.Equal(HttpStatusCode.NotFound, (await clientD.GetAsync($"/api/affectations/{id}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await clientD.DeleteAsync($"/api/affectations/{id}", ct)).StatusCode);
    }

    [Fact]
    public async Task La_feuille_d_un_collaborateur_hors_perimetre_n_existe_pas()
    {
        var ct = TestContext.Current.CancellationToken;

        var horsPerimetre = await _clientA.GetAsync($"/api/feuilles-de-temps?collaborateurId={_d.Id}&semaine=2026-05-04", ct);
        var equipe = await _clientA.GetAsync($"/api/feuilles-de-temps?collaborateurId={_c.Id}&semaine=2026-05-04", ct);
        var sansParametre = await _clientC.GetFromJsonAsync<JsonElement>("/api/feuilles-de-temps?semaine=2026-05-04", ct);

        Assert.Equal(HttpStatusCode.NotFound, horsPerimetre.StatusCode);
        Assert.Equal(HttpStatusCode.OK, equipe.StatusCode);
        Assert.Equal(_c.Id, sansParametre.GetProperty("collaborateurId").GetInt32());
    }

    [Fact]
    public async Task Le_manager_lit_la_feuille_de_son_equipe_sans_pouvoir_la_modifier()
    {
        var ct = TestContext.Current.CancellationToken;
        var feuille = await _clientA.GetFromJsonAsync<JsonElement>(
            $"/api/feuilles-de-temps?collaborateurId={_c.Id}&semaine=2026-05-04", ct);
        var id = feuille.GetProperty("id").GetInt32();

        var ecriture = await _clientA.PutAsJsonAsync($"/api/feuilles-de-temps/{id}/saisies", new { saisies = Array.Empty<object>() }, ct);
        var soumission = await _clientA.PostAsync($"/api/feuilles-de-temps/{id}/soumission", content: null, ct);
        var chezLeVoisin = await _clientB.PutAsJsonAsync($"/api/feuilles-de-temps/{id}/saisies", new { saisies = Array.Empty<object>() }, ct);
        using var clientD = await factory.ConnecterAsync(_d);
        var chezLIntrus = await clientD.PutAsJsonAsync(
            $"/api/feuilles-de-temps/{id}/saisies", new { saisies = Array.Empty<object>() }, ct);

        Assert.Equal(HttpStatusCode.Forbidden, ecriture.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, soumission.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, chezLeVoisin.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, chezLIntrus.StatusCode);
    }

    [Fact]
    public async Task Les_absences_se_limitent_au_perimetre()
    {
        var ct = TestContext.Current.CancellationToken;
        using var clientD = await factory.ConnecterAsync(_d);
        var absenceDeD = await clientD.PostAsJsonAsync("/api/absences",
            new { typeAbsenceId = 1, dateDebut = "2026-06-15", dateFin = "2026-06-16" }, ct);
        var absenceDeC = await _clientC.PostAsJsonAsync("/api/absences",
            new { typeAbsenceId = 1, dateDebut = "2026-06-15", dateFin = "2026-06-16" }, ct);
        Assert.Equal(HttpStatusCode.Created, absenceDeD.StatusCode);
        Assert.Equal(HttpStatusCode.Created, absenceDeC.StatusCode);
        var idDeD = (await absenceDeD.LireAsync()).GetProperty("id").GetInt32();
        var idDeC = (await absenceDeC.LireAsync()).GetProperty("id").GetInt32();

        var liste = await _clientA.GetFromJsonAsync<JsonElement>("/api/absences", ct);
        var concernes = liste.EnumerateArray().Select(a => a.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(idDeC, concernes);
        Assert.DoesNotContain(idDeD, concernes);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientA.GetAsync($"/api/absences?collaborateurId={_d.Id}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientA.GetAsync($"/api/absences/{idDeD}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _clientA.GetAsync($"/api/absences/{idDeC}", ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientC.GetAsync($"/api/absences/{idDeD}", ct)).StatusCode);
    }
}
