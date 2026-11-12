# Référentiel .NET 10, EF Core 10 et bases de données — pratiques à jour

> **Vérifié le 12 novembre 2026** sur learn.microsoft.com, dotnet.microsoft.com, npgsql.org, postgresql.org, xunit.net, aspire.dev et nuget.org.
> Ce document fait foi pour le cours : en cas de doute entre un tutoriel trouvé en ligne et ce référentiel, c'est le référentiel — ou mieux, la documentation officielle du jour — qui a raison.
> À revérifier avant d'écrire chaque module, et à chaque correctif mensuel de .NET.

## 1. Versions

| Élément | Valeur | Source |
| --- | --- | --- |
| .NET 10 (LTS) | 10.0.15, SDK 10.0.404 | [téléchargements](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) |
| Fin de support de .NET 10 | 14 novembre 2028 | [politique de support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) |
| Langage | C# 14 | [nouveautés](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14) |
| EF Core | 10.0.15 | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore) |
| Fournisseur SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15 | [NuGet](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/10.0.15) |
| Fournisseur PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 | [notes de version](https://www.npgsql.org/efcore/release-notes/10.0.html) |
| SQL Server | 2025 (17.x) | [nouveautés](https://learn.microsoft.com/en-us/sql/sql-server/what-s-new-in-sql-server-2025) |
| PostgreSQL | 18 (18.6) | [versions](https://www.postgresql.org/support/versioning/) |
| xUnit | v3 — `xunit.v3` 4.0.3 | [versions](https://xunit.net/releases/) |
| Testcontainers pour .NET | 4.16.0 | [NuGet](https://www.nuget.org/packages/Testcontainers) |
| Aspire | 13.6.5 | [NuGet](https://www.nuget.org/packages/Aspire.Hosting.AppHost) |

### .NET 10 ou .NET 11 ?

**.NET 11 est sorti le 10 novembre 2026** ([annonce](https://devblogs.microsoft.com/dotnet/announcing-dotnet-11/)). C'est une version STS ; .NET 10 est la LTS. Les deux sont prises en charge jusqu'au 14 novembre 2028. .NET 8 et .NET 9 ne sont plus pris en charge depuis le 10 novembre 2026.

Le cours cible **.NET 10**. Trois conséquences pratiques :

- Le SDK installé par défaut aujourd'hui est le 11. Il faut installer le **SDK 10** et le fixer par un fichier `global.json` à la racine de la solution ([global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)).
- Les paquets NuGet se choisissent en version **10.x**. Les versions 11.x d'EF Core et de `Microsoft.AspNetCore.*` exigent .NET 11 : un `dotnet add package` sans numéro de version risque d'aller les chercher.
- La documentation Microsoft se lit avec le sélecteur de version positionné sur **ASP.NET Core 10** (`?view=aspnetcore-10.0` dans l'adresse).

## 2. Ce que le cours enseigne

### Projet et outillage

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Solution | Format `.slnx`, créé par défaut par `dotnet new sln`. | [slnx](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default) |
| Projet d'API | `dotnet new webapi` : Minimal APIs et OpenAPI intégré, sans Swashbuckle. | [modèles](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates) |
| Démarrage | `WebApplication.CreateBuilder(args)` dans `Program.cs`. Pas de classe `Startup`. | [démarrage](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-10.0) |
| Versions des paquets | Gestion centralisée dans `Directory.Packages.props`. | [gestion centralisée](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) |
| Version du SDK | `global.json`. | [global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) |
| Essais rapides | Application en un seul fichier : `dotnet run essai.cs`. | [apps fichier unique](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps) |

### API web

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Style d'API | **Minimal APIs**, recommandées pour tout nouveau projet. Les contrôleurs sont présentés comme l'alternative que le mentoré croisera dans du code existant. | [vue d'ensemble](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-10.0) |
| Organisation des routes | `MapGroup`, un fichier d'endpoints par ressource. | [gestionnaires de routes](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-10.0) |
| Réponses | `TypedResults` et `Results<T1, T2>` — de préférence à `Results`. | [réponses](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0) |
| Validation des entrées | `builder.Services.AddValidation()` avec les annotations de données. | [validation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-10.0) |
| Documentation de l'API | `AddOpenApi()` et `MapOpenApi()` (OpenAPI 3.1), interface Scalar, en développement uniquement. | [OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0), [tutoriel](https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0) |
| Erreurs | `AddProblemDetails()`, `UseExceptionHandler()`, `UseStatusCodePages()`, et un `IExceptionHandler` pour les erreurs métier. | [erreurs d'API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0) |
| JSON | `System.Text.Json`, réglé par `ConfigureHttpJsonOptions`. | [bibliothèques .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/libraries) |
| Date et heure courantes | `TimeProvider` injecté ; `FakeTimeProvider` dans les tests. | [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) |
| Autorisation | `AddAuthorizationBuilder().AddPolicy(...)` et `.RequireAuthorization()`. | [sécurité](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-10.0) |
| Journalisation | `ILogger` avec `[LoggerMessage]` sur des méthodes `partial`. | [génération de source](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation) |
| Mapping entité ↔ DTO | À la main. Aucune bibliothèque de mapping ni de médiation. | voir § 4 |

### Accès aux données

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| Enregistrement | `AddDbContext` (durée de vie *scoped*). | [configuration](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/) |
| Objets valeur | Types complexes (`ComplexProperty`), de préférence aux types possédés. | [EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) |
| Archivage logique (RG-05) | Filtres de requête nommés : `HasQueryFilter("Actifs", ...)`. | [EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) |
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
| Framework | **xUnit v3**. Le modèle s'installe à part : `dotnet new install xunit.v3.templates`, puis `dotnet new xunit3`. | [migration v3](https://xunit.net/docs/getting-started/v3/migration) |
| Assertions | `Assert` de xUnit. Pas de bibliothèque d'assertions supplémentaire. | voir § 4 |
| Tests d'intégration | `WebApplicationFactory<Program>`. | [tests d'intégration](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) |
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

> Cet exemple est assemblé à partir des API documentées. Il sera compilé et testé dans la solution de référence au moment d'écrire le module correspondant.

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

Trois pièges propres aux conteneurs :

- **PostgreSQL 18** : le volume se monte sur `/var/lib/postgresql`, et non plus sur `/var/lib/postgresql/data`.
- **SQL Server** : la variable `SA_PASSWORD` est dépréciée au profit de `MSSQL_SA_PASSWORD`.
- **Chaîne de connexion SQL Server** : `Encrypt=true` est le défaut. `TrustServerCertificate=true` se limite au poste de développement.

PostgreSQL 19 est attendu le 19 novembre 2026. Le cours reste sur la version 18 tant que le fournisseur Npgsql 10 n'annonce pas sa prise en charge.

## 4. Pièges : ce qu'on trouve encore partout et qu'on n'écrit plus

« Obsolète » signifie que le compilateur ou la documentation annonce le retrait. « Déconseillé » signifie que cela fonctionne toujours mais que la documentation oriente vers autre chose pour du code neuf.

### Plateforme et API web

| Ce qu'on voit dans les tutoriels | Statut | À écrire à la place | Source |
| --- | --- | --- | --- |
| Cibler `net8.0` ou `net9.0` | **Hors support** depuis le 10 nov. 2026 | `net10.0` | [politique de support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) |
| `Startup.cs`, `ConfigureServices`, `Configure` | Déconseillé | `Program.cs` avec `WebApplication.CreateBuilder` | [démarrage](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup?view=aspnetcore-10.0) |
| `WebHost.CreateDefaultBuilder`, `WebHostBuilder`, `IWebHost` | **Obsolète** en .NET 10 | `WebApplication.CreateBuilder` | [rupture](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/webhostbuilder-deprecated) |
| `app.UseEndpoints(...)` | Déconseillé (analyseur ASP0014) | `app.MapGet(...)` directement | [ASP0014](https://learn.microsoft.com/en-us/aspnet/core/diagnostics/asp0014) |
| `AddSwaggerGen()`, `UseSwagger()` | Retiré des modèles depuis .NET 9 | `AddOpenApi()`, `MapOpenApi()`, Scalar | [annonce](https://github.com/dotnet/aspnetcore/issues/54599) |
| `.WithOpenApi()` | **Obsolète** en .NET 10 | Le supprimer | [rupture](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/withopenapi-deprecated) |
| `Results.Ok(...)` | Fonctionne, mais `TypedResults` est préféré | `TypedResults.Ok(...)` | [réponses](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0) |
| Validation à la main ou par filtre d'endpoint | Remplacé | `AddValidation()` | [validation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-10.0) |
| `public partial class Program {}` pour les tests | Inutile en .NET 10 | Rien : un générateur de source s'en charge | [notes de version](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0) |
| `Newtonsoft.Json` | N'est plus le défaut | `System.Text.Json` | [formatage](https://learn.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting?view=aspnetcore-10.0) |
| `DateTime.Now`, `DateTime.UtcNow` dans le code métier | Pas obsolète, mais difficile à tester | `TimeProvider` | [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview) |
| `ISystemClock` | **Obsolète** depuis .NET 8 | `TimeProvider` | [rupture](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/8/isystemclock-obsolete?view=aspnetcore-10.0) |
| Fabriquer soi-même des JWT à partir d'un identifiant et d'un mot de passe | **Explicitement déconseillé** | Cookies avec ASP.NET Core Identity, ou un fournisseur OpenID Connect | [JWT](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) |
| S'attendre à une redirection vers la page de connexion sur une API | Comportement changé en .NET 10 | L'API renvoie 401 ou 403 | [ruptures .NET 10](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/overview) |
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
| Paquet `System.Linq.Async` | En conflit avec .NET 10 | Le retirer : `System.Linq.AsyncEnumerable` est intégré | [rupture](https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/asyncenumerable) |

### Tests

| Ce qu'on voit dans les tutoriels | Statut | À écrire à la place | Source |
| --- | --- | --- | --- |
| Paquet `xunit` (v2), `dotnet new xunit` | En maintenance ; le modèle du SDK génère encore du v2 | `xunit.v3`, `dotnet new xunit3` | [versions](https://xunit.net/releases/) |
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
- **`dotnet add package`** fonctionne toujours ; `dotnet package add` est une forme supplémentaire.
- **`IMemoryCache` et `IDistributedCache`** ne sont pas dépréciés ; `HybridCache` les complète.

## 5. Choix structurants à valider

Ces choix engagent le cours. Ils sont argumentés ici pour pouvoir être discutés avant d'écrire les modules concernés.

- **Authentification.** La documentation déconseille de fabriquer ses propres jetons et recommande les cookies pour une application dans le navigateur ([Identity pour SPA](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)). Le cours partirait donc sur ASP.NET Core Identity avec cookies (`AddIdentityApiEndpoints`, `MapIdentityApi`). L'alternative est un fournisseur OpenID Connect (Keycloak, Microsoft Entra ID) : plus proche d'un contexte d'entreprise, mais nettement plus lourd pour un junior.
- **Environnement local.** Docker Compose pour démarrer les deux bases : simple, sans dépendance au SDK. [Aspire](https://aspire.dev/get-started/what-is-aspire/) orchestre l'API, la base et le front et apporte un tableau de bord d'observabilité ; il est présenté dans le dernier module plutôt que dès le départ, pour ne pas ajouter un outil de plus à l'entrée.
- **Lanceur de tests.** `dotnet test` utilise VSTest par défaut ; le mode Microsoft.Testing.Platform s'active dans `global.json` ([dotnet test](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)). À trancher par un essai avec xUnit v3 lors du module sur les tests.

## 6. Points à surveiller

Ces points n'ont pas pu être tranchés par une source officielle, ou les sources officielles se contredisent. Ils sont à revérifier avant d'écrire le module concerné.

- **La documentation Microsoft est elle-même en retard par endroits.** Des exemples de la documentation ASP.NET Core 10 utilisent encore `WithOpenApi()`, xUnit v2 ou la validation par filtre. Un exemple officiel n'est donc pas une garantie : on vérifie l'API dans sa page de référence.
- **Migrations avec deux fournisseurs.** L'organisation des migrations quand un même modèle cible SQL Server et PostgreSQL (un jeu de migrations par fournisseur) n'a pas été vérifiée dans cette passe. À faire avant le module sur les deux SGBD.
- **Base de données en test.** La documentation ASP.NET Core montre SQLite en mémoire ; celle d'EF Core recommande le vrai SGBD. Le cours suit EF Core.
- **Fin de support de .NET 10.** 14 novembre 2028 selon la politique de support ; 10 novembre 2028 selon la documentation d'EF Core.
- **Validation.** En .NET 10, `AddValidation()` ne valide pas les paramètres de type valeur *nullable* et n'offre pas de validation asynchrone (arrivée en .NET 11).
- **Passkeys.** La prise en charge par `MapIdentityApi` n'a pas été confirmée.
- **Convention de nommage PostgreSQL.** `UseSnakeCaseNamingConvention` n'apparaît pas dans la documentation officielle de Npgsql ; il vient du paquet tiers `EFCore.NamingConventions`.
- **SQL Server en conteneur.** La valeur de `MSSQL_PID` pour l'édition Developer n'est pas cohérente dans la documentation (`Developer`, `StandardDeveloper` ou `EnterpriseDeveloper`).
