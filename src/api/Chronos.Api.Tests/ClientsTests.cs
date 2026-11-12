using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Chronos.Api.Tests;

public class ClientsTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Le_cycle_complet_d_un_client()
    {
        var ct = TestContext.Current.CancellationToken;

        var creation = await _client.PostAsJsonAsync("/api/clients", new { nom = "Cycle SA" }, ct);
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        var adresse = creation.Headers.Location;
        Assert.NotNull(adresse);

        var lu = await _client.GetFromJsonAsync<JsonElement>(adresse, ct);
        Assert.Equal("Cycle SA", lu.GetProperty("nom").GetString());
        Assert.Equal(0, lu.GetProperty("projetsEnCours").GetInt32());

        var modification = await _client.PutAsJsonAsync(adresse, new { nom = "Cycle SAS" }, ct);
        Assert.Equal(HttpStatusCode.OK, modification.StatusCode);

        var liste = await _client.GetFromJsonAsync<JsonElement>("/api/clients", ct);
        Assert.Contains(liste.EnumerateArray(), c => c.GetProperty("nom").GetString() == "Cycle SAS");
        Assert.DoesNotContain(liste.EnumerateArray(), c => c.GetProperty("nom").GetString() == "Cycle SA");

        var suppression = await _client.DeleteAsync(adresse, ct);
        Assert.Equal(HttpStatusCode.NoContent, suppression.StatusCode);

        var relecture = await _client.GetAsync(adresse, ct);
        Assert.Equal(HttpStatusCode.NotFound, relecture.StatusCode);
    }

    [Fact]
    public async Task Un_client_qui_a_des_projets_ne_se_supprime_pas()
    {
        var ct = TestContext.Current.CancellationToken;
        var creation = await _client.PostAsJsonAsync("/api/clients", new { nom = "Avec projet SA" }, ct);
        var client = await creation.Content.ReadFromJsonAsync<JsonElement>(ct);
        var clientId = client.GetProperty("id").GetInt32();
        var projet = await _client.PostAsJsonAsync("/api/projets",
            new { code = "CLI-PROJET", libelle = "Projet du client", nature = "Facturable", clientId,
                dateDebut = "2026-05-04", budgetJours = 10, responsableId = 1 }, ct);
        Assert.Equal(HttpStatusCode.Created, projet.StatusCode);

        var suppression = await _client.DeleteAsync($"/api/clients/{clientId}", ct);

        Assert.Equal(HttpStatusCode.Conflict, suppression.StatusCode);
    }
}
