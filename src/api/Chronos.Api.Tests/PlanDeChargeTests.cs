using System.Net;
using System.Text.Json;

namespace Chronos.Api.Tests;

public class PlanDeChargeTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync() => _client = await factory.ClientRhAsync();

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    // L'exemple chiffré du cahier des charges (§ 7, précisé au § 8) : Camille (identifiant 2, 80 %) et les projets
    // 1 et 2 viennent des données initiales, comme le jour férié du vendredi 8 mai.
    [Fact]
    public async Task L_exemple_de_Camille_est_en_surcharge_a_cause_de_son_conge()
    {
        var ct = TestContext.Current.CancellationToken;
        foreach (var projetId in new[] { 1, 2 })
        {
            var affectation = await _client.PostAsJsonAsync("/api/affectations",
                new { collaborateurId = 2, projetId, dateDebut = "2026-05-04", dateFin = "2026-05-08", tauxAllocation = 0.5 }, ct);
            Assert.Equal(HttpStatusCode.Created, affectation.StatusCode);
        }

        // Camille pose elle-même son congé ; le gestionnaire RH l'approuve.
        using var camille = await factory.ClientCamilleAsync();
        var demande = await camille.PostAsJsonAsync("/api/absences",
            new { typeAbsenceId = 1, dateDebut = "2026-05-04", dateFin = "2026-05-04" }, ct);
        Assert.Equal(HttpStatusCode.Created, demande.StatusCode);
        var absence = await demande.Content.ReadFromJsonAsync<JsonElement>(ct);
        var approbation = await _client.PostAsync($"/api/absences/{absence.GetProperty("id").GetInt32()}/approbation", null, ct);
        Assert.Equal(HttpStatusCode.OK, approbation.StatusCode);

        var plan = await _client.GetFromJsonAsync<JsonElement>(
            "/api/plan-de-charge?du=2026-05-04&au=2026-05-08&pas=semaine", ct);

        var ligneDeCamille = plan.GetProperty("lignes").EnumerateArray()
            .Single(l => l.GetProperty("collaborateurId").GetInt32() == 2);
        var semaine = Assert.Single(ligneDeCamille.GetProperty("periodes").EnumerateArray());
        Assert.Equal(2.4m, semaine.GetProperty("capacite").GetDecimal());
        Assert.Equal(3.2m, semaine.GetProperty("chargePlanifiee").GetDecimal());
        Assert.Equal(1.3333m, semaine.GetProperty("tauxDeCharge").GetDecimal());
        Assert.True(semaine.GetProperty("surcharge").GetBoolean());
    }

    [Fact]
    public async Task Sans_parametre_le_plan_porte_sur_la_semaine_de_l_horloge()
    {
        var ct = TestContext.Current.CancellationToken;

        // L'horloge de la fabrique est réglée sur le lundi 4 mai 2026.
        var plan = await _client.GetFromJsonAsync<JsonElement>("/api/plan-de-charge", ct);

        Assert.Equal("2026-05-04", plan.GetProperty("du").GetString());
        Assert.Equal("2026-05-10", plan.GetProperty("au").GetString());
    }
}
