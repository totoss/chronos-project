# Référentiel .NET 11, EF Core 10 et bases de données — pratiques à jour

> **Vérifié le 12 novembre 2026** sur learn.microsoft.com, dotnet.microsoft.com, npgsql.org, postgresql.org, xunit.net, aspire.dev et nuget.org, pour la construction du cours en .NET 10. **Aligné sur .NET 11 le 13 novembre 2026** : les points qui dépendent de la version de .NET ont été relus sur la documentation de la version 11 ou rejoués avec le SDK 11.0.100 ; la colonne « En .NET 11 » des constats et la section « Constats du 13 novembre 2026 » disent ce qui l'a été. Ce qui n'a pas été rejoué est marqué comme tel.
> Ce document fait foi pour le cours : en cas de doute entre un tutoriel trouvé en ligne et ce référentiel, c'est le référentiel — ou mieux, la documentation officielle du jour — qui a raison.
> À revérifier avant d'écrire chaque module, et à chaque correctif mensuel de .NET.

## 1. Versions

| Élément | Valeur | Source |
| --- | --- | --- |
| .NET 11 (STS) | 11.0.0, SDK 11.0.100 | [téléchargements](https://dotnet.microsoft.com/en-us/download/dotnet/11.0) |
| Fin de support de .NET 11 | 14 novembre 2028 | [politique de support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) |
| .NET 10 (LTS), pour mémoire | Prise en charge jusqu'au 14 novembre 2028, comme .NET 11 | [politique de support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) |
| Langage | C# 15, version par défaut d'un projet `net11.0` avec le SDK 11 | [nouveautés](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15) |
| EF Core | **10.0.15** (LTS). EF Core 11.0.0 existe, mais le fournisseur PostgreSQL ne le suit pas encore : voir ci-dessous. | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore) |
| Fournisseur SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15 | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.15) |
| Fournisseur PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` **10.0.3** (dernière version stable, juillet 2026). Une préversion `11.0.0-rc.3` existe depuis le 28 octobre 2026. | [NuGet](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/absoluteLatest), [notes de version](https://www.npgsql.org/efcore/release-notes/10.0.html) |
| SQL Server | 2025 (17.x) | [nouveautés](https://learn.microsoft.com/en-us/sql/sql-server/what-s-new-in-sql-server-2025) |
| PostgreSQL | 18 (18.6) | [versions](https://www.postgresql.org/support/versioning/) |
| xUnit | v3 — `xunit.v3` 4.0.3 | [versions](https://xunit.net/releases/) |
| Testcontainers pour .NET | 4.16.0 | [NuGet](https://www.nuget.org/packages/Testcontainers) |
| Aspire | 13.6.5 | [NuGet](https://www.nuget.org/packages/Aspire.Hosting.AppHost) |

SQL Server, PostgreSQL, xUnit, Testcontainers et Aspire : valeurs relevées le 12 novembre 2026 et non revérifiées le 13 ; la solution de référence les utilise telles quelles sous .NET 11.

### Pourquoi .NET 11 ?

**.NET 11 est sorti le 10 novembre 2026** ([annonce](https://devblogs.microsoft.com/dotnet/announcing-dotnet-11/)). C'est une version STS ; .NET 10 est la LTS. D'après la [politique de support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), les deux sont prises en charge **jusqu'à la même date, le 14 novembre 2028**. .NET 8 et .NET 9 ne sont plus pris en charge depuis le 10 novembre 2026.

Le cours cible **.NET 11** (décision du 13 novembre 2026 ; il avait été construit et rejoué en .NET 10, et les tags `module-NN` en gardent l'état). Les raisons, sans plaidoyer :

- C'est la version courante : le SDK qu'installe un poste neuf aujourd'hui est le 11 (WinGet : `Microsoft.DotNet.SDK.11`, [dépôt des paquets](https://github.com/microsoft/winget-pkgs/tree/master/manifests/m/Microsoft/DotNet/SDK)), celui des images Docker `11.0` et celui de l'image Ubuntu 24.04 des machines GitHub (image 20261111).
- La LTS n'offre pas de fenêtre de support plus longue : les deux versions s'arrêtent le même jour. Le critère « prise en charge plus longue » qui justifiait la LTS ne joue donc pas ici.
- Partir de la version courante évite d'installer un SDK ancien à côté du SDK récent, et de lutter contre des paquets (`Microsoft.AspNetCore.*`) plus récents que le projet.

Ce que ce choix coûte, aussi :

- **Une STS.** Elle s'arrête le 14 novembre 2028, comme .NET 10 : sur la durée de prise en charge, le choix est neutre, et dans les deux cas il faudra migrer avant cette date. La différence est ailleurs : une STS est moins adoptée, l'écosystème la suit moins vite (voir le fournisseur PostgreSQL ci-dessous), et une entreprise qui ne standardise que sur des LTS ne l'acceptera pas.
- **Un SDK tout neuf.** Le 11.0.100 a un défaut connu qui touche la solution du cours (voir « Constats du 13 novembre 2026 »).
- **Un écosystème qui n'a pas fini de suivre.** Le fournisseur PostgreSQL d'EF Core n'existe en version 11 qu'en préversion : **EF Core reste en 10.0.15** (une LTS, prise en charge jusqu'au 10 novembre 2028 ; elle cible .NET 10 et fonctionne sur .NET 11 par compatibilité ascendante — constaté par les 369 tests de la solution, non garanti par la documentation d'EF Core, qui ne lie EF Core 10 qu'à .NET 10), avec `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3. Les paquets `Microsoft.AspNetCore.*` se prennent en 11.0.0, les paquets `Microsoft.EntityFrameworkCore.*` en 10.0.15 : deux numéros dans le même projet, et un piège à la clé (voir ci-dessous).

Trois conséquences pratiques :

- Le SDK 11 suffit : pas de SDK 10 à installer. `global.json` à la racine de la solution (`src/api/`) fixe tout de même la version, pour que le poste, celui du mentor et l'intégration continue utilisent le même ([global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)).
- **Le piège des versions se déplace.** Sans numéro de version, `dotnet package add` (et `dotnet tool install`) prennent la dernière version publiée : 11.0.0 pour tous les paquets Microsoft, EF Core compris. Pour `Microsoft.AspNetCore.*` c'est ce qu'il faut ; pour `Microsoft.EntityFrameworkCore.*`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` et `dotnet-ef`, il faut écrire **`--version 10.0.15`**, sinon le fournisseur PostgreSQL signale `NU1608`. Les essais sont dans « Constats du 13 novembre 2026 ».
- La documentation Microsoft se lit avec le sélecteur de version positionné sur **ASP.NET Core 11** (`?view=aspnetcore-11.0` dans l'adresse). Sans `?view=`, la page s'ouvre en version 10 : le site garde la LTS comme version par défaut (constaté le 13 novembre 2026 dans les métadonnées de la page [Vue d'ensemble des API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-11.0)). Pour EF Core, les liens restent ceux d'EF Core 10.

Question pour la séance : à quelles conditions choisiriez-vous la LTS plutôt que la STS pour une application qui doit vivre cinq ans ?

## 2. Ce que le cours enseigne

### Projet et outillage

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Solution | Format `.slnx`, créé par défaut par `dotnet new sln` (rejoué avec le SDK 11.0.100). | [slnx](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default) |
| Projet d'API | `dotnet new webapi` : Minimal APIs et OpenAPI intégré, sans Swashbuckle. Le modèle du SDK 11 cible `net11.0` et référence `Microsoft.AspNetCore.OpenApi` 11.0.0 (rejoué). | [modèles](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates) |
| Démarrage | `WebApplication.CreateBuilder(args)` dans `Program.cs`. Pas de classe `Startup`. | [démarrage](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-11.0) |
| Versions des paquets | Gestion centralisée dans `Directory.Packages.props`. | [gestion centralisée](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) |
| Version du SDK | `global.json`. | [global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) |
| Essais rapides | Application en un seul fichier : `dotnet run essai.cs`. | [apps fichier unique](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps) |

### API web

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Style d'API | **Minimal APIs**, recommandées pour tout nouveau projet. Les contrôleurs sont présentés comme l'alternative que le mentoré croisera dans du code existant. | [vue d'ensemble](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-11.0) |
| Organisation des routes | `MapGroup`, un fichier d'endpoints par ressource. | [gestionnaires de routes](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-11.0) |
| Réponses | `TypedResults` et `Results<T1, T2>` — de préférence à `Results`. | [réponses](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-11.0) |
| Validation des entrées | `builder.Services.AddValidation()` avec les annotations de données. | [validation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-11.0) |
| Documentation de l'API | `AddOpenApi()` et `MapOpenApi()`, interface Scalar, en développement uniquement. Le document généré est en **OpenAPI 3.2** par défaut en .NET 11 (3.1 en .NET 10) : `/openapi/v1.json` annonce `"openapi": "3.2.0"` (rejoué). | [OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-11.0), [version du document](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-11.0), [tutoriel](https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-11.0) |
| Erreurs | `AddProblemDetails()`, `UseExceptionHandler()`, `UseStatusCodePages()`, et un `IExceptionHandler` pour les erreurs métier. | [erreurs d'API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-11.0) |
| JSON | `System.Text.Json`, réglé par `ConfigureHttpJsonOptions`. | [bibliothèques .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/libraries) |
| Date et heure courantes | `TimeProvider` injecté ; `FakeTimeProvider` dans les tests. | [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) |
| Autorisation | `AddAuthorizationBuilder().AddPolicy(...)` et `.RequireAuthorization()`. | [sécurité](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-11.0) |
| Journalisation | `ILogger` avec `[LoggerMessage]` sur des méthodes `partial`. | [génération de source](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation) |
| Mapping entité ↔ DTO | À la main. Aucune bibliothèque de mapping ni de médiation. | voir § 4 |

### Accès aux données

EF Core reste en version 10 (10.0.15) : les liens ci-dessous sont ceux d'EF Core 10, qui est la version utilisée.

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Enregistrement | `AddDbContext` (durée de vie *scoped*). | [configuration](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/) |
| Objets valeur | Types complexes (`ComplexProperty`), de préférence aux types possédés. | [EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) |
| Archivage logique (RG-05) | Filtres de requête nommés : `HasQueryFilter("NonArchives", ...)`. | [EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) |
| Mises à jour en masse | `ExecuteUpdateAsync`, `ExecuteDeleteAsync`. | [EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) |
| Migrations en local | `dotnet ef database update`. | [appliquer les migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) |
| Migrations en déploiement | Bundle (`dotnet ef migrations bundle`) ou script idempotent si le SQL doit être relu. | [appliquer les migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) |
| Contrôle en intégration continue | `dotnet ef migrations has-pending-model-changes`. | [appliquer les migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) |
| Données initiales | `UseSeeding` **et** `UseAsyncSeeding`. `HasData` seulement pour des données statiques. | [seeding](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding) |
| Traçabilité (RG-53) | `SaveChangesInterceptor`. | [intercepteurs](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors) |
| Concurrence optimiste | Jeton de concurrence et gestion de `DbUpdateConcurrencyException`. | [concurrence](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) |

### Tests

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Framework | **xUnit v3**. Le modèle s'installe à part : `dotnet new install xunit.v3.templates`, puis `dotnet new xunit3 -f net10.0`. La version 4.0.3 du modèle **ne propose pas `net11.0`** (`-f` accepte `net10.0`, `net9.0`, `net8.0` et les .NET Framework) ; le cours demande `net10.0` puis supprime la ligne `TargetFramework`, que `Directory.Build.props` fournit. Sans `-f`, le modèle cible `net8.0`, qui n'est plus pris en charge (constaté le 13 novembre 2026 avec le SDK 11.0.100). | [migration v3](https://xunit.net/docs/getting-started/v3/migration) |
| Lanceur de tests | **Microsoft.Testing.Platform**. C'est le choix par défaut du modèle `xunit3`, qui référence `xunit.v3.mtp-v2` et écrit `"test": { "runner": "Microsoft.Testing.Platform" }` dans `global.json` (constaté, y compris avec le SDK 11.0.100). | [dotnet test](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test) |
| Rapport de tests | `dotnet test --report-xunit-trx --results-directory TestResults` (exécuté, y compris en .NET 11 : 73 tests au module 04, rapport produit). | [xUnit et MTP](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) |
| Assertions | `Assert` de xUnit. Pas de bibliothèque d'assertions supplémentaire. | voir § 4 |
| Tests d'intégration | `WebApplicationFactory<Program>`. | [tests d'intégration](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-11.0) |
| Base de données en test | Le vrai SGBD, démarré par Testcontainers. | [stratégie de test](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy) |

### Exemple de référence — `Program.cs`

```csharp
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ChronosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Chronos")));
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

var collaborateurs = app.MapGroup("/api/collaborateurs");

collaborateurs.MapGet("/{id:int}",
    async Task<Results<Ok<CollaborateurDto>, NotFound>> (int id, ChronosDbContext db) =>
    {
        var collaborateur = await db.Collaborateurs
            .Where(c => c.Id == id)
            .Select(c => new CollaborateurDto(c.Id, c.Matricule, c.Nom, c.Prenom))
            .FirstOrDefaultAsync();

        return collaborateur is null ? TypedResults.NotFound() : TypedResults.Ok(collaborateur);
    });

app.Run();
```

Ce qu'on ne voit **pas** dans cet exemple est aussi important : pas de `Startup.cs`, pas de contrôleur, pas de Swashbuckle, pas d'AutoMapper, pas de `public partial class Program {}` en fin de fichier.

> Cet exemple est assemblé à partir des API documentées et n'a pas été compilé tel quel, ni en .NET 10 ni en .NET 11. Le code équivalent des modules 03, 06 et 08 l'a été ; en cas d'écart, ce sont les modules qui font foi (organisation en `MapGroup` par fichier, choix du fournisseur par `Chronos:Fournisseur`).

## 3. SQL Server et PostgreSQL

Le code métier reste indépendant du SGBD ; seuls la configuration, les migrations et quelques types diffèrent. Le cours traite les deux, avec un module consacré à leurs différences.

| Sujet | SQL Server 2025 | PostgreSQL 18 |
| --- | --- | --- |
| Configuration EF Core | `UseSqlServer(...)` | `UseNpgsql(...)` |
| Image de conteneur | `mcr.microsoft.com/mssql/server:2025-latest` | `postgres:18` |
| Variables obligatoires | `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD` | `POSTGRES_PASSWORD` |
| Hôte du conteneur | Linux x86-64 uniquement (pas d'ARM, pas d'émulation) | Toutes plateformes |
| Licence | Éditions Developer gratuites, **interdites en production** | Libre |
| `DateOnly` / `TimeOnly` | `date` / `time` | `date` / `time` |
| Horodatage | `datetime2` ou `datetimeoffset` — jamais `datetime` | `timestamptz`, avec un `DateTime` **UTC** obligatoirement |
| Jeton de concurrence | `byte[]` avec `IsRowVersion()` | `uint` mappé sur la colonne système `xmin` |
| Clés générées | `IDENTITY` | Colonnes *identity* (pas `serial`) |
| JSON | Type `json` natif, avec `UseCompatibilityLevel(170)` | `jsonb` |

Sources : [fournisseur SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/), [SQL Server en conteneur](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker), [éditions de SQL Server 2025](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025), [types date/heure SQL Server](https://learn.microsoft.com/en-us/sql/t-sql/data-types/datetime-transact-sql), [image postgres](https://hub.docker.com/_/postgres), [dates avec Npgsql](https://www.npgsql.org/doc/types/datetime.html), [concurrence avec Npgsql](https://www.npgsql.org/efcore/modeling/concurrency.html), [propriétés générées avec Npgsql](https://www.npgsql.org/efcore/modeling/generated-properties.html).

Ce tableau décrit EF Core 10 avec ses deux fournisseurs de version 10. En EF Core 11, `UseSqlServer` prend par défaut le niveau de compatibilité 160 ([nouveautés d'EF Core 11](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)) ; le cours reste en EF Core 10 et règle le niveau lui-même.

Trois pièges propres aux conteneurs :

- **PostgreSQL 18** : le volume se monte sur `/var/lib/postgresql`, et non plus sur `/var/lib/postgresql/data`.
- **SQL Server** : la variable `SA_PASSWORD` est dépréciée au profit de `MSSQL_SA_PASSWORD`.
- **Chaîne de connexion SQL Server** : `Encrypt=true` est le défaut. `TrustServerCertificate=true` se limite au poste de développement.

PostgreSQL 19 est attendu le 19 novembre 2026. Le cours reste sur la version 18 tant que le fournisseur Npgsql 10 n'annonce pas sa prise en charge.

## 4. Pièges : ce qu'on trouve encore partout et qu'on n'écrit plus

« Obsolète » signifie que le compilateur ou la documentation annonce le retrait. « Déconseillé » signifie que cela fonctionne toujours mais que la documentation oriente vers autre chose pour du code neuf. Les avertissements cités ont été **rejoués avec le SDK 11.0.100** quand la ligne le dit.

### Plateforme et API web

| Ce qu'on voit dans les tutoriels | Statut | À écrire à la place | Source |
| --- | --- | --- | --- |
| Cibler `net8.0` ou `net9.0` | **Hors support** depuis le 10 nov. 2026 | `net11.0` (`net10.0` reste pris en charge) | [politique de support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) |
| `Startup.cs`, `ConfigureServices`, `Configure` | Déconseillé | `Program.cs` avec `WebApplication.CreateBuilder` | [démarrage](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-11.0) |
| `WebHost.CreateDefaultBuilder`, `WebHostBuilder`, `IWebHost` | **Obsolète** depuis .NET 10 ; avertissements `ASPDEPR008` (`WebHost`, `IWebHost`) et `ASPDEPR004` (`WebHostBuilder`) toujours émis en .NET 11 (rejoué) | `WebApplication.CreateBuilder` | [rupture](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/webhostbuilder-deprecated) |
| `app.UseEndpoints(...)` | Déconseillé (analyseur ASP0014, avertissement rejoué en .NET 11) | `app.MapGet(...)` directement | [ASP0014](https://learn.microsoft.com/en-us/aspnet/core/diagnostics/asp0014) |
| `AddSwaggerGen()`, `UseSwagger()` | Retiré des modèles depuis .NET 9 | `AddOpenApi()`, `MapOpenApi()`, Scalar | [annonce](https://github.com/dotnet/aspnetcore/issues/54599) |
| `.WithOpenApi()` | **Obsolète** depuis .NET 10 ; en .NET 11, avertissement `ASPDEPR002` : « deprecated and will be removed in a future release » (rejoué) | Le supprimer | [rupture](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/withopenapi-deprecated) |
| `Results.Ok(...)` | Fonctionne, mais `TypedResults` est préféré | `TypedResults.Ok(...)` | [réponses](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-11.0) |
| Validation à la main ou par filtre d'endpoint | Remplacé | `AddValidation()` | [validation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-11.0) |
| `public partial class Program {}` pour les tests | Inutile depuis .NET 10 (un générateur de source s'en charge) ; toujours vrai en .NET 11 : la solution n'en contient pas et ses tests d'intégration passent | Rien | [notes de version de .NET 10](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0) |
| `Newtonsoft.Json` | N'est plus le défaut | `System.Text.Json` | [formatage](https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-11.0) |
| `DateTime.Now`, `DateTime.UtcNow` dans le code métier | Pas obsolète, mais difficile à tester | `TimeProvider` | [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) |
| `ISystemClock` | **Obsolète** depuis .NET 8 ; avertissement `CS0618` « Use TimeProvider instead » en .NET 11 (rejoué) | `TimeProvider` | [rupture](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/8/isystemclock-obsolete?view=aspnetcore-11.0) |
| Fabriquer soi-même des JWT à partir d'un identifiant et d'un mot de passe | **Explicitement déconseillé** | Cookies avec ASP.NET Core Identity, ou un fournisseur OpenID Connect | [JWT](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-11.0) |
| S'attendre à une redirection vers la page de connexion sur une API | Comportement changé en .NET 10 ; ce qu'il en est en .NET 11 est établi au module 12 | L'API renvoie 401 ou 403 | [ruptures .NET 10](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/overview) |
| `_logger.LogInformation($"...")` | Avertissement CA2254 | Gabarit de message, ou `[LoggerMessage]` | [CA2254](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2254) |
| `dotnet new angular`, `dotnet new react` | Abandonnés depuis le SDK 8 | Deux projets séparés : API et application Angular | [modèles](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates) |
| `BinaryFormatter` | **Retiré** depuis .NET 9 | `System.Text.Json` | [guide de migration](https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-migration-guide/) |

### Accès aux données

| Ce qu'on voit dans les tutoriels | Statut | À écrire à la place | Source |
| --- | --- | --- | --- |
| `System.Data.SqlClient` | **Déprécié** | `Microsoft.Data.SqlClient` | [NuGet](https://www.nuget.org/packages/System.Data.SqlClient) |
| `HasData` pour toutes les données initiales | Déconseillé | `UseSeeding` / `UseAsyncSeeding` | [seeding](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding) |
| `EnsureCreated()` suivi de `Migrate()` | À proscrire | `Migrate()` seul, ou un bundle | [appliquer les migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying) |
| Fournisseur `InMemory` pour les tests | Fortement déconseillé | Le vrai SGBD avec Testcontainers | [stratégie de test](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy) |
| Types possédés (`OwnsOne`) pour les objets valeur | Déconseillé | Types complexes (`ComplexProperty`) | [EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) |
| Type SQL Server `datetime` | Déconseillé | `datetime2`, `date`, `datetimeoffset` | [datetime](https://learn.microsoft.com/en-us/sql/t-sql/data-types/datetime-transact-sql) |
| `UseXminAsConcurrencyToken()` (Npgsql) | **Obsolète** | Propriété `uint` marquée `[Timestamp]` | [Npgsql 7.0](https://www.npgsql.org/efcore/release-notes/7.0.html) |
| Colonnes `serial` (PostgreSQL) | Déconseillé | Colonnes *identity* | [propriétés générées](https://www.npgsql.org/efcore/modeling/generated-properties.html) |
| Paquet `System.Linq.Async` | En conflit avec .NET 10 **et .NET 11** : `ToListAsync` devient ambigu (erreur `CS0121`, rejoué en .NET 11 avec `System.Linq.Async` 6.0.3) | Le retirer : `System.Linq.AsyncEnumerable` est intégré | [rupture](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/asyncenumerable) |

### Tests

| Ce qu'on voit dans les tutoriels | Statut | À écrire à la place | Source |
| --- | --- | --- | --- |
| Paquet `xunit` (v2), `dotnet new xunit` | En maintenance ; le modèle du SDK génère encore du v2 (rejoué avec le SDK 11.0.100 : `xunit` 2.9.3, `net11.0`) | `xunit.v3`, `dotnet new xunit3` | [versions](https://xunit.net/releases/) |
| `dotnet new xunit3` sans option | Cible `net8.0`, **hors support** | `dotnet new xunit3 -f net10.0`, puis supprimer la ligne `TargetFramework` (le modèle n'offre pas `net11.0`) | constaté le 13 nov. 2026 |
| `dotnet test --logger trx` | Option de VSTest, **refusée** par Microsoft.Testing.Platform (constaté le 12 nov. en .NET 10, rejoué le 13 nov. en .NET 11 : « Option '--logger' uses VSTest syntax ») | `dotnet test --report-xunit-trx` | constaté |
| `new PostgreSqlBuilder()` sans image | Refusé depuis Testcontainers 4.10 | `new PostgreSqlBuilder("postgres:18")` | [versions](https://github.com/testcontainers/testcontainers-dotnet/releases) |
| `coverlet.collector` | Incompatible avec Microsoft.Testing.Platform | `Microsoft.Testing.Extensions.CodeCoverage` | [couverture](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-code-coverage) |

### Bibliothèques devenues payantes

Plusieurs bibliothèques omniprésentes dans les tutoriels sont passées sous licence commerciale. Le cours ne les utilise pas : un mentoré doit pouvoir réutiliser ce qu'il apprend sans se poser de question de licence.

| Bibliothèque | Situation | Ce que fait le cours | Source |
| --- | --- | --- | --- |
| AutoMapper, MediatR | Commerciales depuis juillet 2025 | Mapping à la main ; appels de services directs | [annonce](https://www.jimmybogard.com/automapper-and-mediatr-commercial-editions-launch-today/) |
| FluentAssertions 8+ | Licence payante en usage commercial | `Assert` de xUnit | [dépôt](https://github.com/fluentassertions/fluentassertions) |
| MassTransit 9, Duende IdentityServer | Commerciales | Non utilisées | [MassTransit](https://www.nuget.org/packages/MassTransit), [Duende](https://duendesoftware.com/products/communityedition) |

### Ce qui n'est *pas* obsolète, contrairement à ce qu'on lit parfois

- **Les contrôleurs** restent pleinement pris en charge. Ils ne sont simplement plus le choix par défaut.
- **Le format `.sln`** fonctionne toujours ; `.slnx` est le nouveau défaut.
- **Swashbuckle** existe toujours. Il n'est plus fourni dans les modèles.
- **`dotnet add package`** fonctionne toujours (rejoué avec le SDK 11.0.100) ; `dotnet package add` est une forme supplémentaire.
- **`IMemoryCache` et `IDistributedCache`** ne sont pas dépréciés ; `HybridCache` les complète.

## 5. Choix structurants

Ces choix engagent le cours. Ils sont argumentés ici pour pouvoir être discutés ; tous ont été mis en œuvre et exécutés lors de la rédaction des modules, le 12 novembre 2026, en .NET 10. La solution a été revérifiée en .NET 11 le 13 novembre 2026 (voir les constats de cette date).

- **Authentification — mis en œuvre au module 12.** La documentation déconseille de fabriquer ses propres jetons et recommande les cookies pour une application dans le navigateur ([Identity pour SPA](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-11.0)). Le cours utilise ASP.NET Core Identity avec cookies (`AddIdentityApiEndpoints`, `MapIdentityApi` sous `/api/compte`). Constat de la construction en .NET 10 : `MapIdentityApi` 10.0.15 expose dix routes, sans déconnexion, sans passkeys et sans option pour en retirer ; le module 12 ajoute `logout` et `moi`, et neutralise les routes d'inscription. Ce qu'il en est de `MapIdentityApi` dans ASP.NET Core 11 est établi au module 12. L'alternative est un fournisseur OpenID Connect (Keycloak, Microsoft Entra ID) : plus proche d'un contexte d'entreprise, mais nettement plus lourd pour un junior.
- **Protection CSRF — mise en œuvre aux modules 12 et 19.** En .NET 10, `UseAntiforgery()` n'arrête pas un endpoint qui reçoit du JSON (constaté). En .NET 11, ASP.NET Core ajoute d'office un middleware de protection CSRF ([documentation](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-11.0), section « Automatic CSRF protection »). Il ne lit aucun jeton : il accepte `GET`/`HEAD`/`OPTIONS`/`TRACE`, `Sec-Fetch-Site` à `same-origin` ou `none`, une origine approuvée par une politique CORS avec identifiants, et refuse `cross-site` et `same-site` ; sans `Sec-Fetch-Site`, il compare `Origin` à l'hôte ; sans aucun des deux en-têtes, il accepte. Il ne répond jamais lui-même : il inscrit un verdict, que seuls les composants qui consomment un formulaire transforment en `400`. **Un endpoint JSON s'exécute donc malgré un verdict invalide.** Rejoué le 13 novembre 2026 sur Chronos (modules 12 et 19) : sans le `FiltreAntiforgery` du cours, une écriture JSON par cookie sans jeton passe (201), même en `cross-site` ; avec lui, elle est refusée (400). Le filtre reste la protection effective des écritures JSON ; la connexion d'Identity n'est pas affectée. Désactivation possible : `DisableCsrfProtection`, `.DisableAntiforgery()`, `[IgnoreAntiforgeryToken]`. Le module 12 pose un cookie `XSRF-TOKEN` et exige l'en-tête `X-XSRF-TOKEN` sur toute écriture : ce sont les noms par défaut d'Angular, qui les gère sans configuration.
- **Migrations avec deux fournisseurs — tranché au module 07.** Un seul `ChronosDbContext` ; un projet de migrations par fournisseur (`Chronos.Migrations.PostgreSql`, `Chronos.Migrations.SqlServer`) désigné par `MigrationsAssembly(...)` ; le fournisseur se choisit par la clé `Chronos:Fournisseur`, passée aux outils par `-- --Chronos:Fournisseur <F>` ([migrations avec plusieurs fournisseurs](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)). Appliqué sur PostgreSQL 18.6 et SQL Server 2025.
- **Concurrence optimiste — mise en œuvre au module 08.** Jeton géré par l'application (`Guid Version`, `IsConcurrencyToken()`), identique sur les deux SGBD ; les jetons natifs (`rowversion`, `xmin`) sont présentés au module 07 comme l'alternative propre à chaque fournisseur.
- **Environnement local.** Docker Compose pour démarrer les deux bases : simple, sans dépendance au SDK. [Aspire](https://aspire.dev/get-started/what-is-aspire/) orchestre l'API, la base et le front et apporte un tableau de bord d'observabilité ; il est présenté dans le dernier module plutôt que dès le départ, pour ne pas ajouter un outil de plus à l'entrée.
- **Lanceur de tests — tranché.** Le modèle `xunit3` configure Microsoft.Testing.Platform par défaut ; le cours suit ce défaut plutôt que de revenir à VSTest (`--test-runner vstest`). L'essai du 12 novembre 2026 a confirmé que `dotnet restore --locked-mode`, `dotnet build --no-restore` et `dotnet test --no-build --report-xunit-trx` s'enchaînent sans autre réglage avec le SDK 10.0.404 ; l'essai du 13 novembre 2026 l'a confirmé avec le SDK 11.0.100 (73 tests, rapport TRX produit).

## 6. Points à surveiller

Ces points n'ont pas pu être tranchés par une source officielle, ou les sources officielles se contredisent. Ils sont à revérifier avant d'écrire le module concerné.

- **La documentation Microsoft est elle-même en retard par endroits.** Des exemples de la documentation ASP.NET Core utilisent encore `WithOpenApi()`, xUnit v2 ou la validation par filtre. Un exemple officiel n'est donc pas une garantie : on vérifie l'API dans sa page de référence. Les pages d'EF Core 11 sont, elles aussi, en avance sur la publication : « What's new in EF Core 11 » (août 2026) parle encore d'une version « en développement ».
- **Base de données en test.** La documentation ASP.NET Core montre SQLite en mémoire ; celle d'EF Core recommande le vrai SGBD. Le cours suit EF Core.
- **Fin de support.** .NET 10 et .NET 11 : 14 novembre 2028 selon la politique de support. EF Core 10 : 10 novembre 2028 selon la documentation d'EF Core (relevé le 13 novembre 2026).
- **Passkeys — tranché pour .NET 10.** `MapIdentityApi` n'en expose aucune ; seul le modèle Blazor Web App les propose ([passkeys](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-11.0)). Pour ASP.NET Core 11, voir le module 12.
- **Convention de nommage PostgreSQL — clos.** `UseSnakeCaseNamingConvention` vient du paquet communautaire `EFCore.NamingConventions` ; le cours ne l'utilise pas (module 07).
- **`MSSQL_PID` — tranché par l'essai**, voir le [référentiel Docker](docker.md).
- **Quand le fournisseur PostgreSQL 11 sera stable.** Le jour où `Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.x stable paraît, tous les paquets EF Core du cours (`Microsoft.EntityFrameworkCore.*`, `Identity.EntityFrameworkCore`, `HealthChecks.EntityFrameworkCore`, `dotnet-ef`) pourront passer en 11.0.x ensemble : c'est une mise à jour à faire d'un bloc, et à rejouer (migrations comprises). Non fait, non essayé.

### Constats des essais du 12 novembre 2026

Relevés en exécutant le code des modules (SDK 10.0.404, ASP.NET Core et EF Core 10.0.15, Npgsql 10.0.3). Chacun est expliqué dans le module cité. La colonne « En .NET 11 » dit ce qu'il en est avec le SDK 11.0.100 : **rejoué** (le constat est confirmé ou corrigé par un essai du 13 novembre), **inchangé par construction** (il porte sur EF Core 10, Npgsql 10 ou xUnit, qui n'ont pas changé), ou **à revérifier, non rejoué en .NET 11** (le comportement vient d'ASP.NET Core ou du SDK et n'a pas été relevé à nouveau pour cette ligne : le module cité fait foi).

| Constat | Module | En .NET 11 |
| --- | --- | --- |
| `dotnet package add <paquet>` sans version installe la version **11.0.0** dans un projet `net10.0`, sans avertissement ; l'erreur n'apparaît qu'à la compilation. Vaut aussi pour `Microsoft.Extensions.TimeProvider.Testing` (prendre 10.11.0). | 03, 09 | **Rejoué : le piège se déplace.** Dans un projet `net11.0`, `Microsoft.AspNetCore.*` en 11.0.0 convient ; les paquets EF Core en 11.0.0 sont incompatibles avec le fournisseur PostgreSQL (`NU1608`). Voir les constats du 13 novembre. |
| `dotnet new webapi` : le cours passe `--no-https`. Sans cela, `UseHttpsRedirection` émet un avertissement à chaque lancement sur le profil `http`. HTTPS est terminé par le proxy frontal (module 23). | 03 | Rejoué : le modèle du SDK 11 garde `app.UseHttpsRedirection()` sans `--no-https`, et l'avertissement « Failed to determine the https port for redirect » apparaît dans le journal (à la première requête). |
| Scalar : paquet `Scalar.AspNetCore` (2.21.0 le jour de l'essai), interface sur `/scalar`. | 03 | Rejoué : `dotnet package add Scalar.AspNetCore` choisit 2.21.0 ; `/scalar` répond (redirection) en .NET 11. Page non ouverte dans un navigateur. |
| `dotnet new xunit3` dans une solution en gestion centralisée échoue en `NU1008` : le modèle écrit `Version="…"` dans le `.csproj`. Correction manuelle. | 04 | Rejoué : toujours le cas (`xunit.v3.templates` 4.0.3). |
| `global.json` : la [documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) conseille `rollForward: disable` quand on utilise des fichiers de verrouillage de paquets. Le cours garde `latestFeature` et en fait une question de séance. | 00, 05 | Relu le 13 novembre : la page conseille toujours `disable` avec les fichiers de verrouillage, et renvoie à dotnet/aspnetcore#65061 et dotnet/sdk#48795. Le cours garde `latestFeature`. |
| `dotnet new tool-manifest` crée `dotnet-tools.json` dans le dossier courant, et non dans `.config/`. `.config/dotnet-ef.json` et plusieurs options de `dotnet ef` documentées aujourd'hui sont réservées à EF Core 11. | 06 | Rejoué : `dotnet-tools.json` dans le dossier courant. `.config/dotnet-ef.json` reste propre à EF Core 11 (page « What's new in EF Core 11 ») : l'outil 11.0.0 le lit, le 10.0.15 de la solution l'ignore. `dotnet tool install dotnet-ef` sans version installe désormais la 11.0.0 ; rejoué le 13 novembre 2026 (module 06 et suivants), cette 11.0.0 **fonctionne** sur le projet resté en EF Core 10 (`migrations list`, `add`, `remove`, `script`, `database update`, `has-pending-model-changes`, bundle, sur PostgreSQL et SQL Server) : le conflit ne touche que les paquets. La solution fixe `10.0.15` pour que l'outil et les paquets aient la même version. |
| Npgsql 10.0.3 résout EF Core 10.0.4 si `Microsoft.EntityFrameworkCore.Relational` 10.0.15 n'est pas référencé explicitement. | 06 | Inchangé par construction : les messages `NU1608` du 13 novembre citent la même contrainte (`>= 10.0.4 && < 11.0.0`). |
| Filtre de requête nommé : le cours l'appelle `"NonArchives"`. Il fait disparaître les projets dont le responsable est archivé (jointure interne, avertissement 10622). | 06, 08 | Inchangé par construction (EF Core 10). |
| Un objet valeur sans *setter* (`Periode`) ne se mappe pas par `ComplexProperty(x => x.Periode)` seul : il faut déclarer ses propriétés dans le lambda. | 06, 09 | Inchangé par construction (EF Core 10). |
| Npgsql refuse un `DateTimeOffset` d'offset non nul ; PostgreSQL tronque les horodatages à la microseconde. PostgreSQL distingue la casse dans un index unique, SQL Server non. | 07 | Inchangé par construction (Npgsql 10, bases inchangées). |
| `AddValidation()` : les clés de `errors` sont les noms C# (`Matricule`, `Saisies[0].Duree`) et les messages sont en anglais. `[Required]` est ignoré sur un paramètre de requête `int?` absent ; un type valeur non nullable absent du corps prend sa valeur par défaut sans erreur ([dotnet/aspnetcore#67033](https://github.com/dotnet/aspnetcore/issues/67033), fermée, jalon 11.0-rc1) : d'où les propriétés nullables marquées `[Required]` dans les requêtes. | 08 | **Rejoué le 13 novembre 2026 (module 08)** : en .NET 11, `[Required]` sur un paramètre nullable absent donne `400` avec `errors` (limite de .NET 10 corrigée) ; `[Range]` seul laisse passer `null`. Nouveauté : `AsyncValidationAttribute` (`IsValidAsync`), appliquée avant le gestionnaire ([validation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-11.0)). Les propriétés nullables du cours restent valables. Les erreurs de liaison et de JSON illisible diffèrent selon l'environnement : en `Development`, `400` titré `Bad Request` sans `errors` (exception journalisée en `fail`) ; en `Production`, `400` titré `One or more validation errors occurred.` avec `errors` (clé `$.champ`, message de `System.Text.Json` avec nom de type .NET), rien en `fail`. |
| En environnement `Development`, un corps JSON illisible donne `500` avec `UseExceptionHandler()` seul (`400` en production). Corrigé par `StatusCodeSelector`. | 08 | Rejoué le 13 novembre 2026 (module 08) : en .NET 11, `UseExceptionHandler()` sans option conserve le code d'une `BadHttpRequestException` : un JSON illisible répond `400`, y compris en `Development` (500 en .NET 10). Le `StatusCodeSelector` de la solution fait double emploi, sans danger (144 tests verts sans lui). |
| Une date en paramètre d'URL se lit selon la culture invariante : `du=04/05/2026` est le 5 avril. Toujours le format ISO. | 09 | À revérifier, non rejoué en .NET 11 : voir le module 09. |
| xUnit v3 : `TheoryData<…>` et `IEnumerable<object[]>` sont [dits dépréciés](https://xunit.net/docs/getting-started/v3/theory-data-attributes) ; on écrit `IEnumerable<TheoryDataRow<…>>`. | 09 | Inchangé par construction (xUnit 4.0.3). |
| Transactions : avec `EnableRetryOnFailure()`, `BeginTransactionAsync` direct lève `InvalidOperationException` ; il faut passer par la stratégie d'exécution. | 10 | Inchangé par construction (EF Core 10). |
| `WebApplicationFactory<Program>` fonctionne sans `public partial class Program {}` (confirmé). `FakeTimeProvider` pilote aussi l'authentification : un cookie persistant daté dans le passé est rejeté par le client de test. | 11, 12 | Rejoué pour la première partie : les 144 tests d'intégration de la solution passent en .NET 11 sans `partial class Program`. Seconde partie : voir les modules 11 et 12. |
| Un `401` d'une API Identity porte `WWW-Authenticate: Bearer` et un `ProblemDetails`, sans redirection (confirmé). | 12 | Rejoué le 13 novembre 2026 (module 12) : inchangé en .NET 11. |
| `ForwardedHeadersOptions.KnownNetworks` est **obsolète** en .NET 10 (`ASPDEPR005`) alors que la page sur les proxys l'emploie encore : écrire `KnownIPNetworks` ([annonce](https://github.com/aspnet/Announcements/issues/523)). | 23 | Rejoué : l'avertissement `ASPDEPR005` est toujours émis en .NET 11 (« Please use KnownIPNetworks instead »). |
| Les migrations générées violent `IDE0161` et font échouer la compilation quand `EnforceCodeStyleInBuild` et `TreatWarningsAsErrors` sont actifs : les déclarer `generated_code = true`. | 22 | À revérifier, non rejoué en .NET 11 : voir le module 22. |
| `dotnet ef migrations bundle` et la publication de conteneur par le SDK réécrivent les `packages.lock.json` (ajout d'un identifiant de plateforme) ; `dotnet restore --force-evaluate` les rétablit. Un même bundle sert les deux SGBD : le fournisseur se choisit à l'exécution. | 23, 25 | À revérifier, non rejoué en .NET 11 pour la réécriture des fichiers de verrouillage : voir les modules 23 et 25. Rejoué le 13 novembre 2026 : sur une copie neuve jamais compilée, toute commande `dotnet ef` échoue sur `NETSDK1004` (fichier `project.assets.json` absent) ; c'est la cause de l'échec du bundle dans le workflow de publication. |

### Constats de la construction de la solution de référence (12 et 13 novembre 2026)

Relevés en enchaînant les 26 modules sur un seul code. Ils complètent le tableau précédent ; chacun est expliqué dans le module cité. Même lecture de la colonne « En .NET 11 ».

| Constat | Module | En .NET 11 |
| --- | --- | --- |
| Dans un record reçu par `[AsParameters]`, une annotation de validation écrite sur un paramètre du constructeur laisse la validation fonctionner mais fait échouer la génération du document OpenAPI (`500`). Écrire `[property: Range(…)]`. Un test doit lire `/openapi/v1.json`. | 08 | Rejoué le 13 novembre 2026 (module 08) : en .NET 11, une annotation portée par le paramètre de constructeur d'un record `[AsParameters]` valide correctement et le document OpenAPI se génère (200) ; le `property:` n'est plus indispensable, il reste valable. Le document généré est en OpenAPI 3.2. |
| La liaison d'un paramètre de requête de type énumération accepte les rangs (`?statut=1`), même quand le JSON les refuse. Une date en paramètre se lit en ISO strict par `DateOnly.TryParseExact`. | 08, 09 | À revérifier, non rejoué en .NET 11 : voir les modules 08 et 09. |
| Recherche insensible à la casse : seule `ToLower().Contains(…)` est traduite en SQL par les deux fournisseurs. `Contains(…, StringComparison)` et `ToLowerInvariant()` ne le sont pas ; `EF.Functions.Like` est sensible à la casse sur PostgreSQL et pas sur SQL Server. Les règles CA1304, CA1311 et CA1862 se neutralisent donc là, par `#pragma` commenté. | 07, 22 | Inchangé par construction (EF Core 10 et ses deux fournisseurs). |
| Sans l'argument `-- --Chronos:Fournisseur <F>`, `dotnet ef migrations has-pending-model-changes --project Chronos.Migrations.SqlServer` vérifie en réalité le jeu PostgreSQL et ne signale rien. | 07 | Inchangé par construction (`dotnet-ef` 10.0.15). |
| Le bloc de configuration d'`AddDbContext` ne s'exécute qu'à la première résolution du contexte : une valeur de configuration invalide doit être contrôlée avant, pour arrêter l'application au démarrage. | 07 | Inchangé par construction (EF Core 10). |
| `DbSet<T> X => Set<T>();` est l'écriture à retenir : avec les analyseurs actifs, `dotnet format` signale `CS8618` sur `{ get; set; }` (il ignore le suppresseur d'EF Core), et sa sous-commande `analyzers` réécrit les propriétés en `DbSet<T>?`, ce qui casse la compilation. | 22 | À revérifier, non rejoué en .NET 11 : voir le module 22 (`dotnet format` est celui du SDK 11.0.100). |
| `GenerateDocumentationFile` reste requis pour que `IDE0005` s'applique à la compilation (SDK 10.0.404) ; le fichier XML est publié avec l'application. | 22 | À revérifier, non rejoué en .NET 11 : voir le module 22. |
| Les migrations générées violent `IDE0161` et `CA1861` et portent un BOM : `generated_code = true` dans un `.editorconfig` du dossier `Migrations/` règle les trois, et doit précéder le premier `dotnet format`. | 22 | À revérifier, non rejoué en .NET 11 : voir le module 22. |
| `UseSeeding` ne s'exécute ni par un script SQL, ni dans une application qui ne migre pas au démarrage. Conditionné par `IsDevelopment()`, il s'exécute aussi depuis les outils d'EF Core et les bundles, qui démarrent l'application en `Development` par défaut. Les données de référence vont dans `HasData` ; les données de démonstration sous un réglage explicite. | 24, 25 | Inchangé par construction (EF Core 10, `dotnet-ef` 10.0.15). |
| Ajouter `HasData` à une table qui contient déjà ces lignes fait échouer la migration sur les bases existantes (clé déjà présente) : en développement, on recrée la base. | 25 | Inchangé par construction (EF Core 10). |
| Un bundle fabriqué pour un projet de migrations contient les deux jeux ; le fournisseur se choisit à l'exécution. Le dossier courant compte : lancé depuis le projet, il lit `appsettings.Development.json`. | 24, 25 | À revérifier, non rejoué en .NET 11 : voir les modules 24 et 25. |
| `MapHealthChecks` répond `401` dès qu'une politique d'autorisation de repli existe : `.AllowAnonymous()`. | 23 | À revérifier, non rejoué en .NET 11 : voir le module 23. |
| Clés de protection des données : un redémarrage du conteneur les conserve, une recréation (tout redéploiement) les perd et déconnecte tout le monde ; volume, et `SetApplicationName`. | 23 | À revérifier, non rejoué en .NET 11 : voir le module 23. |
| Identity ne revalide une session que toutes les 30 minutes par défaut (`SecurityStampValidatorOptions.ValidationInterval`) : un compte verrouillé ou rétrogradé garde sa session jusque-là. La solution retient une minute. | 25 | À revérifier, non rejoué en .NET 11 : voir le module 25. |
| `ILogger<T>` n'accepte pas une classe statique comme argument de type. Avec les analyseurs du module 22, un `logger.LogInformation("…")` direct ne compile pas (`CA1848`, `CA1873`, `CA2254`) : `[LoggerMessage]`. | 25 | À revérifier, non rejoué en .NET 11 : voir le module 25. |
| Deux écritures simultanées : sans transaction sérialisable, deux demandes concurrentes passent toutes les deux ; avec `EnableRetryOnFailure()`, la transaction s'écrit dans la stratégie d'exécution, et le traqueur de changements se vide entre deux tentatives. | 10 | Inchangé par construction (EF Core 10, bases inchangées). |

### Constats du 13 novembre 2026 (alignement sur .NET 11)

Relevés le jour de l'alignement, avec le SDK 11.0.100 (conteneur Linux), sauf mention contraire.

| Constat | Module |
| --- | --- |
| **Sortie de .NET 11.** Publié le 10 novembre 2026, STS, runtime 11.0.0, SDK 11.0.100 (C# 15), prise en charge jusqu'au 14 novembre 2028 ([téléchargements](https://dotnet.microsoft.com/en-us/download/dotnet/11.0), [politique de support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)). Les images Docker `11.0` sont sur Ubuntu 26.04 (`resolute`) ; la variante *chiseled* n'existe qu'en `resolute` ; l'image Ubuntu 24.04 des machines GitHub (20261111) contient le SDK 11.0.100 ([dotnet-docker](https://github.com/dotnet/dotnet-docker/blob/main/README.aspnet.md), [image](https://github.com/actions/runner-images/blob/main/images/ubuntu/Ubuntu2404-Readme.md)). | 00, 05, 23 |
| **Préversion du fournisseur PostgreSQL et `NU1608`.** `Npgsql.EntityFrameworkCore.PostgreSQL` 11.0.0-rc.3 (28 octobre 2026) est la seule version 11 ; elle exige exactement la préversion `11.0.0-rc.3` d'EF Core (cible `net11.0`). La version stable 10.0.3 exige EF Core `>= 10.0.4 && < 11.0.0` : avec EF Core 11.0.0, la restauration émet `NU1608: Detected package version outside of dependency constraint: Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 requires Microsoft.EntityFrameworkCore (>= 10.0.4 && < 11.0.0) but version Microsoft.EntityFrameworkCore 11.0.0 was resolved.` Rejoué : à l'ajout, `warn : NU1608` et `error: NU1107` (la version est écrite quand même) ; à chaque compilation ensuite, 4 avertissements `NU1608` sur une solution à un projet. C'est un avertissement : il devient une erreur (`error NU1608: Warning As Error`) dès que `TreatWarningsAsErrors` est actif, ce qui sera le cas de la solution au module 22 (rejoué sur un projet d'essai). | 03, 06 |
| **`dotnet package add` sans numéro de version, sous SDK 11.** `Microsoft.AspNetCore.OpenApi` : 11.0.0 (convient, et c'est la version écrite par le modèle `webapi`). `Microsoft.AspNetCore.Mvc.Testing` : 11.0.0 (convient). `Scalar.AspNetCore` : 2.21.0. `Microsoft.EntityFrameworkCore.Relational`, `.Design`, `.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` : 11.0.0, d'où `NU1608` avec le fournisseur PostgreSQL (et, selon les paquets présents, `NU1107` à l'ajout ou `NU1605` à la compilation). `Microsoft.Extensions.TimeProvider.Testing` : 11.0.0 aussi (le cours épingle 10.11.0, voir le module 09). `dotnet tool install dotnet-ef` : 11.0.0 (le cours épingle 10.0.15). Si l'on a laissé entrer une 11.0.0 puis demandé `--version 10.0.15` pour un autre paquet EF Core, la restauration échoue en `NU1605` (rétrogradation de `Microsoft.EntityFrameworkCore.Relational` de 11.0.0 à 10.0.15) : on corrige la version dans `Directory.Packages.props`. Dans tous les cas : **`--version 10.0.15` pour tout ce qui touche à EF Core**. | 03, 06, 09, 12, 23 |
| **Défaut du SDK 11.0.100 et son contournement.** En **Debug**, un projet du SDK Web qui en référence un autre échoue à la compilation avec « scoped css bundles do not have matching endpoints » ([dotnet/sdk#56656](https://github.com/dotnet/sdk/issues/56656), ouvert à la date de relevé, présent depuis `11.0.100-rc.2`). C'est le cas de `Chronos.Api.Tests` (module 11). Contournement retenu : `<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>` dans `Chronos.Api.Tests.csproj`, avec un commentaire qui renvoie au ticket ; en Release, l'erreur n'apparaît pas. Autres contournements cités par le ticket : `EnableHotReloadInRuntimeConfigDevFile=false` dans le projet référencé, ou un `global.json` sur le SDK 10.0.404. Reproduit par le coordinateur ; non rejoué ici. | 11 |
| **Étape de restauration manquante avant le bundle.** Dans le workflow de publication, `dotnet ef migrations bundle` sur une machine neuve échoue (`NETSDK1004`, fichier `project.assets.json` absent) : le job `migrations` a désormais une étape `dotnet restore --locked-mode` avant. Constaté le 13 novembre 2026 à la première exécution du workflow de publication sur GitHub (job du bundle en échec) ; la version corrigée a réussi le même jour sur `main`, deux fois. | 24 |
| **Le modèle `xunit3` n'offre pas `net11.0`.** `dotnet new xunit3 -f net11.0` échoue : « 'net11.0' is not a valid value for -f », les valeurs possibles étant `net10.0`, `net472`, `net48`, `net481`, `net8.0`, `net9.0` (`xunit.v3.templates` 4.0.3). Avec `-f net10.0`, le `.csproj` généré est celui que le cours attend une fois la ligne `TargetFramework` supprimée ; sans `-f`, le modèle ajoute en plus `TestingPlatformDotnetTestSupport` et cible `net8.0`. | 04 |
| **Le document OpenAPI passe à la version 3.2.** Par défaut, `/openapi/v1.json` annonce `"openapi": "3.2.0"` en .NET 11 (3.1 en .NET 10) ; on peut choisir une version antérieure par `OpenApiVersion` ([documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-11.0)). | 03, 08 |
| **`global.json` et version incomplète.** Avec le SDK 11.0.100, `"version": "10.0"` ou `"11.0"` n'est pas refusé : `dotnet --version` n'affiche aucune erreur et répond `11.0.100`, la valeur étant ignorée (le réglage ne protège donc de rien) ; seul `dotnet --info` signale `Version '11.0' is not valid for the 'sdk/version' value`. Un `global.json` qui demande une version absente (`11.0.500` avec `latestFeature`, ou `10.0.400`) échoue avec `A compatible .NET SDK was not found.`, en nommant la version demandée et le fichier. Avec `rollForward` omis (politique `patch`), `11.0.50` ne se contente pas du `11.0.100` installé : la bande de fonctionnalités compte. | 00, 03 |
| **Winget et Visual Studio.** Le paquet WinGet du SDK est `Microsoft.DotNet.SDK.11` (version 11.0.100 dans le dépôt `winget-pkgs`) ; la page d'installation de Microsoft ne cite encore que les paquets `.10`. La page des versions du SDK associe SDK 11.0.1xx et Visual Studio 18.12 ; Rider 2026.3 annonce la prise en charge de .NET 11 et C# 15 ([versions du SDK](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs)). Installation non exécutée sous Windows. | 00 |
