using System.Net;
using System.Text.Json;
using Chronos.Infrastructure.Identite;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Tests;

public class SecuriteTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    // Un client qui ne suit pas les redirections : une redirection vers une page de connexion se verrait.
    private HttpClient ClientAnonyme() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/api/collaborateurs")]
    [InlineData("/api/collaborateurs/1")]
    [InlineData("/api/clients")]
    [InlineData("/api/projets")]
    [InlineData("/api/affectations")]
    [InlineData("/api/jours-feries")]
    [InlineData("/api/plan-de-charge")]
    [InlineData("/api/feuilles-de-temps/soumises")]
    [InlineData("/api/absences")]
    [InlineData("/api/types-absence")]
    [InlineData("/api/compte/moi")]
    public async Task Sans_connexion_les_routes_repondent_401_sans_redirection(string route)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientAnonyme();

        var reponse = await client.GetAsync(route, ct);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
        Assert.Null(reponse.Headers.Location);
    }

    [Fact]
    public async Task Une_ecriture_sans_connexion_repond_401_et_non_400()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientAnonyme();

        var reponse = await client.PostAsJsonAsync("/api/clients", new { nom = "Anonyme SA" }, ct);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Le_401_est_un_ProblemDetails()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientAnonyme();

        var reponse = await client.GetAsync("/api/collaborateurs", ct);

        Assert.Equal("application/problem+json", reponse.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("POST", "/api/compte/register")]
    [InlineData("POST", "/api/compte/refresh")]
    [InlineData("POST", "/api/compte/forgotPassword")]
    [InlineData("POST", "/api/compte/resetPassword")]
    [InlineData("POST", "/api/compte/resendConfirmationEmail")]
    [InlineData("POST", "/api/compte/manage/2fa")]
    [InlineData("POST", "/api/compte/manage/info")]
    [InlineData("GET", "/api/compte/manage/info")]
    [InlineData("GET", "/api/compte/confirmEmail?userId=1&code=x")]
    public async Task Les_autres_routes_d_Identity_repondent_404_connecte_ou_non(string methode, string route)
    {
        var ct = TestContext.Current.CancellationToken;
        using var anonyme = ClientAnonyme();
        using var rh = await factory.ClientRhAsync();

        foreach (var client in new[] { anonyme, rh })
        {
            var requete = new HttpRequestMessage(new HttpMethod(methode), route);
            if (methode == "POST")
            {
                // Un corps qui satisfait toutes les formes de requête d'Identity : sinon la lecture du corps échoue (400) avant le filtre.
                requete.Content = JsonContent.Create(new
                {
                    email = "intrus@chronos.example",
                    password = ChronosApiFactory.MotDePasse,
                    refreshToken = "x",
                    resetCode = "x",
                    newPassword = ChronosApiFactory.MotDePasse,
                });
            }

            var reponse = await client.SendAsync(requete, ct);

            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        }
    }

    [Fact]
    public async Task L_inscription_ne_cree_aucun_compte()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientAnonyme();
        var avant = await CompterLesComptesAsync(ct);

        var reponse = await client.PostAsJsonAsync("/api/compte/register",
            new { email = "intrus@chronos.example", password = ChronosApiFactory.MotDePasse }, ct);

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        Assert.Equal(avant, await CompterLesComptesAsync(ct));
    }

    private async Task<int> CompterLesComptesAsync(CancellationToken ct)
    {
        using var scope = factory.Services.CreateScope();
        var comptes = scope.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();
        return await comptes.Users.CountAsync(ct);
    }

    [Fact]
    public async Task La_documentation_OpenAPI_reste_ouverte_en_developpement()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = ClientAnonyme();

        var reponse = await client.GetAsync("/openapi/v1.json", ct);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Theory]
    [InlineData("inconnu@chronos.example", "Chronos-Test-2026!")]
    [InlineData("hugo.bernard@chronos.example", "mauvais-mot-de-passe")]
    public async Task Une_connexion_refusee_repond_401_en_ProblemDetails(string email, string motDePasse)
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        using var client = ClientAnonyme();

        var reponse = await client.PostAsJsonAsync("/api/compte/login?useCookies=true",
            new { email, password = motDePasse }, ct);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
        Assert.Equal("application/problem+json", reponse.Content.Headers.ContentType?.MediaType);
        Assert.False(reponse.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task La_connexion_pose_un_cookie_de_session_HttpOnly_SameSite_Strict()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        using var client = ClientAnonyme();

        var reponse = await client.PostAsJsonAsync("/api/compte/login?useCookies=true",
            new { email = "hugo.bernard@chronos.example", password = ChronosApiFactory.MotDePasse }, ct);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(0, reponse.Content.Headers.ContentLength);
        var cookie = Assert.Single(reponse.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("chronos.session=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        // Les tests tournent en développement, en HTTP : sans « secure » (voir le réglage dans Program.cs).
        Assert.DoesNotContain("secure", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Moi_decrit_l_utilisateur_et_pose_le_cookie_anti_CSRF()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await factory.ClientSophieAsync();

        var reponse = await client.GetAsync("/api/compte/moi", ct);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var moi = await reponse.LireAsync();
        Assert.Equal(ChronosApiFactory.SophieId, moi.GetProperty("collaborateurId").GetInt32());
        Assert.Equal("Martin", moi.GetProperty("nom").GetString());
        Assert.Equal("Sophie", moi.GetProperty("prenom").GetString());
        Assert.Equal("sophie.martin@chronos.example", moi.GetProperty("email").GetString());
        Assert.Equal(["Collaborateur", "Manager"],
            moi.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray());

        var cookie = Assert.Single(reponse.Headers.GetValues("Set-Cookie"),
            c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        Assert.DoesNotContain("httponly", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Moi_d_un_compte_sans_fiche_n_a_ni_nom_ni_identifiant()
    {
        var ct = TestContext.Current.CancellationToken;
        using var admin = await factory.ClientAdministrateurAsync();

        var moi = await admin.GetFromJsonAsync<JsonElement>("/api/compte/moi", ct);

        Assert.Equal(JsonValueKind.Null, moi.GetProperty("collaborateurId").ValueKind);
        Assert.Equal(JsonValueKind.Null, moi.GetProperty("nom").ValueKind);
        Assert.Equal(["Administrateur"], moi.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToArray());
    }

    [Theory]
    [InlineData("POST", "/api/clients")]
    [InlineData("PUT", "/api/clients/999")]
    [InlineData("DELETE", "/api/clients/999")]
    public async Task Une_ecriture_sans_jeton_anti_CSRF_repond_400(string methode, string route)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await factory.ClientRhAsync();
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var reponse = await client.SendAsync(new HttpRequestMessage(new HttpMethod(methode), route)
        {
            Content = JsonContent.Create(new { nom = "Sans jeton SA" }),
        }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal("application/problem+json", reponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Un_jeton_anti_CSRF_invalide_repond_400_et_le_jeton_valide_laisse_passer()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await factory.ClientRhAsync();
        var jeton = client.DefaultRequestHeaders.GetValues("X-XSRF-TOKEN").Single();
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "n-importe-quoi");

        var refuse = await client.PostAsJsonAsync("/api/clients", new { nom = "Jeton faux SA" }, ct);

        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", jeton);
        var accepte = await client.PostAsJsonAsync("/api/clients", new { nom = "Jeton bon SA" }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, refuse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, accepte.StatusCode);
    }

    [Fact]
    public async Task Le_jeton_anti_CSRF_est_lie_a_l_utilisateur()
    {
        var ct = TestContext.Current.CancellationToken;
        using var camille = await factory.ClientCamilleAsync();
        using var rh = await factory.ClientRhAsync();
        var jetonDeCamille = camille.DefaultRequestHeaders.GetValues("X-XSRF-TOKEN").Single();
        rh.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        rh.DefaultRequestHeaders.Add("X-XSRF-TOKEN", jetonDeCamille);

        var reponse = await rh.PostAsJsonAsync("/api/clients", new { nom = "Jeton emprunté SA" }, ct);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task La_connexion_elle_meme_n_exige_pas_de_jeton()
    {
        var ct = TestContext.Current.CancellationToken;
        using var rh = await factory.ClientRhAsync();
        using var client = ClientAnonyme();

        var reponse = await client.PostAsJsonAsync("/api/compte/login?useSessionCookies=true",
            new { email = "hugo.bernard@chronos.example", password = ChronosApiFactory.MotDePasse }, ct);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task La_deconnexion_repond_204_supprime_la_session_et_les_deux_cookies()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await factory.ClientCamilleAsync();

        var reponse = await client.PostAsJsonAsync("/api/compte/logout", new { }, ct);

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        var cookies = reponse.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.StartsWith("chronos.session=;", StringComparison.Ordinal));
        Assert.Contains(cookies, c => c.StartsWith("XSRF-TOKEN=;", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/compte/moi", ct)).StatusCode);
    }

    [Fact]
    public async Task La_deconnexion_sans_corps_repond_400()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await factory.ClientCamilleAsync();

        var reponse = await client.PostAsync("/api/compte/logout", content: null, ct);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/compte/moi", ct)).StatusCode);
    }
}
