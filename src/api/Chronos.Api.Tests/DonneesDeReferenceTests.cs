using Chronos.Domain;
using Chronos.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Api.Tests;

/// <summary>
/// Les types d'absence sont des données de référence : ils viennent des migrations, donc une base neuve les
/// contient sans qu'aucune donnée de démonstration soit semée (ni en production, ni par un script SQL).
/// </summary>
public class DonneesDeReferenceTests(ServeurDeBase serveur)
{
    private ChronosDbContext NouvelleBase()
    {
        var chaine = serveur.ChaineDeConnexion($"chronos_ref_{Guid.NewGuid():N}");
        var options = new DbContextOptionsBuilder<ChronosDbContext>();
        _ = serveur.Fournisseur switch
        {
            "SqlServer" => options.UseSqlServer(chaine, o => o.MigrationsAssembly("Chronos.Migrations.SqlServer")),
            _ => options.UseNpgsql(chaine, o => o.MigrationsAssembly("Chronos.Migrations.PostgreSql")),
        };

        return new ChronosDbContext(options.Options);
    }

    [Fact]
    public async Task Une_base_neuve_contient_les_types_d_absence_et_aucune_donnee_de_demonstration()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = NouvelleBase();
        await db.Database.MigrateAsync(ct);

        var types = await db.TypesAbsence.OrderBy(t => t.Id).Select(t => new { t.Id, t.Code }).ToListAsync(ct);

        Assert.Equal([(1, "CP"), (2, "RTT"), (3, "MAL")], types.Select(t => (t.Id, t.Code)));
        Assert.Empty(await db.Collaborateurs.ToListAsync(ct));
        Assert.Empty(await db.Projets.ToListAsync(ct));
        Assert.Empty(await db.JoursFeries.ToListAsync(ct));
    }

    [Fact]
    public async Task Un_type_ajoute_apres_les_donnees_de_reference_recoit_l_identifiant_suivant()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = NouvelleBase();
        await db.Database.MigrateAsync(ct);

        var teletravail = new TypeAbsence("TEL", "Télétravail");
        db.TypesAbsence.Add(teletravail);
        await db.SaveChangesAsync(ct);

        Assert.Equal(4, teletravail.Id);
    }
}
