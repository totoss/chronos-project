using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Chronos.Domain;
using Chronos.Infrastructure;
using Chronos.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chronos.Api.Tests;

public class CollaborateursTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

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

        var apres = await _client.GetFromJsonAsync<JsonElement>("/api/collaborateurs?statut=sorti", ct);
        Assert.Contains(apres.GetProperty("elements").EnumerateArray(),
            c => c.GetProperty("matricule").GetString() == "C-0004");
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
}
