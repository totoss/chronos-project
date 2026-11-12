using System.Net;
using System.Text.Json;
using Chronos.Api.Securite;
using Chronos.Infrastructure.Identite;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Tests;

public class ComptesDeDemonstrationTests(ChronosApiFactory factory) : IClassFixture<ChronosApiFactory>
{
    private const string MotDePasseDeDemo = "Demo-Tests-2026!";

    [Fact]
    public async Task Un_compte_par_role_est_relie_a_sa_fiche_et_la_creation_est_idempotente()
    {
        var ct = TestContext.Current.CancellationToken;

        await ComptesDeDemonstration.CreerAsync(factory.Services, MotDePasseDeDemo);
        await ComptesDeDemonstration.CreerAsync(factory.Services, MotDePasseDeDemo);

        using (var scope = factory.Services.CreateScope())
        {
            var comptes = scope.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();
            Assert.Equal(4, await comptes.Users.CountAsync(ct));
        }

        (string Email, int? Fiche, string[] Roles)[] attendus =
        [
            ("camille.durand@chronos.example", ChronosApiFactory.CamilleId, [Roles.Collaborateur]),
            ("sophie.martin@chronos.example", ChronosApiFactory.SophieId, [Roles.Collaborateur, Roles.Manager]),
            ("hugo.bernard@chronos.example", ChronosApiFactory.HugoId, [Roles.Collaborateur, Roles.GestionnaireRh]),
            ("admin@chronos.example", null, [Roles.Administrateur]),
        ];
        foreach (var (email, fiche, roles) in attendus)
        {
            using var client = factory.CreateClient();
            var connexion = await client.PostAsJsonAsync("/api/compte/login?useSessionCookies=true",
                new { email, password = MotDePasseDeDemo }, ct);
            Assert.Equal(HttpStatusCode.OK, connexion.StatusCode);

            var moi = await client.GetFromJsonAsync<JsonElement>("/api/compte/moi", ct);
            Assert.Equal(fiche, moi.GetProperty("collaborateurId").ValueKind == JsonValueKind.Null
                ? null
                : moi.GetProperty("collaborateurId").GetInt32());
            Assert.Equal(roles.Order(), moi.GetProperty("roles").EnumerateArray().Select(r => r.GetString()!));
        }
    }
}
