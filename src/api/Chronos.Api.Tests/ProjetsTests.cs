using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Chronos.Api.Tests;

public class ProjetsTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // Sophie, la responsable des données initiales, porte l'identifiant 1.
    private static object NouveauProjet(string code, int clientId) => new
    {
        code,
        libelle = "Refonte",
        nature = "Facturable",
        clientId,
        dateDebut = "2026-05-04",
        budgetJours = 40,
        responsableId = 1,
    };

    private async Task<int> CreerClientAsync(string nom)
    {
        var reponse = await _client.PostAsJsonAsync("/api/clients", new { nom }, TestContext.Current.CancellationToken);
        var client = await reponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return client.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Un_projet_facturable_sans_client_renvoie_422_et_cite_la_regle()
    {
        var ct = TestContext.Current.CancellationToken;
        var requete = new { code = "P-001", libelle = "Refonte", nature = "Facturable", clientId = (int?)null,
            dateDebut = "2026-05-04", budgetJours = 40, responsableId = 1 };

        var reponse = await _client.PostAsJsonAsync("/api/projets", requete, ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
        using var json = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync(ct));
        Assert.Equal("RG-11", json.RootElement.GetProperty("regle").GetString());
    }

    [Fact]
    public async Task Le_cycle_complet_d_un_projet()
    {
        var ct = TestContext.Current.CancellationToken;
        var clientId = await CreerClientAsync("Cycle projet SA");

        var creation = await _client.PostAsJsonAsync("/api/projets", NouveauProjet("p-cycle", clientId), ct);
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var cree = await creation.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal("P-CYCLE", cree.GetProperty("code").GetString());
        Assert.Equal("EnPreparation", cree.GetProperty("statut").GetString());

        var modification = await _client.PutAsJsonAsync(creation.Headers.Location,
            new { libelle = "Refonte du portail", nature = "Facturable", clientId, dateDebut = "2026-05-04",
                budgetJours = 55, responsableId = 1, version = cree.GetProperty("version").GetGuid() }, ct);
        Assert.Equal(HttpStatusCode.OK, modification.StatusCode);

        var lu = await _client.GetFromJsonAsync<JsonElement>(creation.Headers.Location, ct);
        Assert.Equal("Refonte du portail", lu.GetProperty("libelle").GetString());
        Assert.Equal(55, lu.GetProperty("budgetJours").GetInt32());
        Assert.Equal("Cycle projet SA", lu.GetProperty("clientNom").GetString());

        var page = await _client.GetFromJsonAsync<JsonElement>("/api/projets?recherche=p-cycle&taille=5", ct);
        Assert.Equal(1, page.GetProperty("total").GetInt32());
        Assert.Equal(5, page.GetProperty("taille").GetInt32());
        Assert.Equal("P-CYCLE", page.GetProperty("elements")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Un_code_de_projet_deja_utilise_renvoie_409()
    {
        var ct = TestContext.Current.CancellationToken;
        var clientId = await CreerClientAsync("Doublon SA");
        var premier = await _client.PostAsJsonAsync("/api/projets", NouveauProjet("P-DOUBLON", clientId), ct);
        Assert.Equal(HttpStatusCode.Created, premier.StatusCode);

        var doublon = await _client.PostAsJsonAsync("/api/projets", NouveauProjet("p-doublon", clientId), ct);

        Assert.Equal(HttpStatusCode.Conflict, doublon.StatusCode);
    }

    [Fact]
    public async Task Une_modification_faite_avec_une_version_perimee_renvoie_409()
    {
        var ct = TestContext.Current.CancellationToken;
        var clientId = await CreerClientAsync("Concurrence SA");
        var creation = await _client.PostAsJsonAsync("/api/projets", NouveauProjet("P-CONCURRENCE", clientId), ct);
        var cree = await creation.Content.ReadFromJsonAsync<JsonElement>(ct);
        var modification = new { libelle = "Modifié", nature = "Facturable", clientId, dateDebut = "2026-05-04",
            budgetJours = 41, responsableId = 1, version = cree.GetProperty("version").GetGuid() };

        var premiere = await _client.PutAsJsonAsync(creation.Headers.Location, modification, ct);
        var seconde = await _client.PutAsJsonAsync(creation.Headers.Location, modification, ct);

        Assert.Equal(HttpStatusCode.OK, premiere.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, seconde.StatusCode);
    }
}
