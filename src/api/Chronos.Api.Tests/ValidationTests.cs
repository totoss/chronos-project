using System.Net;
using System.Text.Json;
using Chronos.Api.Securite;
using Chronos.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Tests;

/// <summary>
/// RG-36 : seul le manager du collaborateur (ou un gestionnaire RH) valide ou rejette sa feuille ; nul ne valide la sienne.
/// Les absences suivent la même règle. Une hiérarchie A → B → C et une personne D sans lien.
/// </summary>
public class ValidationTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>, IAsyncLifetime
{
    private static int _sequence;

    private HttpClient _rh = null!;
    private (int Id, string Email) _a, _b, _c, _d;
    private HttpClient _clientA = null!, _clientB = null!, _clientC = null!, _clientD = null!;

    public async ValueTask InitializeAsync()
    {
        _rh = await factory.ClientRhAsync();
        var prefixe = $"V{Interlocked.Increment(ref _sequence)}-";
        _a = await _rh.CreerCollaborateurAsync(prefixe + "A");
        _b = await _rh.CreerCollaborateurAsync(prefixe + "B", _a.Id);
        _c = await _rh.CreerCollaborateurAsync(prefixe + "C", _b.Id);
        _d = await _rh.CreerCollaborateurAsync(prefixe + "D");
        _clientA = await factory.ConnecterAsync(_a, Roles.Collaborateur, Roles.Manager);
        _clientB = await factory.ConnecterAsync(_b, Roles.Collaborateur, Roles.Manager);
        _clientC = await factory.ConnecterAsync(_c);
        _clientD = await factory.ConnecterAsync(_d, Roles.Collaborateur, Roles.Manager);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        _rh.Dispose();
        _clientA.Dispose();
        _clientB.Dispose();
        _clientC.Dispose();
        _clientD.Dispose();
        return ValueTask.CompletedTask;
    }

    // Le collaborateur ouvre puis soumet sa feuille de la semaine du 4 mai 2026 (vide : seule l'autorisation compte).
    private static async Task<int> SoumettreSaFeuilleAsync(HttpClient client)
    {
        var ct = TestContext.Current.CancellationToken;
        var feuille = await client.GetFromJsonAsync<JsonElement>("/api/feuilles-de-temps?semaine=2026-05-04", ct);
        var id = feuille.GetProperty("id").GetInt32();
        var soumission = await client.PostAsync($"/api/feuilles-de-temps/{id}/soumission", content: null, ct);
        Assert.Equal(HttpStatusCode.OK, soumission.StatusCode);
        return id;
    }

    [Fact]
    public async Task Le_manager_direct_valide_la_feuille_de_son_collaborateur()
    {
        var ct = TestContext.Current.CancellationToken;
        var feuille = await SoumettreSaFeuilleAsync(_clientC);

        var validation = await _clientB.PostAsync($"/api/feuilles-de-temps/{feuille}/validation", content: null, ct);

        Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
        Assert.Equal("Validee", (await validation.LireAsync()).GetProperty("statut").GetString());
    }

    [Fact]
    public async Task Un_manager_indirect_voit_la_feuille_mais_ne_la_valide_pas()
    {
        var ct = TestContext.Current.CancellationToken;
        var feuille = await SoumettreSaFeuilleAsync(_clientC);

        var validation = await _clientA.PostAsync($"/api/feuilles-de-temps/{feuille}/validation", content: null, ct);
        var rejet = await _clientA.PostAsJsonAsync($"/api/feuilles-de-temps/{feuille}/rejet", new { motif = "Trop tard" }, ct);

        Assert.Equal(HttpStatusCode.Forbidden, validation.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, rejet.StatusCode);
    }

    [Fact]
    public async Task Un_manager_d_une_autre_equipe_ne_voit_pas_la_feuille()
    {
        var ct = TestContext.Current.CancellationToken;
        var feuille = await SoumettreSaFeuilleAsync(_clientC);

        var validation = await _clientD.PostAsync($"/api/feuilles-de-temps/{feuille}/validation", content: null, ct);
        var rejet = await _clientD.PostAsJsonAsync($"/api/feuilles-de-temps/{feuille}/rejet", new { motif = "Non" }, ct);

        Assert.Equal(HttpStatusCode.NotFound, validation.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rejet.StatusCode);
    }

    [Fact]
    public async Task Nul_ne_valide_ni_ne_rejette_sa_propre_feuille()
    {
        var ct = TestContext.Current.CancellationToken;
        // B est manager : il a la fonction, pas le droit sur sa propre feuille. Le RH non plus.
        var feuilleDeB = await SoumettreSaFeuilleAsync(_clientB);
        var feuilleDuRh = await SoumettreSaFeuilleAsync(_rh);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await _clientB.PostAsync($"/api/feuilles-de-temps/{feuilleDeB}/validation", content: null, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _clientB.PostAsJsonAsync($"/api/feuilles-de-temps/{feuilleDeB}/rejet", new { motif = "Moi" }, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await _rh.PostAsync($"/api/feuilles-de-temps/{feuilleDuRh}/validation", content: null, ct)).StatusCode);
    }

    [Fact]
    public async Task Le_gestionnaire_RH_valide_la_feuille_de_n_importe_qui_sauf_la_sienne()
    {
        var ct = TestContext.Current.CancellationToken;
        var feuilleDeD = await SoumettreSaFeuilleAsync(_clientD);
        var feuilleDeB = await SoumettreSaFeuilleAsync(_clientB);

        var validation = await _rh.PostAsync($"/api/feuilles-de-temps/{feuilleDeD}/validation", content: null, ct);
        var rejet = await _rh.PostAsJsonAsync($"/api/feuilles-de-temps/{feuilleDeB}/rejet", new { motif = "À revoir" }, ct);

        Assert.Equal(HttpStatusCode.OK, validation.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rejet.StatusCode);
        var rejetee = await rejet.LireAsync();
        Assert.Equal("Brouillon", rejetee.GetProperty("statut").GetString());
        Assert.Equal("À revoir", rejetee.GetProperty("motifRejet").GetString());
    }

    [Fact]
    public async Task La_validation_est_tracee_au_nom_du_valideur()
    {
        var ct = TestContext.Current.CancellationToken;
        var feuille = await SoumettreSaFeuilleAsync(_clientC);
        factory.Horloge.Advance(TimeSpan.FromMinutes(5));

        await _clientB.PostAsync($"/api/feuilles-de-temps/{feuille}/validation", content: null, ct);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var ligne = await db.FeuillesDeTemps.AsNoTracking().SingleAsync(f => f.Id == feuille, ct);
        Assert.Equal(factory.Horloge.GetUtcNow(), ligne.ModifieLe);
        Assert.Equal(await factory.IdentifiantDuCompteAsync(_b.Email), ligne.ModifiePar);
        Assert.Equal(await factory.IdentifiantDuCompteAsync(_c.Email), ligne.CreePar);
    }

    [Fact]
    public async Task Les_feuilles_a_valider_sont_celles_que_l_appelant_peut_valider()
    {
        var ct = TestContext.Current.CancellationToken;
        var feuilleDeC = await SoumettreSaFeuilleAsync(_clientC);
        var feuilleDeD = await SoumettreSaFeuilleAsync(_clientD);

        var vueDeB = await _clientB.GetFromJsonAsync<JsonElement>("/api/feuilles-de-temps/soumises", ct);
        var vueDeA = await _clientA.GetFromJsonAsync<JsonElement>("/api/feuilles-de-temps/soumises", ct);
        var vueDuRh = await _rh.GetFromJsonAsync<JsonElement>("/api/feuilles-de-temps/soumises", ct);

        var idsDeB = vueDeB.EnumerateArray().Select(f => f.GetProperty("id").GetInt32()).ToList();
        var idsDeA = vueDeA.EnumerateArray().Select(f => f.GetProperty("id").GetInt32()).ToList();
        var idsDuRh = vueDuRh.EnumerateArray().Select(f => f.GetProperty("id").GetInt32()).ToList();
        // Le manager direct la voit ; le manager indirect et celui d'une autre équipe non.
        Assert.Contains(feuilleDeC, idsDeB);
        Assert.DoesNotContain(feuilleDeD, idsDeB);
        Assert.DoesNotContain(feuilleDeC, idsDeA);
        Assert.DoesNotContain(feuilleDeD, idsDeA);
        Assert.Contains(feuilleDeC, idsDuRh);
        Assert.Contains(feuilleDeD, idsDuRh);
    }

    // Des absences, comme des feuilles : le manager direct approuve ou refuse, nul n'approuve la sienne.

    private static async Task<int> DemanderUneAbsenceAsync(HttpClient client, string debut, string fin)
    {
        var reponse = await client.PostAsJsonAsync("/api/absences",
            new { typeAbsenceId = 1, dateDebut = debut, dateFin = fin }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        return (await reponse.LireAsync()).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Le_manager_direct_approuve_et_refuse_les_absences_de_son_collaborateur()
    {
        var ct = TestContext.Current.CancellationToken;
        var aApprouver = await DemanderUneAbsenceAsync(_clientC, "2026-06-01", "2026-06-02");
        var aRefuser = await DemanderUneAbsenceAsync(_clientC, "2026-06-03", "2026-06-04");

        var approbation = await _clientB.PostAsync($"/api/absences/{aApprouver}/approbation", content: null, ct);
        var refus = await _clientB.PostAsync($"/api/absences/{aRefuser}/refus", content: null, ct);

        Assert.Equal("Approuvee", (await approbation.LireAsync()).GetProperty("statut").GetString());
        Assert.Equal("Refusee", (await refus.LireAsync()).GetProperty("statut").GetString());
    }

    [Fact]
    public async Task Les_absences_suivent_les_trois_niveaux_de_RG_36()
    {
        var ct = TestContext.Current.CancellationToken;
        var deC = await DemanderUneAbsenceAsync(_clientC, "2026-06-08", "2026-06-09");
        var deB = await DemanderUneAbsenceAsync(_clientB, "2026-06-08", "2026-06-09");

        // La politique : un simple collaborateur n'approuve rien.
        Assert.Equal(HttpStatusCode.Forbidden, (await _clientC.PostAsync($"/api/absences/{deC}/approbation", content: null, ct)).StatusCode);
        // Le périmètre : un manager d'une autre équipe ne voit pas l'absence.
        Assert.Equal(HttpStatusCode.NotFound, (await _clientD.PostAsync($"/api/absences/{deC}/approbation", content: null, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientD.PostAsync($"/api/absences/{deC}/refus", content: null, ct)).StatusCode);
        // La relation : un manager indirect la voit sans l'approuver, et nul n'approuve la sienne.
        Assert.Equal(HttpStatusCode.Forbidden, (await _clientA.PostAsync($"/api/absences/{deC}/approbation", content: null, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _clientB.PostAsync($"/api/absences/{deB}/approbation", content: null, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _clientB.PostAsync($"/api/absences/{deB}/refus", content: null, ct)).StatusCode);
        // Le gestionnaire RH approuve celle de n'importe qui d'autre.
        Assert.Equal(HttpStatusCode.OK, (await _rh.PostAsync($"/api/absences/{deB}/approbation", content: null, ct)).StatusCode);
    }

    [Fact]
    public async Task Seul_le_demandeur_annule_son_absence()
    {
        var ct = TestContext.Current.CancellationToken;
        var deC = await DemanderUneAbsenceAsync(_clientC, "2026-06-15", "2026-06-16");

        Assert.Equal(HttpStatusCode.Forbidden, (await _clientB.PostAsync($"/api/absences/{deC}/annulation", content: null, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _clientD.PostAsync($"/api/absences/{deC}/annulation", content: null, ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _clientC.PostAsync($"/api/absences/{deC}/annulation", content: null, ct)).StatusCode);
    }

    [Fact]
    public async Task Un_compte_sans_fiche_ne_pose_pas_d_absence()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.ClientAdministrateurAsync();

        var reponse = await admin.PostAsJsonAsync("/api/absences",
            new { typeAbsenceId = 1, dateDebut = "2026-06-22", dateFin = "2026-06-23" }, ct);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }
}
