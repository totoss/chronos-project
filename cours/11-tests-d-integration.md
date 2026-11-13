# Module 11 — Tests d'intégration

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 11](../docs/references/dotnet-11.md) et le [référentiel GitHub Actions](../docs/references/github-actions.md).
> Le code de ce module a été **rejoué le 12 novembre 2026 sur la solution de référence** (état du module 10, SDK 10.0.404, `xunit.v3.mtp-v2` 4.0.3, `Microsoft.AspNetCore.Mvc.Testing` 10.0.15, Testcontainers 4.16.0, EF Core 10.0.15, Npgsql 10.0.3, PostgreSQL 18, SQL Server 2025) : la fabrique applique les migrations de `Chronos.Migrations.PostgreSql` ou de `Chronos.Migrations.SqlServer` (module 07) et les données initiales du module 06 ; les tests du pas à pas et des exercices 1 à 5 passent (15 tests, environ 6 s sur PostgreSQL et 9 s sur SQL Server), deux fois de suite et classe par classe ; les deux commandes de l'étape 9 aussi, et `actionlint` accepte le workflow. Les essais ont été lancés depuis un conteneur du SDK relié au moteur Docker du poste, et non depuis PowerShell. **Non exécuté** : le workflow modifié, sur GitHub. Signalez au mentor tout écart constaté.
> **Aligné sur .NET 11 le 13 novembre 2026** (SDK 11.0.100 ; `Microsoft.AspNetCore.Mvc.Testing` 11.0.0 ; EF Core et Npgsql restés en 10). Ont été rejoués en .NET 11, à partir de l'état du tag `module-10` dont on a remplacé la plomberie (SDK, cadre cible, `Microsoft.AspNetCore.OpenApi` 11.0.0) : l'étape 1 en entier (`dotnet new xunit3`, `NU1008`, ajout du projet à la solution, paquets, forme du `.csproj`), puis `dotnet build` du projet de tests en Debug (l'erreur du SDK décrite à l'étape 3, et son contournement) et en Release ; `dotnet package add Microsoft.AspNetCore.Mvc.Testing` sans version. Les 144 tests d'intégration de l'état final passent en .NET 11 sur PostgreSQL et SQL Server (vérifié par le coordinateur ; non rejoué dans ce module). **N'ont pas été rejoués** : les étapes 2 à 9 écrites pas à pas depuis le tag `module-10`, le workflow sur GitHub.

Les tests des modules 04 et 09 vérifient les règles du domaine en quelques millisecondes, sans base ni réseau. Ils ne disent rien de ce qui entoure le domaine : une route mal écrite, une annotation de validation oubliée, une date mal sérialisée, une requête LINQ que PostgreSQL refuse, un index unique absent de la migration. Ces erreurs-là ne se voient qu'en faisant ce que fera l'interface Angular : envoyer une requête HTTP à l'API, branchée sur une vraie base.

## Objectifs

À la fin de ce module, vous saurez :

- dire ce qu'un test unitaire ne peut pas voir, et choisir le niveau de test adapté à une règle ;
- démarrer l'API en mémoire dans un test avec `WebApplicationFactory<Program>` ;
- faire démarrer un PostgreSQL jetable par les tests eux-mêmes, avec Testcontainers ;
- isoler les tests les uns des autres sans bibliothèque supplémentaire ;
- remplacer l'horloge de l'application par une horloge que le test pilote ;
- faire tourner ces tests dans le workflow d'intégration continue.

## Prérequis

- Module 10 : l'API expose collaborateurs, clients, projets, affectations, feuilles de temps et absences ; elle lit sa chaîne de connexion dans `ConnectionStrings:Chronos` et son fournisseur dans `Chronos:Fournisseur`.
- Module 09 : `TimeProvider.System` est enregistré dans `Program.cs`, et aucun code métier n'appelle `DateTime.Now`.
- Module 05 : le workflow `.github/workflows/ci.yml` compile et teste la solution.
- Docker Desktop est démarré (module 01). Les tests de ce module créent eux-mêmes leurs conteneurs : le `compose.yaml` du dépôt n'a pas besoin de tourner.

## Règles de gestion

Aucune règle nouvelle. Ce module vérifie de bout en bout des règles déjà écrites : RG-01 (unicité, portée par un index), RG-11 (règle du domaine, traduite en 422), et la validation des entrées du module 08.

## Notions

### La pyramide des tests, appliquée à Chronos

| Niveau | Ce qu'il exécute | Ce qu'il vérifie dans Chronos | Durée |
| --- | --- | --- | --- |
| Test unitaire (`Chronos.Domain.Tests`) | Une classe du domaine, en mémoire | RG-02, RG-03, RG-11 à RG-13, le calcul de charge (RG-20 à RG-25), les transitions d'une feuille de temps | millisecondes |
| Test d'intégration (`Chronos.Api.Tests`) | L'API entière et une vraie base | Routage, validation, sérialisation JSON, codes HTTP, SQL réellement exécuté, index uniques (RG-01, RG-10), filtre d'archivage (RG-05), transactions | dixièmes de seconde |
| Test de bout en bout (module 20) | Le navigateur, l'interface, l'API, la base | Un parcours utilisateur complet | secondes |

On parle de pyramide parce qu'on écrit beaucoup de tests rapides en bas, et peu de tests lents en haut. La règle pratique : **une règle se teste au niveau le plus bas qui peut la voir**. Les quinze cas limites du calcul de charge restent des tests unitaires. Un test d'intégration vérifie seulement que l'endpoint appelle ce calcul et renvoie son résultat dans le bon format.

### `WebApplicationFactory<Program>`

Cette classe du paquet `Microsoft.AspNetCore.Mvc.Testing` démarre votre application **dans le processus de test**, sans ouvrir de port : elle exécute votre `Program.cs`, puis fournit un `HttpClient` branché directement sur le pipeline HTTP. Tout ce que traverse une vraie requête est exécuté : routage, validation, gestion des erreurs, sérialisation.

La fabrique permet aussi de modifier l'application avant son démarrage : changer un réglage, remplacer un service. C'est ainsi qu'on lui donnera la base de test et une horloge pilotée.

`Program` est la classe que le compilateur génère à partir des instructions de `Program.cs`. Jusqu'à .NET 9, elle était `internal`, et tous les tutoriels ajoutent `public partial class Program { }` à la fin du fichier pour que le projet de tests y accède. **Depuis .NET 10, c'est inutile** (et cela le reste en .NET 11) : un générateur de source la déclare publique. Vous le constaterez à l'étape 3.

Documentation : [Tests d'intégration dans ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-11.0), [nouveautés d'ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)

### Testcontainers

Testcontainers est une bibliothèque libre (licence MIT) qui pilote Docker depuis le code de test : elle crée un conteneur, attend qu'il soit prêt, donne sa chaîne de connexion, puis le supprime. Le test n'a besoin de rien d'autre que d'un Docker démarré : ni base installée, ni `docker compose up` préalable, ni port réservé — Testcontainers publie le port 5432 du conteneur sur un port libre choisi au hasard.

À côté de votre conteneur, Testcontainers en démarre un second, nommé `testcontainers-ryuk-…`. C'est son **nettoyeur** : si le processus de test est tué avant d'avoir supprimé ses conteneurs, Ryuk le fait à sa place quelques secondes plus tard.

Documentation : [module PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/), [nettoyage des ressources](https://dotnet.testcontainers.org/api/resource_reaper/)

### Fixtures xUnit v3

xUnit crée une **nouvelle instance de la classe de tests pour chaque test**. Ce qui coûte cher à construire — un conteneur, une application — se partage donc par une *fixture* : un objet que xUnit crée une fois et injecte dans le constructeur des classes de tests.

| Fixture | Durée de vie | Déclaration |
| --- | --- | --- |
| de classe | Tous les tests d'une classe | La classe de tests implémente `IClassFixture<T>` |
| de collection | Toutes les classes d'une collection, qui ne tournent alors plus en parallèle | `[CollectionDefinition]` et `ICollectionFixture<T>` |
| d'assembly (nouveauté de la v3) | Tous les tests du projet | `[assembly: AssemblyFixture(typeof(T))]` |

Une fixture qui a un démarrage asynchrone implémente `IAsyncLifetime`. En v3, ses deux méthodes renvoient `ValueTask` : `InitializeAsync()` et `DisposeAsync()`.

Documentation : [Partager un contexte entre tests](https://xunit.net/docs/shared-context), [nouveautés de xUnit v3](https://xunit.net/docs/getting-started/v3/whats-new)

### Isoler les tests : une base par classe de tests

Deux tests qui écrivent dans la même base peuvent se gêner : l'un crée le matricule `C-0001`, l'autre vérifie que la liste est vide. Le cours retient l'organisation suivante :

- **un seul conteneur PostgreSQL** pour tout le projet de tests (fixture d'assembly) : le démarrage, qui prend quelques secondes, n'est payé qu'une fois ;
- **une base de données par classe de tests** (fixture de classe) : chaque classe reçoit sa propre fabrique, qui crée une base au nom unique et y applique les migrations ;
- à l'intérieur d'une classe, les tests partagent la base : ils s'exécutent l'un après l'autre et utilisent chacun leurs propres données (un matricule différent par test).

Ce choix n'ajoute aucune dépendance, laisse les classes tourner en parallèle, et vérifie au passage que **les migrations s'appliquent sur une base vide**. C'est ce que recommande la documentation d'EF Core : créer autant de bases de test que nécessaire n'est pas un problème.

L'autre approche courante consiste à garder une seule base et à la vider entre deux tests. La bibliothèque Respawn le fait efficacement ; elle est libre (Apache 2.0) et maintenue. Elle devient intéressante quand les migrations sont longues ; ce n'est pas encore le cas de Chronos.

Documentation : [Tester avec le vrai SGBD](https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database), [exécution en parallèle](https://xunit.net/docs/running-tests-in-parallel)

### Pourquoi pas une base en mémoire

Beaucoup de tutoriels remplacent la base par le fournisseur `InMemory` d'EF Core, ou par SQLite en mémoire — la page d'ASP.NET Core sur les tests d'intégration montre elle-même SQLite. La documentation d'EF Core, elle, est nette :

- le fournisseur **InMemory** est « fortement déconseillé » : il n'exécute pas de SQL, ignore les transactions, et n'est pas une base relationnelle. Les transactions du module 10 passeraient sans rien prouver ;
- **SQLite** est un autre SGBD : comparaison de chaînes, types, fonctions et dialecte SQL diffèrent. Un test vert sur SQLite ne garantit pas que la requête fonctionne sur PostgreSQL ou sur SQL Server.

Chronos doit tourner sur deux SGBD : la seule façon de le savoir est de tester sur chacun.

Documentation : [Choisir une stratégie de test](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy)

## Pas à pas

Les commandes s'exécutent dans `src/api`.

### 1. Créer le projet de tests

```powershell
dotnet new xunit3 -f net10.0 --name Chronos.Api.Tests
```

Comme au [module 04](04-modeliser-le-domaine.md), la commande se termine par l'erreur `NU1008` : le modèle a écrit une version de paquet dans le `.csproj`, ce que la gestion centralisée refuse. La correction est la même (étape 4 du module 04) ; la version de `xunit.v3.mtp-v2` est déjà dans `Directory.Packages.props`. La commande affiche aussi `Successfully modified global.json` : le fichier contient déjà la section `test` depuis le module 04, et le modèle ne fait qu'en retirer le saut de ligne final. Annulez cette modification (`git restore global.json`).

> **`-f net10.0`, et pas `net11.0`.** Avec le SDK 11.0.100, `dotnet new xunit3 -f net11.0` est refusé : « 'net11.0' is not a valid value for -f », et la liste des valeurs permises s'arrête à `net10.0` (le modèle vient du paquet `xunit.v3.templates` 4.0.3, installé au module 04 par `dotnet new install xunit.v3.templates` ; sans `-f`, il écrit `net8.0`). La valeur ne compte pas : la ligne `<TargetFramework>` du `.csproj` est supprimée un peu plus bas, le cadre cible venant de `Directory.Build.props` (`net11.0`). Prenez donc la valeur la plus haute que le modèle accepte. **Si vous oubliez de supprimer la ligne**, la restauration échoue (rejoué) : `NU1201` « Project Chronos.Api is not compatible with net10.0 » et `NU1202` pour `Mvc.Testing` 11.0.0.

```powershell
dotnet sln add Chronos.Api.Tests
```

Supprimez `UnitTest1.cs`. Ajoutez les versions des nouveaux paquets dans `Directory.Packages.props` (ne dupliquez pas `Microsoft.Extensions.TimeProvider.Testing` si le module 09 l'y a déjà mise) :

```xml
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="11.0.0" />
<PackageVersion Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.11.0" />
<PackageVersion Include="Testcontainers.PostgreSql" Version="4.16.0" />
```

Puis donnez cette forme à `Chronos.Api.Tests.csproj` (les propriétés communes viennent de `Directory.Build.props`) :

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <Content Update="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />
    <PackageReference Include="Testcontainers.PostgreSql" />
    <PackageReference Include="xunit.v3.mtp-v2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Chronos.Api\Chronos.Api.csproj" />
  </ItemGroup>

</Project>
```

Trois différences avec le fichier produit par le modèle :

- **`Sdk="Microsoft.NET.Sdk.Web"`** : la documentation des tests d'intégration le demande pour le projet de tests.
- **`Content Update`** au lieu de `Content Include` : le SDK web inclut déjà les fichiers `.json` du projet. Avec `Include`, la compilation échoue (`NETSDK1022: Duplicate 'Content' items`).
- **Pas de numéro de version** sur les paquets, et pas de `TargetFramework`, `Nullable` ni `ImplicitUsings` : tout cela est centralisé depuis le module 03.

La référence au projet de l'API peut aussi s'ajouter par `dotnet reference add Chronos.Api --project Chronos.Api.Tests`, comme au module 04.

> **Les numéros de version de ces paquets ne sont pas tous les mêmes.** `Microsoft.AspNetCore.Mvc.Testing` suit ASP.NET Core : **11.0.0**, la même que `Microsoft.AspNetCore.OpenApi` (rejoué : `dotnet package add Microsoft.AspNetCore.Mvc.Testing --project Chronos.Api.Tests`, sans `--version`, écrit bien `11.0.0`). `Microsoft.Extensions.TimeProvider.Testing` reste en `10.11.0` et Testcontainers en `4.16.0`, qui ne suivent pas le numéro du SDK. Écrire les numéros soi-même, dans `Directory.Packages.props`, évite qu'une commande sans `--version` n'en choisisse un autre le jour où une version plus récente paraît.

### 2. Un serveur PostgreSQL pour tous les tests

Créez `ServeurPostgres.cs` :

```csharp
using Chronos.Api.Tests;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ServeurPostgres))]

namespace Chronos.Api.Tests;

public sealed class ServeurPostgres : IAsyncLifetime
{
    private readonly PostgreSqlContainer _conteneur = new PostgreSqlBuilder("postgres:18").Build();

    public async ValueTask InitializeAsync() => await _conteneur.StartAsync();

    public async ValueTask DisposeAsync() => await _conteneur.DisposeAsync();

    public string ChaineDeConnexion(string nomDeBase) =>
        new NpgsqlConnectionStringBuilder(_conteneur.GetConnectionString()) { Database = nomDeBase }.ConnectionString;
}
```

- **`[assembly: AssemblyFixture(...)]`** : xUnit crée un `ServeurPostgres` avant le premier test du projet, et le détruit après le dernier.
- **`new PostgreSqlBuilder("postgres:18")`** : l'image est un argument obligatoire. C'est la même que dans `compose.yaml` : les tests tournent sur la version de PostgreSQL que vous utilisez en développement.
- **`StartAsync`** télécharge l'image si nécessaire, crée le conteneur et rend la main quand PostgreSQL accepte les connexions.
- **`ChaineDeConnexion`** reprend la chaîne fournie par Testcontainers (hôte, port aléatoire, identifiants par défaut) en changeant seulement le nom de la base. Chaque classe de tests demandera la sienne.

### 3. La fabrique de l'application

Créez `ChronosApiFactory.cs` :

```csharp
using Chronos.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Chronos.Api.Tests;

public sealed class ChronosApiFactory(ServeurPostgres serveur) : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _chaineDeConnexion = serveur.ChaineDeConnexion($"chronos_{Guid.NewGuid():N}");

    public FakeTimeProvider Horloge { get; } = new(new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Chronos", _chaineDeConnexion);
        builder.UseSetting("Chronos:Fournisseur", "PostgreSql");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Horloge);
        });
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChronosDbContext>();
        await db.Database.MigrateAsync();
    }
}
```

Ce qu'il faut y lire :

- **Le constructeur reçoit `ServeurPostgres`** : une fixture de classe peut dépendre de la fixture d'assembly ; xUnit la lui injecte.
- **`UseSetting`** remplace un réglage de configuration. La valeur l'emporte sur `appsettings.Development.json` et sur vos secrets de développement, et — point décisif — elle est visible **dès le début de `Program.cs`**.
- **`Chronos:Fournisseur` est fixé explicitement.** Dans le `switch` du [module 07](07-deux-sgbd-un-seul-code.md), cette clé choisit à la fois le fournisseur et l'assembly de migrations (`Chronos.Migrations.PostgreSql`). Sans cette ligne, les tests suivraient vos secrets de développement : il suffirait qu'ils désignent SQL Server pour que `UseSqlServer` reçoive une chaîne de connexion PostgreSQL.
- **`ConfigureTestServices`** s'exécute après l'enregistrement des services de l'application : on y retire le `TimeProvider` réel pour le remplacer par un `FakeTimeProvider`, réglé sur le lundi 4 mai 2026, la semaine de l'exemple du cahier des charges.
- **`InitializeAsync`** crée la base et applique toutes les migrations du jeu désigné par `MigrationsAssembly`. Le projet de tests n'a rien à référencer de plus : les deux projets de migrations lui arrivent par sa référence à `Chronos.Api`. L'accès à `Services` démarre l'application.
- **L'environnement reste `Development`**, valeur par défaut de la fabrique. `MigrateAsync` exécute donc aussi les données initiales du module 06 : la base d'une classe de tests **n'est pas vide**. Un test ne compte jamais « tous » les collaborateurs ; il cherche les siens, avec des matricules qui ne sont pas ceux des données initiales.
- `WebApplicationFactory` sait déjà se libérer : inutile d'écrire `DisposeAsync`.

Compilez :

```powershell
dotnet build
```

Avec le SDK 11.0.100, **en `Debug`, la compilation échoue sur le projet de tests** (c'est la configuration par défaut de `dotnet build` ; un éditeur qui compile tout seul vous la montrera dès l'étape 1) :

```text
Microsoft.NET.Sdk.StaticWebAssets.JSModules.targets(233,3): error : The following scoped css bundles do not have matching endpoints: _framework/Microsoft.NET.Sdk.BlazorWeb.HotReload#[.{fingerprint}]!.lib.module.js [.../Chronos.Api.Tests/Chronos.Api.Tests.csproj]
```

Ce n'est pas votre code : c'est un défaut du SDK (dotnet/sdk, [ticket 56656](https://github.com/dotnet/sdk/issues/56656), ouvert à la date de ce cours). Il touche tout projet du SDK Web qui en référence un autre — exactement le cas de `Chronos.Api.Tests`, qui doit être du SDK Web (étape 1) et référence `Chronos.Api`. En `Release`, l'erreur n'apparaît pas (rejoué : `dotnet build -c Release` réussit). Le contournement tient en une ligne dans `Chronos.Api.Tests.csproj`, dans le premier `PropertyGroup` :

```xml
<!-- Contournement d'un défaut du SDK 11.0.100 : en Debug, un projet du SDK Web qui en référence un autre échoue sur -->
<!-- « scoped css bundles do not have matching endpoints » (dotnet/sdk, ticket 56656). Ce projet ne sert aucun fichier -->
<!-- statique : la ligne est sans effet sur les tests. À retirer quand le SDK est corrigé. -->
<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>
```

`StaticWebAssetsEnabled` à `false` désactive la machinerie des « ressources web statiques » du SDK (le manifeste qui permet à une application web de servir les fichiers de `wwwroot` et des bibliothèques de classes Razor), et avec elle la vérification qui échoue. Elle est sans risque ici : le projet de tests est un hôte pour `WebApplicationFactory`, il ne sert aucun fichier statique, et l'API de Chronos n'en sert pas non plus (les 144 tests de la solution de référence passent avec cette ligne). **Elle se retirera** quand le SDK sera corrigé : essayez de la supprimer à chaque mise à jour du SDK, et gardez le commentaire pour que le prochain lecteur sache pourquoi elle est là. Recompilez : la compilation réussit.

La compilation réussit alors que `Program.cs` ne contient aucune ligne `public partial class Program { }` : la classe est bien publique. Si cette ligne traîne dans votre `Program.cs`, supprimez-la ; l'analyseur [ASP0027](https://learn.microsoft.com/en-us/aspnet/core/diagnostics/asp0027) le suggère dans l'éditeur.

> **Pourquoi `UseSetting` et pas `ConfigureAppConfiguration` ?** La documentation décrit plusieurs façons de fournir une configuration de test. Celle qu'on trouve dans la plupart des tutoriels, `builder.ConfigureAppConfiguration(...)`, n'est appliquée qu'**après** `builder.Build()`. Or `Program.cs` lit `Chronos:Fournisseur` et la chaîne de connexion avant, dans deux variables, pour choisir entre `UseNpgsql` et `UseSqlServer` (module 07). L'essai le confirme : avec `ConfigureAppConfiguration`, `Program.cs` lit encore la valeur du fichier de développement ; avec `UseSetting`, il lit celle du test.

Depuis .NET 11, la documentation décrit aussi `ConfigureWebApplicationBuilder`, dont la configuration est visible dès `WebApplication.CreateBuilder` ; `UseSetting` suffit à Chronos (non essayé ici).

Documentation : [Personnaliser `WebApplicationFactory`](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-11.0), [Testcontainers avec ASP.NET Core](https://dotnet.testcontainers.org/examples/aspnet/)

### 4. Un premier test : créer puis relire

Créez `CollaborateursTests.cs` :

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
}
```

- **`IClassFixture<ChronosApiFactory>`** : une fabrique, donc une base, pour cette classe.
- **`factory.CreateClient()`** renvoie un `HttpClient` relié à l'application en mémoire. Il suit les redirections et conserve les cookies, ce qui servira au module 12.
- **Le corps de la requête est un objet anonyme** qui porte tous les champs obligatoires de `CreerCollaborateurRequete`, y compris le premier contrat (`typeContrat`, `quotite`) exigé depuis le module 08 ; sans eux, la réponse est 400. La réponse est lue comme du JSON brut. C'est volontaire : le test écrit le JSON **tel que l'interface l'enverra**. S'il réutilisait `CreerCollaborateurRequete` et `CollaborateurDto`, un renommage de propriété casserait le contrat avec Angular sans faire échouer aucun test.
- Les trois dernières assertions vérifient la sérialisation : noms en camelCase, énumération en chaîne, date au format `2024-09-02`. Aucun test unitaire ne voit cela.
- **`TestContext.Current.CancellationToken`** est le jeton d'annulation du test en cours. L'analyseur de xUnit v3 signale (xUnit1051) tout appel asynchrone qui ne le reçoit pas.

Lancez :

```powershell
dotnet test --project Chronos.Api.Tests
```

La première exécution télécharge l'image du nettoyeur de Testcontainers. Pendant le test, `docker ps` dans un autre terminal montre deux conteneurs : `postgres:18` et `testcontainers/ryuk`. Quelques secondes après la fin, ils ont disparu.

### 5. Les erreurs : 400, 409, 422

Ajoutez à la même classe :

```csharp
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
        await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0002"), ct);

        var doublon = await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0002"), ct);

        Assert.Equal(HttpStatusCode.Conflict, doublon.StatusCode);
    }
```

Le premier test vérifie que `AddValidation()` est bien branché et que la réponse suit le format `ProblemDetails`. Si les clés de `errors` ne sont pas celles que vous attendiez, ce n'est pas le test qui a tort : c'est le contrat réel de votre API, celui que le module 17 devra lire.

Le second vérifie RG-01 de bout en bout. Chaque test utilise **son propre matricule** : c'est ce qui permet aux tests de la classe de partager la base.

Pour une règle du domaine, créez `ProjetsTests.cs` sur le même modèle, avec ce test :

```csharp
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
```

RG-11 est déjà couverte par un test unitaire du module 04. Ce test-ci vérifie autre chose : que l'`IExceptionHandler` du module 08 transforme bien `RegleMetierException` en réponse 422 portant le code de la règle. Un seul test de ce genre suffit ; les variantes de la règle restent au niveau unitaire.

`ProjetsTests` a sa propre fabrique, donc sa propre base : les deux classes peuvent tourner en même temps.

### 6. Ce que seule la vraie base peut dire

Un test d'intégration peut aussi atteindre les services de l'application. Ajoutez à `CollaborateursTests`, avec les `using` de `Chronos.Domain`, `Chronos.Infrastructure`, `Microsoft.EntityFrameworkCore` et `Microsoft.Extensions.DependencyInjection` :

```csharp
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
```

Adaptez les arguments au constructeur public de votre `Collaborateur` (module 04). Ce test contourne l'endpoint et sa vérification préalable : c'est **la base** qui refuse le doublon. Si l'index unique manquait dans la migration, deux requêtes simultanées pourraient créer deux fois le même matricule, et aucun autre test ne le dirait. Avec le fournisseur InMemory, ce test échouerait : il n'y a pas d'index.

`factory.Services.CreateScope()` reproduit ce que fait ASP.NET Core pour chaque requête : `ChronosDbContext` a une durée de vie *scoped*, il lui faut une portée.

### 7. Piloter le temps

Le filtre `statut=actif|sorti` de la liste compare la date de sortie à la date du jour. La création n'accepte pas de date de sortie (`CreerCollaborateurRequete` n'en a pas) : le test la renseigne par une modification, qui exige la `version` lue à la création (module 08). Ajoutez :

```csharp
    [Fact]
    public async Task Le_filtre_sorti_depend_de_la_date_du_jour()
    {
        var ct = TestContext.Current.CancellationToken;
        var creation = await _client.PostAsJsonAsync("/api/collaborateurs", NouveauCollaborateur("C-0004"), ct);
        var cree = await creation.Content.ReadFromJsonAsync<JsonElement>(ct);

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
```

Le 4 mai, Camille n'est pas encore sortie. Le test avance l'horloge de trente jours, et la même requête donne un autre résultat. Aucun `Thread.Sleep`, aucun test qui ne passerait que certains jours.

Deux limites à connaître. L'horloge appartient à la fabrique : elle est **partagée par les tests de la classe**, et un test qui l'avance le fait pour les suivants. Et un `FakeTimeProvider` ne recule pas. Un test qui dépend de la date exprime donc ses dates par rapport à `factory.Horloge.GetUtcNow()`, ou vit dans une classe à part.

Documentation : [`TimeProvider`](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview), [`FakeTimeProvider`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.time.testing.faketimeprovider)

### 8. Lire une exécution

```powershell
dotnet test
```

Sans option, la commande exécute tous les projets de tests de la solution : les tests unitaires et les tests d'intégration. Lors de l'essai, les tests de ce pas à pas ont pris environ six secondes, démarrage de PostgreSQL compris.

Provoquez un échec pour savoir le lire : dans `ServeurPostgres`, remplacez `postgres:18` par `postgres:99`. Tous les tests échouent avec le même message, qui désigne la fixture et non un test : `Assembly fixture type 'Chronos.Api.Tests.ServeurPostgres' threw in InitializeAsync`, suivi de la cause. Quand tous les tests d'un projet tombent en même temps, cherchez dans une fixture, pas dans les tests. Remettez `postgres:18`.

### 9. Dans l'intégration continue

Poussez votre branche sans toucher au workflow, et regardez l'exécution. Le job `API` du module 05 lance `dotnet test` sur la solution : il exécute déjà le nouveau projet. Et les tests trouvent un Docker : il est préinstallé sur les machines `ubuntu-24.04`. Testcontainers n'a besoin d'aucun réglage sur les machines hébergées par GitHub ; **la modification minimale du workflow est donc… aucune**.

Deux ajustements rendent pourtant l'exécution plus lisible. Remplacez l'étape `Tester` par :

```yaml
      - name: Tests unitaires
        run: dotnet test --no-build --configuration Release --project Chronos.Domain.Tests --report-xunit-trx --results-directory TestResults

      - name: Tests d'intégration
        run: dotnet test --no-build --configuration Release --project Chronos.Api.Tests --report-xunit-trx --results-directory TestResults
```

- **Deux étapes** : si une règle du domaine est cassée, l'étape rapide échoue seule et le message est immédiat. Si seule la seconde échoue, on sait que le problème est dans l'API, la base ou Docker. Chaque étape affiche sa durée.
- **Un seul dossier de résultats** : chaque projet y écrit son propre fichier TRX. L'étape `Conserver les résultats des tests` reste inchangée.
- **`timeout-minutes: 15`** reste suffisant. Surveillez tout de même la durée de la seconde étape : elle inclut le téléchargement de `postgres:18`, à chaque exécution puisque la machine est neuve.

Le workflow n'ajoute aucune action : `actions.lock` ne change pas.

> **L'autre technique** que vous verrez est le bloc `services:` du job, qui demande à GitHub de démarrer un conteneur PostgreSQL à côté des étapes ([documentation](https://docs.github.com/en/actions/tutorials/use-containerized-services/create-postgresql-service-containers)). Elle fonctionne, mais les tests dépendent alors d'une base préparée par le workflow : ils ne se lancent plus de la même façon sur votre poste et sur GitHub. Avec Testcontainers, la commande est la même partout.

Documentation : [Testcontainers en intégration continue](https://dotnet.testcontainers.org/cicd/)

## À vous

1. **Clients et projets.** Écrivez les tests d'intégration du cycle complet d'un client, puis d'un projet : création, lecture, modification, liste (paginée pour les projets ; celle des clients est un simple tableau). Ajoutez le test de RG-10 (code de projet déjà utilisé → 409). Pour quelles règles de ces deux ressources un test d'intégration est-il justifié, et pour lesquelles le test unitaire suffit-il ?

2. **Archivage (RG-05).** Créez un collaborateur, donnez-lui une affectation, puis appelez `DELETE /api/collaborateurs/{id}`. Vérifiez par l'API qu'il n'apparaît plus dans la liste, puis, par `ChronosDbContext`, que sa ligne existe toujours. Il vous faudra désactiver dans le test le filtre nommé du module 06 : `IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])`.

3. **Concurrence optimiste.** Lisez un projet, puis envoyez deux `PUT` avec la même valeur de `version`. Que doit renvoyer le second ? Écrivez le test, puis demandez-vous pourquoi il serait trompeur avec une base en mémoire.

4. **L'exemple de Camille, par HTTP.** Reproduisez l'exemple chiffré du cahier des charges : jour férié du 8 mai, absence du lundi, deux affectations à 50 %. Appelez `GET /api/plan-de-charge?du=2026-05-04&au=2026-05-08&pas=semaine` et vérifiez la capacité, la charge et le taux décidés au module 09. Ce test fait-il double emploi avec les tests unitaires du calcul ?

5. **Les mêmes tests sur SQL Server.** Ajoutez le paquet `Testcontainers.MsSql` (4.16.0). Une seconde fixture d'assembly démarrerait les deux conteneurs à chaque exécution : remplacez plutôt `ServeurPostgres` par une fixture `ServeurDeBase` qui lit la variable d'environnement `CHRONOS_TESTS_SGBD` (`PostgreSql` par défaut, ou `SqlServer`) et ne démarre que le conteneur choisi — `new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")` pour SQL Server, l'image est obligatoire là aussi. Les deux conteneurs ont un type commun, `IDatabaseContainer` (espace de noms `DotNet.Testcontainers.Containers`). La fixture expose aussi le `Fournisseur` choisi, que la fabrique passe à `UseSetting("Chronos:Fournisseur", ...)` : `MigrateAsync` applique alors le jeu `Chronos.Migrations.SqlServer`. `ChaineDeConnexion` utilise `NpgsqlConnectionStringBuilder` : quel est son équivalent ? Tous les tests passent-ils sans modification ? Essayez ensuite, sur une branche, de faire tourner le job sur les deux SGBD avec `strategy.matrix` : combien de temps cela ajoute-t-il, et que devient le nom du job, que la protection de `main` exige depuis le module 05 sous le nom `API` ? La solution de référence ne garde pas la matrice : le workflow reste celui de l'étape 9, sur PostgreSQL.

6. **Mesurer.** Combien de temps dure un test d'intégration, hors démarrage du conteneur ? Et un test unitaire ? Passez provisoirement à une seule base pour tout le projet de tests (un nom fixe au lieu de `Guid.NewGuid()`) et relancez plusieurs fois : que se passe-t-il, et pourquoi ? Lisez les messages : quelle étape échoue, avant même le premier test ? Remettez le `Guid`.

## Critères de réussite

- `dotnet test`, lancé dans `src/api` sans qu'aucune base ne tourne, réussit. Seul Docker doit être démarré.
- Après l'exécution, `docker ps -a` ne montre aucun conteneur laissé par les tests.
- `Program.cs` ne contient pas `public partial class Program`.
- Le projet de tests ne référence ni `Microsoft.EntityFrameworkCore.InMemory` ni SQLite.
- Les tests couvrent au moins : une création suivie d'une lecture, un 400, un 409, un 422, et un comportement qui dépend de la date.
- Lancer deux fois de suite `dotnet test` donne deux fois le même résultat.
- Sur GitHub, le job `API` exécute les tests unitaires puis les tests d'intégration ; l'artefact contient un fichier TRX par projet.
- Comparez avec la solution de référence : `git diff module-10 module-11`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `public partial class Program { }` à la fin de `Program.cs` | Inutile depuis .NET 10 (et en .NET 11) : un générateur de source rend `Program` publique. | Supprimer la ligne. |
| `dotnet build` du projet de tests en `Debug` : « scoped css bundles do not have matching endpoints » | Défaut du SDK 11.0.100 (dotnet/sdk, ticket 56656) : un projet du SDK Web qui en référence un autre échoue en Debug. Constaté ; absent en Release. | `<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>` dans `Chronos.Api.Tests.csproj`, avec son commentaire ; à retirer quand le SDK sera corrigé. |
| `dotnet new xunit3 -f net11.0` | Refusé par le SDK 11.0.100 : le modèle s'arrête à `net10.0`. Constaté. | `-f net10.0` ; la ligne `<TargetFramework>` du modèle est supprimée de toute façon. |
| `UseInMemoryDatabase(...)` dans la fabrique | Pas de SQL, pas de transactions, pas d'index : les tests passent sans rien prouver. Fortement déconseillé par la documentation d'EF Core. | Le vrai SGBD, avec Testcontainers. |
| SQLite en mémoire « pour aller vite » | Un autre SGBD, avec un autre dialecte et d'autres comparaisons. | Le vrai SGBD ; un PostgreSQL local démarre en quelques secondes. |
| `new PostgreSqlBuilder()` sans argument | Refusé depuis Testcontainers 4.10 : l'image doit être nommée. | `new PostgreSqlBuilder("postgres:18")`. |
| `builder.ConfigureAppConfiguration(...)` pour la chaîne de connexion | Appliqué après `builder.Build()` : `Program.cs` a déjà lu l'ancienne valeur. | `builder.UseSetting("ConnectionStrings:Chronos", ...)`. |
| `EnsureCreated()` dans la fixture | Crée le schéma sans passer par les migrations : une migration fausse ou manquante n'est pas détectée. | `Database.MigrateAsync()`. |
| `Task InitializeAsync()` et `Task DisposeAsync()` | Signatures de xUnit v2 : elles ne compilent pas en v3. | `ValueTask` pour les deux. |
| Une seule base pour toutes les classes de tests | Les classes tournent en parallèle : leurs fabriques appliquent les migrations en même temps sur la même base, et toutes sauf une échouent (`42P07: relation … already exists`) ; ensuite, leurs données se mélangent. | Une base par classe, ou une collection xUnit pour les classes qui doivent partager. |
| Deux tests de la même classe qui créent le même matricule | Le second reçoit 409 selon l'ordre d'exécution. | Des données propres à chaque test. |
| Désérialiser les réponses dans les DTO de l'API | Un renommage casse le contrat JSON sans faire échouer le test. | Lire le JSON brut, ou des types propres au projet de tests. |
| Tester par HTTP chaque cas limite d'une règle du domaine | Lent, et redondant avec les tests unitaires. | Un test d'intégration par chemin (succès, 400, 409, 422) ; les cas limites restent unitaires. |
| `TESTCONTAINERS_RYUK_DISABLED=true` recopié d'un tutoriel | Sans nettoyeur, un test interrompu laisse ses conteneurs derrière lui. | Laisser Ryuk actif. |
| `dotnet test --logger trx` | Option de VSTest, refusée par Microsoft.Testing.Platform. | `--report-xunit-trx`. |
| `toomanyrequests` au téléchargement d'une image | Docker Hub limite les téléchargements anonymes par adresse IP. | Relancer ; si cela se répète, s'authentifier auprès de Docker Hub. |

## Pour la séance

- Pourquoi ne pas tout tester par des tests d'intégration, puisqu'ils « testent tout » ? Et pourquoi ne pas s'en passer, puisque les tests unitaires sont si rapides ?
- Le test de création envoie un objet anonyme plutôt qu'un `CreerCollaborateurRequete`. Qu'y gagne-t-on, qu'y perd-on ?
- Une base par classe de tests, ou une base vidée entre deux tests : quels critères feraient changer de choix ?
- Un test échoue une fois sur dix dans l'intégration continue et jamais sur votre poste. Par où commencez-vous ?
- Le remplacement de `TimeProvider` par la fabrique fonctionne parce que personne n'appelle `DateTime.Now`. Comment s'en assurer durablement ?
- Les tests d'intégration appliquent les migrations sur une base vide. Quel scénario de déploiement cela ne couvre-t-il pas ?

## Pour aller plus loin

- [Respawn](https://github.com/jbogard/Respawn) : remettre une base à zéro entre deux tests, quand recréer une base par classe devient trop long.
- [Testcontainers et xUnit](https://dotnet.testcontainers.org/test_frameworks/xunit_net/) : le paquet `Testcontainers.XunitV3` fournit des fixtures prêtes à l'emploi, qui recopient les journaux du conteneur dans la sortie des tests.
- [Module SQL Server de Testcontainers](https://dotnet.testcontainers.org/modules/mssql/), pour l'exercice 5.
- [Lancer les tests depuis un conteneur](https://dotnet.testcontainers.org/dind/) : quand `dotnet test` tourne lui-même dans un conteneur relié au Docker de l'hôte, Testcontainers a besoin de la variable `TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal` sous Docker Desktop. C'est ainsi que ce module a été vérifié.
- [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro) : les options de `dotnet test`, dont `--filter-class` pour n'exécuter qu'une classe.
- Suite dans le cours : le module 12 protège l'API ; les tests de ce module devront alors se connecter. Le module 20 teste l'interface.
