# Module 07 — Deux SGBD, un seul code

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 10](../docs/references/dotnet-10.md) et le [référentiel Docker](../docs/references/docker.md).
> L'organisation décrite ici a été **exécutée le 12 novembre 2026** dans le conteneur `mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.404, EF Core 10.0.15, Npgsql 10.0.3, `Microsoft.EntityFrameworkCore.SqlServer` 10.0.15) contre PostgreSQL 18.6 et SQL Server 2025 (17.0.5045.3) : deux jeux de migrations créés et appliqués, la même API démarrée sur l'une puis l'autre base. Les extraits SQL, les messages d'erreur et les tableaux de comportement sont ceux réellement obtenus ; les types de date et les jetons de concurrence l'ont été sur une entité d'essai. **Non exécutés** : la saisie des commandes dans PowerShell sous Windows (elles l'ont été dans un terminal Linux, et les variables d'environnement de l'étape 8 y ont été posées autrement), le projet Compose du dépôt (les bases d'essai étaient des conteneurs isolés, de mêmes images) et l'exercice sur l'intégration continue. Signalez au mentor tout écart constaté.

Chronos doit pouvoir s'installer chez un client équipé de SQL Server comme chez un client équipé de PostgreSQL. Depuis le module 06, l'application tourne sur PostgreSQL. Que faut-il changer pour qu'elle tourne aussi sur SQL Server ? Presque rien dans le code — et c'est justement ce « presque » qu'il faut connaître : les migrations, les dates, la casse des textes, la concurrence.

## Objectifs

À la fin de ce module, vous saurez :

- faire choisir le SGBD par la configuration, sans recompiler ;
- organiser un jeu de migrations par fournisseur, et le tenir à jour ;
- écrire une chaîne de connexion SQL Server correcte pour le développement, et dire ce qui change en production ;
- expliquer ce qui diffère entre les deux SGBD pour les dates, les clés générées, la casse et les jetons de concurrence ;
- comparer le SQL produit par les deux fournisseurs pour une même requête LINQ.

## Prérequis

- [Module 06](06-persister-avec-ef-core.md) : `Chronos.Infrastructure`, `ChronosDbContext`, les migrations PostgreSQL, `/api/projets` servi depuis la base, et l'exercice 1 terminé (configuration de `Collaborateur` et de `Contrat`).
- [Module 01](01-docker-les-bases.md) : le service `sqlserver` de `compose.yaml`. SQL Server réclame au moins 2 Go de mémoire.

Les commandes `dotnet` se lancent depuis `src/api/`, les commandes `docker compose` depuis la racine du dépôt.

## Règles de gestion

Aucune règle nouvelle. Mais **RG-01** et **RG-10** (unicité du matricule, de l'adresse électronique, du code de projet) ne se comportent pas de la même façon sur les deux SGBD tant qu'on n'a pas décidé ce que « le même code » veut dire : `ACME-MIGR` et `acme-migr` sont-ils deux projets ? L'étape 9 le montre, l'exercice 1 tranche.

## Notions

### Ce qui est commun, ce qui ne l'est pas

EF Core sépare deux choses. Le **modèle** (vos entités, leurs configurations) et vos **requêtes LINQ** ne dépendent pas du SGBD. Le **fournisseur** — `Npgsql.EntityFrameworkCore.PostgreSQL` ou `Microsoft.EntityFrameworkCore.SqlServer` — traduit le tout dans le dialecte de sa base : types de colonnes, syntaxe SQL, fonctions.

Trois choses restent propres à chaque SGBD :

- la **chaîne de connexion** ;
- les **migrations** : vous l'avez vu au module 06, elles contiennent des types comme `character varying(20)` ;
- quelques **comportements** de la base elle-même, que le fournisseur ne gomme pas : la casse, la précision des dates.

Documentation : [Fournisseur SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/), [Fournisseur Npgsql](https://www.npgsql.org/efcore/index.html)

### Un jeu de migrations par fournisseur

L'outil `dotnet ef` ne génère une migration que pour le fournisseur actif. La documentation est claire sur la conséquence : avec deux fournisseurs, on tient **deux jeux de migrations**, et chaque changement du modèle ajoute une migration à chacun.

Elle décrit deux organisations :

| Organisation | Principe | Pour Chronos |
| --- | --- | --- |
| Un type de contexte par fournisseur | `SqlServerChronosDbContext` hérite de `ChronosDbContext` ; les deux jeux vivent dans le même projet. | Écartée : les endpoints devraient savoir quel contexte demander. |
| **Un seul type de contexte** | Chaque jeu de migrations vit dans **son propre projet** ; le contexte reçoit le nom de l'assembly à utiliser (`MigrationsAssembly`). | **Retenue.** |

Chronos aura donc deux projets, `Chronos.Migrations.PostgreSql` et `Chronos.Migrations.SqlServer`. Ils ne contiennent que des migrations. `Chronos.Api` reste le projet de démarrage des outils : c'est sa configuration, la vraie, qui décide du fournisseur, et l'outil lui transmet ce choix par un argument placé après `--`.

Documentation : [Migrations avec plusieurs fournisseurs](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers), [Un projet de migrations séparé](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects), [exemple officiel](https://github.com/dotnet/EntityFramework.Docs/tree/main/samples/core/Schemas/TwoProjectMigrations)

### Les types : ce que chaque fournisseur choisit

Relevé sur les migrations des deux jeux :

| C# | PostgreSQL 18 | SQL Server 2025 |
| --- | --- | --- |
| `int` (clé) | `integer GENERATED BY DEFAULT AS IDENTITY` | `int IDENTITY` |
| `string`, 20 caractères au plus | `character varying(20)` | `nvarchar(20)` |
| `bool` | `boolean` | `bit` |
| `DateOnly` | `date` | `date` |
| `decimal`, précision (3,2) | `numeric(3,2)` | `decimal(3,2)` |
| `decimal` sans précision | `numeric` | `decimal(18,2)`, avec un avertissement |
| `Guid` | `uuid` | `uniqueidentifier` |
| `DateTimeOffset` | `timestamp with time zone` | `datetimeoffset` |
| `DateTime` | `timestamp with time zone` | `datetime2` |

Les **clés** sont générées par la base dans les deux cas ; on ne renseigne jamais `Id` soi-même. Les tutoriels PostgreSQL montrent encore `serial` : c'est l'ancien mécanisme, que Npgsql ne recommande que pour d'anciennes versions.

Documentation : [Génération de valeurs avec Npgsql](https://www.npgsql.org/efcore/modeling/generated-properties.html), [Génération de valeurs avec SQL Server](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/value-generation)

### Les dates

Le cahier des charges distingue deux sortes de dates.

Les **dates métier** — début d'un projet, jour d'une saisie — sont des `DateOnly`. Les deux SGBD ont un type `date`, sans heure ni fuseau : aucune différence.

Les **horodatages techniques** — la traçabilité du module 12 — seront des `DateTimeOffset` en UTC. Là, les deux bases divergent :

| | PostgreSQL (`timestamp with time zone`) | SQL Server (`datetimeoffset`) |
| --- | --- | --- |
| Ce qui est stocké | Un instant UTC. Le décalage d'origine n'est **pas** conservé, malgré le nom du type. | L'instant **et** son décalage. |
| Écrire `10:00 +02:00` | Refusé par Npgsql : `only offset 0 (UTC) is supported`. | Accepté, relu `10:00 +02:00`. |
| Écrire un `DateTime` local | Refusé : `only UTC is supported`. | Accepté dans un `datetime2`, relu sans indication de fuseau. |
| Précision | La microseconde (6 décimales). | 100 nanosecondes (7 décimales). |

La règle qui rend le code portable tient en une ligne : **un horodatage est toujours un `DateTimeOffset` en UTC**, obtenu par `TimeProvider.GetUtcNow()` (module 09). Dans ce cas, les deux bases se comportent de la même façon — à la septième décimale près, que PostgreSQL tronque : un horodatage relu n'est pas forcément égal, au tic près, à celui qu'on a écrit. On s'en souviendra dans les tests du module 11.

Documentation : [Dates et heures avec Npgsql](https://www.npgsql.org/doc/types/datetime.html), [types date et heure de PostgreSQL](https://www.postgresql.org/docs/18/datatype-datetime.html), [`datetimeoffset`](https://learn.microsoft.com/en-us/sql/t-sql/data-types/datetimeoffset-transact-sql)

### La casse et les collations

Une **collation** est la règle qu'une base applique pour comparer et trier du texte. C'est elle qui décide si `Martin` égale `martin`.

- SQL Server, par défaut, est **insensible** à la casse (collation `SQL_Latin1_General_CP1_CI_AS` dans le conteneur ; `CI` pour *case insensitive*).
- PostgreSQL, par défaut, est **sensible** à la casse.

La différence touche tout ce qui compare du texte : `WHERE`, `LIKE`, et les **index uniques**. Une même requête LINQ donne donc des résultats différents selon la base. EF Core ne corrige pas cela, et refuse volontairement de traduire `string.Equals(..., StringComparison.OrdinalIgnoreCase)`.

Chaque SGBD a ses outils — `ILIKE` ou une collation non déterministe pour PostgreSQL, `COLLATE` pour SQL Server — mais aucun n'est commun aux deux. Chronos prend le parti le plus simple : **le code décide**, pas la base. Les valeurs qui doivent être uniques sans tenir compte de la casse sont normalisées par le domaine avant d'être enregistrées, et une recherche met les deux côtés en minuscules.

Documentation : [Collations et sensibilité à la casse (EF Core)](https://learn.microsoft.com/en-us/ef/core/miscellaneous/collations-and-case-sensitivity), [avec Npgsql](https://www.npgsql.org/efcore/misc/collations-and-case-sensitivity.html)

### Les jetons de concurrence natifs

Le [module 08](08-une-api-robuste.md) mettra en place la **concurrence optimiste** : refuser une modification quand la ligne a changé depuis qu'on l'a lue. Le principe est un **jeton**, une valeur qui change à chaque écriture et que l'`UPDATE` vérifie dans son `WHERE`. Chaque SGBD en propose un, natif :

| | SQL Server | PostgreSQL |
| --- | --- | --- |
| Mécanisme | Une colonne de type `rowversion`, que la base renouvelle à chaque écriture. | `xmin`, une colonne système présente dans toute table : l'identifiant de la transaction qui a écrit la ligne. Aucune colonne à créer. |
| Propriété C# | `byte[]` | `uint` |
| Configuration | `IsRowVersion()` | `IsRowVersion()` |
| SQL produit à l'`UPDATE` | `... OUTPUT INSERTED.[Version] WHERE [Id] = @p1 AND [Version] = @p2` | `... WHERE "Id" = @p1 AND xmin = @p2 RETURNING xmin` |

La configuration est la même, mais **le type de la propriété ne l'est pas** : avec les jetons natifs, l'entité elle-même dépendrait du SGBD. Le cours retient donc l'autre voie décrite par la documentation, le jeton **géré par l'application** : une propriété `Guid Version`, marquée `IsConcurrencyToken()`, à laquelle le code donne une nouvelle valeur à chaque modification. Même entité, même colonne (`uuid` ou `uniqueidentifier`), même JSON renvoyé au client. Le prix : c'est à nous de ne pas oublier de la renouveler.

Documentation : [Gérer les conflits de concurrence](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), [jetons de concurrence avec Npgsql](https://www.npgsql.org/efcore/modeling/concurrency.html), [`rowversion`](https://learn.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql), [colonnes système de PostgreSQL](https://www.postgresql.org/docs/18/ddl-system-columns.html)

## Pas à pas

### 1. Démarrer SQL Server et connaître son édition

```powershell
docker compose up -d --wait sqlserver
```

```powershell
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -Q "SELECT SERVERPROPERTY('Edition')"
```

Après saisie du mot de passe, la réponse est `Enterprise Developer Edition (64-bit)`. Les éditions *Developer* offrent toutes les fonctions de l'édition correspondante, gratuitement, mais leur licence les réserve au développement et aux tests : **pas de production**.

L'édition se choisit avec la variable `MSSQL_PID`, que le `compose.yaml` du cours ne définit pas. Essai sur l'image `2025-latest` :

| `MSSQL_PID` | Édition obtenue |
| --- | --- |
| *(absente)* | Enterprise Developer |
| `EnterpriseDeveloper` | Enterprise Developer |
| `StandardDeveloper` | Standard Developer |
| `Developer` | Enterprise Developer — valeur des versions précédentes, encore acceptée, mais absente de la liste documentée pour 2025 |

Documentation : [Éditions de SQL Server 2025](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17), [variables d'environnement](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-configure-environment-variables?view=sql-server-ver17)

### 2. Ajouter le fournisseur SQL Server

```powershell
dotnet package add Microsoft.EntityFrameworkCore.SqlServer --project Chronos.Infrastructure --version 10.0.15
```

### 3. Choisir le fournisseur par configuration

Dans `Program.cs`, l'enregistrement du contexte devient :

```csharp
var fournisseur = builder.Configuration["Chronos:Fournisseur"] ?? "PostgreSql";
var chaineDeConnexion = builder.Configuration.GetConnectionString("Chronos");

builder.Services.AddDbContext<ChronosDbContext>(options =>
{
    _ = fournisseur switch
    {
        "PostgreSql" => options.UseNpgsql(chaineDeConnexion,
            npgsql => npgsql.MigrationsAssembly("Chronos.Migrations.PostgreSql")),

        "SqlServer" => options.UseSqlServer(chaineDeConnexion,
            sqlServer => sqlServer.MigrationsAssembly("Chronos.Migrations.SqlServer")),

        _ => throw new InvalidOperationException(
            $"Fournisseur de base de données inconnu : « {fournisseur} ». Valeurs admises : PostgreSql, SqlServer.")
    };

    if (builder.Environment.IsDevelopment())
    {
        options
            .UseSeeding((context, _) => DonneesInitiales.Alimenter(context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                DonneesInitiales.AlimenterAsync(context, cancellationToken));
    }
});
```

- **`Chronos:Fournisseur`** est une clé de configuration ordinaire : fichier `appsettings.json`, secrets utilisateur, variable d'environnement ou argument de ligne de commande. Sans elle, c'est PostgreSQL.
- **`MigrationsAssembly`** indique dans quel projet se trouvent les migrations du fournisseur choisi.
- **Une valeur inconnue arrête l'application** au démarrage, avec un message qui dit quoi corriger. Un défaut silencieux enverrait l'application sur la mauvaise base.
- Le reste du code — endpoints, configurations, données initiales — ne change pas.

### 4. Un projet par jeu de migrations

```powershell
dotnet new classlib --name Chronos.Migrations.PostgreSql
```

```powershell
dotnet new classlib --name Chronos.Migrations.SqlServer
```

```powershell
dotnet sln add Chronos.Migrations.PostgreSql Chronos.Migrations.SqlServer
```

```powershell
dotnet reference add Chronos.Infrastructure --project Chronos.Migrations.PostgreSql
```

```powershell
dotnet reference add Chronos.Infrastructure --project Chronos.Migrations.SqlServer
```

```powershell
dotnet reference add Chronos.Migrations.PostgreSql --project Chronos.Api
```

```powershell
dotnet reference add Chronos.Migrations.SqlServer --project Chronos.Api
```

Supprimez les deux `Class1.cs` et allégez les deux `.csproj`, comme pour les autres projets.

Les projets de migrations référencent l'infrastructure, où se trouve le contexte ; l'API les référence tous les deux, pour que leurs assemblies soient à côté d'elle quand l'outil ou l'application en a besoin. L'infrastructure, elle, ne les connaît que par leur nom : une référence dans ce sens créerait un cycle.

Déplacez maintenant les migrations du module 06 : le dossier `Chronos.Infrastructure/Migrations` devient `Chronos.Migrations.PostgreSql/Migrations`. Dans ses fichiers, remplacez `namespace Chronos.Infrastructure.Migrations` par `namespace Chronos.Migrations.PostgreSql.Migrations`.

Vérifiez que rien n'est perdu :

```powershell
dotnet ef migrations list --project Chronos.Migrations.PostgreSql --startup-project Chronos.Api
```

```powershell
dotnet ef migrations has-pending-model-changes --project Chronos.Migrations.PostgreSql --startup-project Chronos.Api
```

La première commande liste vos migrations, sans la mention `(Pending)` : la base les reconnaît comme déjà appliquées, parce que la table `__EFMigrationsHistory` ne retient que leur identifiant, pas leur emplacement. La seconde répond qu'il n'y a aucun changement.

### 5. Créer le jeu SQL Server

```powershell
dotnet ef migrations add Initiale --project Chronos.Migrations.SqlServer --startup-project Chronos.Api -- --Chronos:Fournisseur SqlServer
```

Tout ce qui suit `--` n'est pas lu par `dotnet ef` : c'est transmis à l'application, dont la configuration lit les arguments de ligne de commande. Pour cette exécution, `Chronos:Fournisseur` vaut donc `SqlServer`. Créer une migration ne demande aucune connexion : la commande réussit alors que vos secrets désignent toujours PostgreSQL.

Sans l'argument, l'outil vous arrête, et son message résume la situation :

```text
Your target project 'Chronos.Migrations.SqlServer' doesn't match your migrations assembly 'Chronos.Migrations.PostgreSql'.
```

Ouvrez `Chronos.Migrations.SqlServer/Migrations/<horodatage>_Initiale.cs` et comparez avec le jeu PostgreSQL : mêmes tables, mêmes index, mais `nvarchar(20)` au lieu de `character varying(20)`, `bit` au lieu de `boolean`, et l'annotation `SqlServer:Identity`. Ce jeu n'a qu'une migration, là où l'autre en a plusieurs : chaque jeu a son histoire, seul l'état final doit correspondre au modèle.

### 6. La chaîne de connexion SQL Server

```text
Server=localhost,1433;Database=chronos;User Id=sa;Password=VOTRE_MOT_DE_PASSE;Encrypt=True;TrustServerCertificate=True
```

- Le port se sépare par une **virgule**.
- **`Encrypt=True`** est déjà la valeur par défaut du pilote `Microsoft.Data.SqlClient` : la connexion est chiffrée.
- **`TrustServerCertificate=True`** accepte le certificat du serveur sans le vérifier. Le conteneur présente un certificat auto-signé ; sans cette option, la connexion échoue pendant la négociation, avec une erreur de certificat non approuvé. C'est acceptable sur votre poste, **jamais en production**, où le serveur doit présenter un certificat valide. Ne « réparez » pas non plus l'erreur avec `Encrypt=False`.
- **`sa`** est l'administrateur du serveur, le seul compte que crée le conteneur. En production, l'application se connecte avec un compte à elle, limité à sa base.

Pour faire de SQL Server votre base courante, changez **les deux** secrets :

```powershell
dotnet user-secrets set "Chronos:Fournisseur" "SqlServer" --project Chronos.Api
```

```powershell
dotnet user-secrets set "ConnectionStrings:Chronos" "Server=localhost,1433;Database=chronos;User Id=sa;Password=VOTRE_MOT_DE_PASSE;Encrypt=True;TrustServerCertificate=True" --project Chronos.Api
```

Le mot de passe est la valeur de `MSSQL_SA_PASSWORD` de votre `.env`. Si vous ne changez qu'un secret sur deux, le fournisseur SQL Server reçoit une chaîne PostgreSQL et le dit à sa façon : `Keyword not supported: 'host'`.

Documentation : [Chaînes de connexion de `Microsoft.Data.SqlClient`](https://learn.microsoft.com/en-us/sql/connect/ado-net/connection-string-syntax), [chiffrement et validation du certificat](https://learn.microsoft.com/en-us/sql/connect/ado-net/encryption-and-certificate-validation)

### 7. Appliquer, démarrer

```powershell
dotnet ef database update --project Chronos.Migrations.SqlServer --startup-project Chronos.Api
```

Le conteneur SQL Server ne contient aucune base `chronos` : EF Core la crée, applique la migration, puis exécute la fonction d'alimentation du module 06, sans qu'une ligne en ait été modifiée.

```powershell
dotnet run --project Chronos.Api
```

```powershell
curl.exe "http://localhost:5080/api/projets?page=1&taille=1"
```

La réponse a exactement la forme obtenue avec PostgreSQL.

### 8. Changer de base sans toucher aux secrets

La configuration d'ASP.NET Core a un ordre de priorité : les arguments de ligne de commande l'emportent sur les variables d'environnement, qui l'emportent sur les secrets utilisateur, qui l'emportent sur `appsettings.json`. On peut donc garder un SGBD dans ses secrets et viser l'autre ponctuellement.

Pour un terminal entier — dans une variable d'environnement, `:` s'écrit `__` :

```powershell
$env:Chronos__Fournisseur = "PostgreSql"
```

```powershell
$env:ConnectionStrings__Chronos = "Host=localhost;Port=5432;Database=chronos;Username=chronos;Password=VOTRE_MOT_DE_PASSE"
```

Pour une seule commande de l'outil :

```powershell
dotnet ef database update --project Chronos.Migrations.PostgreSql --startup-project Chronos.Api --connection "Host=localhost;Port=5432;Database=chronos;Username=chronos;Password=VOTRE_MOT_DE_PASSE" -- --Chronos:Fournisseur PostgreSql
```

Ces deux formes laissent un mot de passe dans l'historique du terminal : réservez-les au mot de passe de développement.

Documentation : [Configuration dans ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0)

### 9. Comparer le SQL, constater les différences

Appelez `/api/projets` sur chaque base et lisez le journal. La même requête LINQ du module 06 donne :

| | PostgreSQL | SQL Server |
| --- | --- | --- |
| Noms | `"Projets"`, `p."Code"` | `[Projets]`, `[p].[Code]` |
| Compter | `SELECT count(*)::int` | `SELECT COUNT(*)` |
| Paginer | `LIMIT @p1 OFFSET @p` | `OFFSET @p ROWS FETCH NEXT @p1 ROWS ONLY` |
| Concaténer | `c0."Prenom" \|\| ' ' \|\| c0."Nom"` | `[c0].[Prenom] + N' ' + [c0].[Nom]` |
| Filtre d'archivage | `WHERE NOT (c."EstArchive")` | `WHERE [c].[EstArchive] = CAST(0 AS bit)` |
| Récupérer la clé créée | `INSERT ... RETURNING "Id"` | `INSERT ... OUTPUT INSERTED.[Id]` |

Le fournisseur a fait son travail : vous n'avez rien eu à savoir de ces dialectes.

Créez maintenant, sur chaque base, un projet dont le code est `bdl-refonte`, en minuscules, alors que `BDL-REFONTE` existe :

| | PostgreSQL | SQL Server |
| --- | --- | --- |
| `POST /api/projets` avec le code `bdl-refonte` | `201` : un second projet est créé. | `409` : le contrôle `AnyAsync` trouve déjà le code. |
| `Where(c => c.Nom == "martin")` | 0 ligne | 1 ligne |
| `Where(c => c.Nom.Contains("mart"))` | 0 ligne | 1 ligne |
| `Where(c => c.Nom.ToLower().Contains("mart"))` | 1 ligne | 1 ligne |
| `EF.Functions.ILike(c.Nom, "%mart%")` | 1 ligne (`ILIKE`) | Exception : `could not be translated` |
| `c.Nom.Equals("martin", StringComparison.OrdinalIgnoreCase)` | Exception : `could not be translated` | Exception : `could not be translated` |

Ici, le fournisseur ne peut rien : c'est la base qui compare. RG-10 n'a pas le même sens des deux côtés, et seule la troisième écriture de la recherche est portable. Supprimez le projet `bdl-refonte` de la base PostgreSQL ; l'exercice 1 empêchera qu'il revienne.

### 10. La routine à deux jeux

Désormais, **chaque changement du modèle** se termine par deux commandes :

```powershell
dotnet ef migrations add NomDuChangement --project Chronos.Migrations.PostgreSql --startup-project Chronos.Api -- --Chronos:Fournisseur PostgreSql
```

```powershell
dotnet ef migrations add NomDuChangement --project Chronos.Migrations.SqlServer --startup-project Chronos.Api -- --Chronos:Fournisseur SqlServer
```

Écrivez toujours l'argument, même pour votre fournisseur courant : la commande ne dépend plus de l'état de vos secrets. Et pour ne pas oublier un jeu, la vérification du module 06 se fait deux fois :

```powershell
dotnet ef migrations has-pending-model-changes --project Chronos.Migrations.SqlServer --startup-project Chronos.Api -- --Chronos:Fournisseur SqlServer
```

Elle ne se connecte à aucune base.

## À vous

1. **Décider de la casse (RG-01, RG-10).** Faites normaliser par le domaine le code d'un projet (majuscules) et l'adresse électronique d'un collaborateur (minuscules), dans leurs constructeurs. Écrivez d'abord les tests unitaires. Vérifiez ensuite que le `POST` de l'étape 9 répond `409` sur les **deux** bases. Les lignes déjà enregistrées sont-elles conformes ? Que faudrait-il faire en production ? Le matricule doit-il être normalisé aussi ?

2. **Une recherche portable.** Ajoutez le paramètre `recherche` à `GET /api/collaborateurs` : il filtre sur le nom ou le prénom, sans tenir compte de la casse, avec le même résultat sur les deux bases. Comparez le SQL produit. Essayez ensuite une recherche de `helene` quand la base contient `Hélène` : que se passe-t-il sur chaque base, et pourquoi ?

3. **Un changement, deux migrations.** Ajoutez un index (non unique) sur le nom des collaborateurs. Créez la migration dans les deux jeux, comparez les deux fichiers, appliquez-les sur les deux bases. Puis créez volontairement un écart : modifiez une longueur et ne générez la migration que pour PostgreSQL. Quelle commande le révèle ? Annulez.

4. **Deux vérifications dans l'intégration continue.** Complétez l'étape ajoutée à l'exercice 5 du module 06 pour qu'elle vérifie les deux jeux. Faut-il un service de base de données dans le workflow ?

5. **Les horodatages, par l'expérience.** Sur une branche que vous jetterez, ajoutez à `Client` une propriété `DateTimeOffset CreeLe`. Générez la migration dans les deux jeux et relevez le type des deux colonnes. Dans les données initiales, donnez-lui successivement `DateTimeOffset.UtcNow`, puis `DateTimeOffset.Now`, et lancez `database update` sur chaque base, vidée au préalable. Qu'observez-vous ? Qu'aurait-on observé sur une machine réglée en UTC, comme celle de l'intégration continue ? Reformulez avec vos mots la règle du cours sur les horodatages. Supprimez la branche.

6. **L'édition du client.** Votre client exploite SQL Server en édition Standard. Faites tourner votre conteneur de développement dans l'édition *Standard Developer* et vérifiez l'édition obtenue. Repartez d'un volume neuf : l'essai du cours n'a pas porté sur un volume créé sous une autre édition. Qu'y gagne-t-on par rapport à l'édition par défaut ?

## Critères de réussite

- La solution contient `Chronos.Migrations.PostgreSql` et `Chronos.Migrations.SqlServer` ; `Chronos.Infrastructure` ne contient plus de dossier `Migrations`.
- `dotnet ef database update` aboutit sur une base PostgreSQL vide et sur un serveur SQL Server vide, chacun avec son projet de migrations.
- `dotnet ef migrations has-pending-model-changes` ne signale aucun changement, pour les deux fournisseurs.
- L'API démarre sur l'une ou l'autre base en ne changeant que de la configuration, et `GET /api/projets` renvoie le même JSON.
- Une valeur inconnue de `Chronos:Fournisseur` arrête l'application avec un message explicite.
- Créer `bdl-refonte` quand `BDL-REFONTE` existe répond `409` sur les deux bases.
- Aucune chaîne de connexion n'est versionnée ; aucun fichier ne contient `TrustServerCertificate` en dehors de vos secrets.
- Vous savez dire, sans regarder le cours, pourquoi Chronos n'utilise ni `rowversion` ni `xmin`.
- Comparez avec la solution de référence : `git diff module-06 module-07`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| Un seul dossier `Migrations` pour les deux SGBD | Une migration contient les types d'un fournisseur ; elle échoue ou crée un schéma faux sur l'autre. | Un jeu par fournisseur, chacun dans son projet. |
| Une migration commune retouchée à la main pour tester le fournisseur actif | L'outil ne génère que pour un fournisseur à la fois : chaque migration serait à réécrire, sans rien pour vérifier le résultat. | Deux jeux, générés par l'outil, comme le prescrit la documentation. |
| Générer la migration pour un seul fournisseur | L'autre base n'a plus le schéma du modèle ; on le découvre en déploiement. | Les deux commandes de l'étape 10, et la vérification en intégration continue. |
| Oublier `-- --Chronos:Fournisseur …` | L'outil utilise le fournisseur de vos secrets : erreur de projet cible, ou migration créée dans le mauvais jeu. | Toujours écrire l'argument. |
| `TrustServerCertificate=True` ou `Encrypt=False` en production | La connexion n'authentifie plus le serveur, ou n'est plus chiffrée. | Un certificat valide sur le serveur. |
| Se connecter avec `sa` depuis l'application déployée | L'application a tous les droits sur le serveur. | Un compte dédié, limité à sa base. |
| `System.Data.SqlClient` | Paquet déprécié. | `Microsoft.Data.SqlClient`, amené par le fournisseur EF Core. |
| `DateTime.Now` ou `DateTimeOffset.Now` pour un horodatage | Npgsql refuse tout ce qui n'est pas UTC ; SQL Server l'accepte : le bogue n'apparaît que sur une base. | `TimeProvider.GetUtcNow()`. |
| `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)` | Rétablit le comportement d'avant Npgsql 6 pour faire taire l'erreur, au lieu de corriger les dates. | Des horodatages en UTC. |
| Type SQL Server `datetime` | Déconseillé pour du code neuf par la documentation de SQL Server. | `datetimeoffset` (ou `datetime2`), choisis par EF Core. |
| `EF.Functions.ILike` dans une requête commune | Propre à PostgreSQL : exception sur SQL Server. | `ToLower()` des deux côtés, ou un choix de collation assumé par SGBD. |
| `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` dans une requête | EF Core refuse de le traduire. | Idem. |
| Compter sur l'index unique pour refuser `a@x.fr` quand `A@x.fr` existe | Vrai sur SQL Server, faux sur PostgreSQL. | Normaliser la valeur dans le domaine. |
| `UseXminAsConcurrencyToken()` | Obsolète depuis Npgsql 7. | Une propriété `uint` avec `IsRowVersion()` — ou, comme ici, un jeton applicatif. |
| `UseSnakeCaseNamingConvention()` présenté comme une fonction de Npgsql | Elle vient d'un paquet tiers, `EFCore.NamingConventions`. | Voir « Pour aller plus loin ». |
| Utiliser en développement une fonction de l'édition Enterprise | Le conteneur par défaut l'accepte ; le SQL Server Standard du client la refusera. | Connaître l'édition cible ; `MSSQL_PID=StandardDeveloper` au besoin. |
| Comparer à l'égalité un horodatage écrit puis relu | PostgreSQL garde 6 décimales, .NET et SQL Server 7. | Comparer avec une tolérance, ou tronquer avant d'écrire. |

Les sources sont dans le [référentiel .NET 10](../docs/references/dotnet-10.md#3-sql-server-et-postgresql).

## Pour la séance

- La documentation propose deux organisations des migrations, et une troisième variante où chaque projet de migrations est son propre projet de démarrage. Avec vos mots : pourquoi Chronos garde-t-il un seul contexte et l'API comme projet de démarrage ? Dans quel cas feriez-vous l'autre choix ?
- Le jeu PostgreSQL a plusieurs migrations, le jeu SQL Server une seule, pour le même modèle. Est-ce un problème ? Qu'est-ce qui doit être identique entre les deux jeux, et qu'est-ce qui n'a pas besoin de l'être ?
- Jeton de concurrence natif ou applicatif : qu'apporte le natif que le `Guid` n'apporte pas ? Si Chronos ne visait que SQL Server, que choisiriez-vous ?
- Le cours normalise la casse dans le domaine plutôt que de régler une collation. Qu'est-ce que ce choix ne résout pas ?
- « Un seul code » : listez ce qui, dans votre dépôt, dépend malgré tout du SGBD. Cette liste vous paraît-elle courte ou longue ?
- Prendre en charge deux SGBD a un coût, à chaque changement du modèle. Qui devrait décider de le payer, et sur quels arguments ?
- Le bloc de `Program.cs` qui enregistre le contexte est devenu le plus long du fichier. Où le rangeriez-vous, et qu'est-ce que cela changerait pour les outils et pour les tests ?

## Pour aller plus loin

- **Nommage `snake_case`.** PostgreSQL met en minuscules tout nom écrit sans guillemets ; les tables d'EF Core, nommées `"Projets"`, obligent donc à écrire des guillemets dans `psql`. Le paquet [`EFCore.NamingConventions`](https://github.com/efcore/EFCore.NamingConventions) renomme tout en `snake_case`. Le cours ne l'utilise pas, pour trois raisons : c'est un paquet communautaire, que Microsoft ne prend pas en charge ; l'ajouter à une base existante produit une migration qui renomme tout ; et garder les mêmes noms sur les deux SGBD rend leurs migrations et leur SQL directement comparables.
- [Un projet de migrations séparé](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects) : la variante où le projet de migrations contient une fabrique de contexte (`IDesignTimeDbContextFactory`) et sert lui-même de projet de démarrage. L'outil n'exécute alors plus le démarrage de l'application ; en échange, la configuration du contexte existe en deux endroits, qu'il faut garder identiques.
- [Collations de PostgreSQL](https://www.postgresql.org/docs/18/collation.html) : les collations non déterministes, pour des comparaisons insensibles à la casse ou aux accents décidées par la base.
- [Traductions de Npgsql](https://www.npgsql.org/efcore/mapping/translations.html) : ce que devient chaque méthode .NET en SQL PostgreSQL.
- [Notes de version de Npgsql 10](https://www.npgsql.org/efcore/release-notes/10.0.html).
- Suite dans le cours : le [module 08](08-une-api-robuste.md) met en œuvre le jeton `Guid Version`, le [module 11](11-tests-d-integration.md) fait tourner les mêmes tests sur les deux SGBD, le module 12 ajoute les horodatages de traçabilité, le module 25 applique les migrations en déploiement.
