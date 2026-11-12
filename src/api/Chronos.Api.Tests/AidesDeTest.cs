using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chronos.Api.Securite;

namespace Chronos.Api.Tests;

/// <summary>Ce que plusieurs classes de tests refont : créer une personne, lui ouvrir un compte, lire une réponse.</summary>
internal static class AidesDeTest
{
    /// <summary>Crée une fiche par l'API (le client doit être gestionnaire RH) et renvoie son identifiant et son adresse.</summary>
    public static async Task<(int Id, string Email)> CreerCollaborateurAsync(
        this HttpClient rh, string matricule, int? managerId = null)
    {
        var email = $"{matricule.ToLowerInvariant()}@chronos.example";
        var reponse = await rh.PostAsJsonAsync("/api/collaborateurs", new
        {
            matricule,
            nom = $"Nom {matricule}",
            prenom = $"Prenom {matricule}",
            email,
            categorie = "Consultant",
            poste = "Développeur",
            dateEntree = "2024-09-02",
            typeContrat = "Cdi",
            quotite = 1,
            managerId,
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);

        var cree = await reponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return (cree.GetProperty("id").GetInt32(), email);
    }

    /// <summary>Un client connecté avec le compte d'une fiche fraîchement créée.</summary>
    public static Task<HttpClient> ConnecterAsync(
        this ChronosApiFactory factory, (int Id, string Email) personne, params string[] roles) =>
        factory.ClientConnecteAsync(personne.Email, personne.Id, roles.Length == 0 ? [Roles.Collaborateur] : roles);

    public static async Task<JsonElement> LireAsync(this HttpResponseMessage reponse) =>
        await reponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    /// <summary>Les identifiants des éléments d'une page de la liste des collaborateurs.</summary>
    public static async Task<int[]> IdentifiantsDeLaListeAsync(this HttpClient client, string recherche = "")
    {
        var page = await client.GetFromJsonAsync<JsonElement>($"/api/collaborateurs?taille=100&recherche={recherche}",
            TestContext.Current.CancellationToken);
        return [.. page.GetProperty("elements").EnumerateArray().Select(e => e.GetProperty("id").GetInt32())];
    }
}
