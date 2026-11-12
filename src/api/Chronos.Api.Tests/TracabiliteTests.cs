using System.Net.Http.Json;
using System.Text.Json;
using Chronos.Domain;
using Chronos.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chronos.Api.Tests;

/// <summary>RG-53 : qui et quand, renseignés par l'intercepteur, avec l'horloge de l'application et l'identifiant du compte.</summary>
public class TracabiliteTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    private async Task<Collaborateur> LireAsync(int id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        return await db.Collaborateurs.AsNoTracking().SingleAsync(c => c.Id == id, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Une_creation_porte_l_heure_de_l_horloge_et_l_identifiant_du_compte()
    {
        using var rh = await factory.ClientRhAsync();

        var personne = await rh.CreerCollaborateurAsync("TRA-1");

        var ligne = await LireAsync(personne.Id);
        Assert.Equal(factory.Horloge.GetUtcNow(), ligne.CreeLe);
        Assert.Equal(await factory.IdentifiantDuCompteAsync("hugo.bernard@chronos.example"), ligne.CreePar);
        Assert.Null(ligne.ModifieLe);
        Assert.Null(ligne.ModifiePar);
    }

    [Fact]
    public async Task La_trace_ne_recopie_ni_nom_ni_adresse()
    {
        using var rh = await factory.ClientRhAsync();

        var personne = await rh.CreerCollaborateurAsync("TRA-2");

        var ligne = await LireAsync(personne.Id);
        Assert.True(int.TryParse(ligne.CreePar, out _), "CreePar est l'identifiant numérique du compte.");
    }

    [Fact]
    public async Task Une_modification_ajoute_l_heure_et_l_auteur_sans_toucher_a_la_creation()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        var personne = await rh.CreerCollaborateurAsync("TRA-3");
        var creeLe = factory.Horloge.GetUtcNow();
        factory.Horloge.Advance(TimeSpan.FromHours(2));
        var fiche = await rh.GetFromJsonAsync<JsonElement>($"/api/collaborateurs/{personne.Id}", ct);

        var modification = await rh.PutAsJsonAsync($"/api/collaborateurs/{personne.Id}", new
        {
            nom = "Nouveau nom", prenom = "Prenom", email = personne.Email, poste = "Architecte",
            version = fiche.GetProperty("version").GetGuid(),
        }, ct);

        modification.EnsureSuccessStatusCode();
        var ligne = await LireAsync(personne.Id);
        Assert.Equal(creeLe, ligne.CreeLe);
        Assert.Equal(factory.Horloge.GetUtcNow(), ligne.ModifieLe);
        Assert.Equal(await factory.IdentifiantDuCompteAsync("hugo.bernard@chronos.example"), ligne.ModifiePar);
    }

    [Fact]
    public async Task La_trace_distingue_les_auteurs()
    {
        var ct = TestContext.Current.CancellationToken;
        using var sophie = await factory.ClientSophieAsync();
        var feuille = await sophie.GetFromJsonAsync<JsonElement>("/api/feuilles-de-temps?semaine=2026-05-04", ct);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var ligne = await db.FeuillesDeTemps.AsNoTracking()
            .SingleAsync(f => f.Id == feuille.GetProperty("id").GetInt32(), ct);
        Assert.Equal(await factory.IdentifiantDuCompteAsync("sophie.martin@chronos.example"), ligne.CreePar);
    }

    [Fact]
    public async Task L_approbation_d_une_absence_est_tracee_au_nom_du_valideur()
    {
        var ct = TestContext.Current.CancellationToken;
        using var camille = await factory.ClientCamilleAsync();
        using var sophie = await factory.ClientSophieAsync();
        var demande = await camille.PostAsJsonAsync("/api/absences",
            new { typeAbsenceId = 1, dateDebut = "2026-09-07", dateFin = "2026-09-08" }, ct);
        var id = (await demande.LireAsync()).GetProperty("id").GetInt32();
        factory.Horloge.Advance(TimeSpan.FromMinutes(10));

        var approbation = await sophie.PostAsync($"/api/absences/{id}/approbation", content: null, ct);

        approbation.EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var absence = await db.Absences.AsNoTracking().SingleAsync(a => a.Id == id, ct);
        Assert.Equal(await factory.IdentifiantDuCompteAsync("camille.durand@chronos.example"), absence.CreePar);
        Assert.Equal(await factory.IdentifiantDuCompteAsync("sophie.martin@chronos.example"), absence.ModifiePar);
        Assert.Equal(factory.Horloge.GetUtcNow(), absence.ModifieLe);
    }

    [Fact]
    public async Task Chaque_ressource_creee_par_l_API_est_tracee()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        var compte = await factory.IdentifiantDuCompteAsync("hugo.bernard@chronos.example");
        var client = await (await rh.PostAsJsonAsync("/api/clients", new { nom = "Trace SA" }, ct)).LireAsync();
        var projet = await (await rh.PostAsJsonAsync("/api/projets", new
        {
            code = "TRA-P", libelle = "Projet tracé", nature = "Interne", dateDebut = "2026-05-04",
            budgetJours = 5, responsableId = ChronosApiFactory.SophieId,
        }, ct)).LireAsync();
        var affectation = await (await rh.PostAsJsonAsync("/api/affectations", new
        {
            collaborateurId = ChronosApiFactory.CamilleId, projetId = projet.GetProperty("id").GetInt32(),
            dateDebut = "2026-05-04", dateFin = "2026-05-08", tauxAllocation = 0.1,
        }, ct)).LireAsync();
        (await rh.PostAsJsonAsync("/api/jours-feries", new { date = "2026-12-25", libelle = "Noël" }, ct)).EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        var idClient = client.GetProperty("id").GetInt32();
        var idProjet = projet.GetProperty("id").GetInt32();
        var idAffectation = affectation.GetProperty("id").GetInt32();
        var tracees = new ITracable[]
        {
            await db.Clients.AsNoTracking().SingleAsync(c => c.Id == idClient, ct),
            await db.Projets.AsNoTracking().SingleAsync(p => p.Id == idProjet, ct),
            await db.Affectations.AsNoTracking().SingleAsync(a => a.Id == idAffectation, ct),
            await db.JoursFeries.AsNoTracking().SingleAsync(j => j.Date == new DateOnly(2026, 12, 25), ct),
        };
        foreach (var tracee in tracees)
        {
            Assert.Equal(compte, tracee.CreePar);
            Assert.Equal(factory.Horloge.GetUtcNow(), tracee.CreeLe);
        }
    }

    [Fact]
    public async Task Hors_requete_l_auteur_est_le_systeme()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();

        // Les données initiales sont écrites hors requête, comme cette fiche.
        var sophie = await db.Collaborateurs.AsNoTracking().SingleAsync(c => c.Id == ChronosApiFactory.SophieId, ct);
        db.Collaborateurs.Add(new Collaborateur("TRA-SYS", "Systeme", "Test", "tra-sys@chronos.example",
            CategorieCollaborateur.Interne, "Test", new DateOnly(2024, 9, 2)));
        await db.SaveChangesAsync(ct);
        var ajoutee = await db.Collaborateurs.AsNoTracking().SingleAsync(c => c.Matricule == "TRA-SYS", ct);

        Assert.Equal("systeme", sophie.CreePar);
        Assert.Equal("systeme", ajoutee.CreePar);
    }
}
