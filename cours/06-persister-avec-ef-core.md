# Module 06 — Persister avec EF Core

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 10](../docs/references/dotnet-10.md).
> Le code et les commandes `dotnet` de ce module ont été **exécutés le 12 novembre 2026** dans le conteneur `mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.404, EF Core 10.0.15, fournisseur Npgsql 10.0.3, outil `dotnet-ef` 10.0.15) contre PostgreSQL 18.6, sur un squelette de la solution écrit pour l'essai : `Projet` et `Periode` y sont ceux du module 04 ; `Client`, `Collaborateur` et `Contrat`, que le module 04 vous fait écrire en exercice, y étaient une version minimale, et les vôtres peuvent différer. Les extraits SQL sont ceux réellement produits. **Non exécutés** : la saisie des commandes dans PowerShell sous Windows (elles l'ont été dans un terminal Linux), l'interface Scalar, et l'ajout au workflow GitHub Actions demandé en exercice (les commandes qu'il lance ont été exécutées localement, sans secret ni base). Signalez au mentor tout écart constaté.
> **Rejoué le 12 novembre 2026 sur la solution de référence** (état du tag `module-05`, mêmes versions : SDK 10.0.404, EF Core 10.0.15, Npgsql 10.0.3, `dotnet-ef` 10.0.15, PostgreSQL 18) : pas à pas et exercices, `database drop` puis `update`, appels HTTP aux trois ressources, `has-pending-model-changes` sans secrets utilisateur. Le workflow GitHub Actions lui-même n'a pas été exécuté.

Depuis le module 03, `/api/projets` sert une liste en mémoire : tout est perdu à chaque redémarrage, et rien n'empêche deux projets de porter le même code. Chronos doit conserver ses données dans une base, et c'est la base qui garantira certaines règles — l'unicité, notamment. Ce module branche l'API sur PostgreSQL avec **Entity Framework Core** (EF Core), la bibliothèque d'accès aux données de .NET.

## Objectifs

À la fin de ce module, vous saurez :

- décrire le rôle d'un `DbContext` et l'enregistrer dans l'application ;
- configurer la correspondance entre vos entités et les tables : longueurs, index uniques, énumérations, relations ;
- créer une migration, la relire, lire le SQL qu'elle produit et l'appliquer ;
- alimenter la base de développement avec des données initiales ;
- écrire des requêtes LINQ qui ne ramènent que le nécessaire, et repérer dans le journal celles qui ne le font pas ;
- archiver sans supprimer, à l'aide d'un filtre de requête.

## Prérequis

- Modules [03](03-premiere-api.md), [04](04-modeliser-le-domaine.md) et [05](05-github-actions-integration-continue.md) : la solution `src/api/` contient `Chronos.Api` (endpoints `/api/projets` servis par `ProjetsEnMemoire` — liste filtrable par `?statut=`, lecture, création, suppression — et `/api/clients` servis par `ClientsEnMemoire`, exercices du module 03), `Chronos.Domain` (`Projet`, `Periode`, et les entités des exercices : `Client`, `Collaborateur`, `Contrat`) et un workflow d'intégration continue.
- [Module 01](01-docker-les-bases.md) : la base PostgreSQL démarre avec `docker compose up -d --wait postgres`, depuis la racine du dépôt.
- Des notions de SQL : `SELECT`, `JOIN`, clé primaire, clé étrangère, index.

Toutes les commandes `dotnet` de ce module se lancent depuis `src/api/`.

## Règles de gestion

- **RG-10** — Le code d'un projet est unique : index unique (pas à pas).
- **RG-01** — Le matricule et l'adresse électronique d'un collaborateur sont uniques : index uniques (exercice 1).
- **RG-05** — Un collaborateur qui a des données liées n'est jamais supprimé physiquement, il est archivé : filtre de requête (pas à pas), endpoint d'archivage (exercice 4).

## Notions

### Le `DbContext`

EF Core est un **ORM** : il fait correspondre des objets C# à des lignes de tables, traduit vos requêtes LINQ en SQL et transforme les modifications de vos objets en `INSERT`, `UPDATE` et `DELETE`.

Le `DbContext` est le point d'entrée. Il représente une **session de travail** avec la base : il expose un `DbSet<T>` par type d'entité, retient les entités qu'il a chargées, et écrit tous les changements en une fois quand on appelle `SaveChangesAsync`.

Dans Chronos, un `ChronosDbContext` est créé pour chaque requête HTTP et détruit à la fin (durée de vie *scoped*). C'est voulu : un contexte n'est pas fait pour être partagé entre plusieurs traitements simultanés.

Documentation : [Durée de vie, configuration et initialisation du DbContext](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)

### Le modèle : conventions, puis configuration

EF Core déduit beaucoup de choses de vos classes : une propriété `Id` devient la clé primaire, `ClientId` accompagné d'une navigation `Client` devient une clé étrangère, un `string` non *nullable* devient une colonne `NOT NULL`. Ce sont les **conventions**.

Ce qu'il ne peut pas deviner, vous le **configurez** : la longueur d'un code, l'unicité d'un matricule, la précision d'un nombre décimal. Chronos écrit cette configuration dans une classe par entité, qui implémente `IEntityTypeConfiguration<T>`, dans `Chronos.Infrastructure`. Le domaine reste ainsi sans aucune référence à EF Core.

Documentation : [Créer et configurer un modèle](https://learn.microsoft.com/en-us/ef/core/modeling/), [Propriétés](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties), [Index](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)

### Comment EF Core construit vos entités

Vos entités du module 04 ont un seul constructeur, qui exige tout, et des *setters* privés. EF Core s'en accommode sans que vous les retouchiez, parce que le module 04 a respecté trois règles :

- un *setter* **privé** lui suffit : la propriété est mappée normalement. Une propriété **sans** *setter* n'est pas mappée par convention ;
- quand chaque paramètre du constructeur porte le nom et le type d'une propriété mappée (`code` pour `Code`), EF Core appelle ce constructeur avec les valeurs lues en base, puis renseigne les autres propriétés (`Id`, `Statut`) par leur *setter* ;
- un paramètre de constructeur ne peut pas être une navigation (`Client client`) ni une valeur qui ne correspond à aucune propriété. L'erreur est alors explicite : `No suitable constructor was found for the type 'Client'`. On la corrige en alignant le paramètre sur une propriété, pas en ajoutant un constructeur vide.

Conséquence à garder en tête : puisque EF Core passe par le constructeur de `Projet`, RG-11, RG-12 et ses garde-fous se réexécutent **à chaque lecture** d'une entité. Essai fait : après avoir placé en SQL une date de fin antérieure au début, `db.Projets.ToListAsync()` lève `RegleMetierException`. Une projection vers un DTO, elle, ne construit pas l'entité et n'est pas concernée.

Une collection exposée en lecture seule (`IReadOnlyCollection<Contrat> Contrats => _contrats;`) fonctionne aussi : EF Core trouve le champ `_contrats` par convention de nommage et le remplit directement.

Documentation : [Types d'entité avec constructeurs](https://learn.microsoft.com/en-us/ef/core/modeling/constructors), [Champs de stockage](https://learn.microsoft.com/en-us/ef/core/modeling/backing-field)

### Les migrations

Le schéma de la base doit suivre le modèle. Une **migration** est un fichier C# qui décrit le passage d'un état du schéma au suivant (`Up`) et le retour en arrière (`Down`). EF Core la génère en comparant votre modèle actuel à une photographie du modèle précédent, le **snapshot**, qu'il tient à jour à côté des migrations. Dans la base, la table `__EFMigrationsHistory` retient les migrations déjà appliquées.

Les migrations se versionnent avec le code : le schéma a ainsi un historique, relu en *pull request* comme le reste.

L'outil `dotnet ef` manipule deux projets : le **projet cible**, où il écrit les migrations (`--project`), et le **projet de démarrage**, qu'il compile et exécute pour obtenir la configuration du contexte (`--startup-project`). Pour Chronos : `Chronos.Infrastructure` et `Chronos.Api`.

Documentation : [Vue d'ensemble des migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/), [Référence de `dotnet ef`](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

### Suivi, projection et données liées

Quand une requête renvoie des entités, le contexte les **suit** : il garde une copie de leurs valeurs pour détecter ce que vous modifiez. C'est indispensable pour écrire, et inutile pour lire.

Pour une lecture, deux moyens d'éviter ce coût :

- la **projection** : `Select` construit directement le DTO. Seules les colonnes utiles sont lues, et rien n'est suivi ;
- `AsNoTracking()`, quand on a réellement besoin des entités mais pas de les modifier.

Une navigation (`projet.Responsable`) n'est **pas chargée** automatiquement. On l'obtient soit avec `Include`, qui charge l'entité liée entière, soit par une projection qui ne prend que ce dont elle a besoin (`p.Responsable.Nom`). Charger les données liées une par une, dans une boucle, est le **problème N+1** : une requête pour la liste, puis une par élément.

Documentation : [Requêtes avec et sans suivi](https://learn.microsoft.com/en-us/ef/core/querying/tracking), [Chargement des données liées](https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager), [Requêtes efficaces](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)

### Filtre de requête

Un **filtre de requête** est une condition attachée à un type d'entité, ajoutée par EF Core à toutes les requêtes sur ce type. Chronos s'en sert pour RG-05 : un collaborateur archivé reste en base, mais disparaît des requêtes ordinaires. Depuis EF Core 10, un filtre porte un **nom**, ce qui permet de le désactiver seul, requête par requête.

Documentation : [Filtres de requête globaux](https://learn.microsoft.com/en-us/ef/core/querying/filters)

## Pas à pas

### 1. Créer le projet d'infrastructure

```powershell
dotnet new classlib --name Chronos.Infrastructure
```

```powershell
dotnet sln add Chronos.Infrastructure
```

```powershell
dotnet reference add Chronos.Domain --project Chronos.Infrastructure
```

```powershell
dotnet reference add Chronos.Infrastructure --project Chronos.Api
```

```powershell
dotnet reference remove Chronos.Domain/Chronos.Domain.csproj --project Chronos.Api
```

Comme au module 04, supprimez `Class1.cs` et retirez du `.csproj` les trois réglages que `Directory.Build.props` fournit déjà.

La dernière commande retire la référence directe de l'API vers le domaine, posée au module 04 : l'API atteint désormais le domaine **à travers** l'infrastructure. Les dépendances vont dans un seul sens : `Chronos.Api` → `Chronos.Infrastructure` → `Chronos.Domain`. Pour le retrait, donnez le chemin du fichier `.csproj` : avec le seul nom du dossier, le SDK 10.0.404 répond que la référence est introuvable.

Ajoutez les paquets, **en précisant la version** :

```powershell
dotnet package add Npgsql.EntityFrameworkCore.PostgreSQL --project Chronos.Infrastructure --version 10.0.3
```

```powershell
dotnet package add Microsoft.EntityFrameworkCore.Relational --project Chronos.Infrastructure --version 10.0.15
```

```powershell
dotnet package add Microsoft.EntityFrameworkCore.Design --project Chronos.Api --version 10.0.15
```

- `Npgsql.EntityFrameworkCore.PostgreSQL` est le fournisseur PostgreSQL. Il amène EF Core avec lui.
- `Microsoft.EntityFrameworkCore.Relational` fixe la version d'EF Core. Sans cette ligne, le fournisseur se contente de la version minimale qu'il exige (10.0.4 lors de l'essai) et le projet ne reçoit pas les correctifs suivants.
- `Microsoft.EntityFrameworkCore.Design` est requis par l'outil `dotnet ef` dans le projet de démarrage. La commande l'ajoute avec `PrivateAssets` : il ne sert qu'au développement.

Ouvrez `Directory.Packages.props` : les trois versions s'y trouvent, et les fichiers `.csproj` ne contiennent que les noms des paquets. C'est le piège des versions 11 du module 03 : sans `--version`, la commande aurait choisi la dernière version publiée, une 11.x, qui exige .NET 11.

### 2. Écrire le contexte

`Chronos.Infrastructure/ChronosDbContext.cs` :

```csharp
using Chronos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure;

public class ChronosDbContext(DbContextOptions<ChronosDbContext> options) : DbContext(options)
{
    public DbSet<Collaborateur> Collaborateurs { get; set; }
    public DbSet<Contrat> Contrats { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Projet> Projets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChronosDbContext).Assembly);
    }
}
```

- Le constructeur reçoit les **options** : quel SGBD, quelle chaîne de connexion. Le contexte ne les connaît pas ; c'est l'application qui les fournit.
- Chaque `DbSet<T>` est une table interrogeable. EF Core les initialise lui-même.
- `ApplyConfigurationsFromAssembly` applique toutes les classes `IEntityTypeConfiguration<T>` du projet : vous n'aurez pas à les déclarer une à une.

### 3. Configurer `Projet`

`Chronos.Infrastructure/Configurations/ProjetConfiguration.cs` :

```csharp
using Chronos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chronos.Infrastructure.Configurations;

public class ProjetConfiguration : IEntityTypeConfiguration<Projet>
{
    public void Configure(EntityTypeBuilder<Projet> builder)
    {
        builder.Property(p => p.Code).HasMaxLength(20);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Libelle).HasMaxLength(200);
        builder.Property(p => p.Nature).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Statut).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.Client)
            .WithMany()
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Responsable)
            .WithMany()
            .HasForeignKey(p => p.ResponsableId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

Ce qu'il faut y lire :

- **`HasMaxLength`** : sans longueur, une chaîne devient une colonne de taille illimitée (`text` sous PostgreSQL).
- **`HasIndex(...).IsUnique()`** met en œuvre RG-10. C'est la base qui refusera un doublon, même si deux requêtes arrivent au même instant.
- **`HasConversion<string>()`** enregistre l'énumération sous son nom (`'EnCours'`) plutôt que sous son numéro. C'est le choix du cours : la base se lit sans le code sous les yeux, et insérer une valeur au milieu de l'énumération ne change pas le sens des lignes existantes. Le prix : un peu de place, et une mise à jour des données le jour où l'on renomme une valeur.
- **`HasOne(...).WithMany().HasForeignKey(...)`** relie la navigation `Responsable` à la clé étrangère `ResponsableId`. `WithMany()` est vide : `Collaborateur` n'a pas de collection de ses projets, et n'en a pas besoin.
- **`OnDelete(DeleteBehavior.Restrict)`** : `ResponsableId` n'est pas *nullable*, la relation est donc obligatoire, et la convention pour une relation obligatoire est la suppression **en cascade**. Supprimer un collaborateur supprimerait ses projets. On l'interdit.

Ajoutez sur le même modèle une `ClientConfiguration` qui limite `Nom` à 200 caractères.

Documentation : [Conversions de valeurs](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions), [Relations un-à-plusieurs](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many), [Suppression en cascade](https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete)

### 4. Enregistrer le contexte et la chaîne de connexion

Dans `Chronos.Api/Program.cs`, avant `builder.Build()` :

```csharp
builder.Services.AddDbContext<ChronosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Chronos")));
```

avec, en tête de fichier, `using Chronos.Infrastructure;` et `using Microsoft.EntityFrameworkCore;`.

`GetConnectionString("Chronos")` lit la clé de configuration `ConnectionStrings:Chronos`. Cette chaîne contient un mot de passe : elle n'a rien à faire dans `appsettings.json`, qui est versionné. En développement, on la range dans les **secrets utilisateur**, un fichier stocké dans votre profil Windows, hors du dépôt.

```powershell
dotnet user-secrets init --project Chronos.Api
```

```powershell
dotnet user-secrets set "ConnectionStrings:Chronos" "Host=localhost;Port=5432;Database=chronos;Username=chronos;Password=VOTRE_MOT_DE_PASSE" --project Chronos.Api
```

La première commande ajoute un identifiant `UserSecretsId` dans `Chronos.Api.csproj` ; versionnez ce changement. Avec le SDK 10.0.404, elle réécrit le fichier entier : marque d'ordre d'octets (BOM) ajoutée, `<PropertyGroup>` collé sur la première ligne. Relisez le diff et remettez le fichier en forme, pour ne versionner que le nouveau `<PropertyGroup>` et son `UserSecretsId`. Dans la seconde, remplacez le mot de passe par la valeur de `POSTGRES_PASSWORD` de votre fichier `.env`.

Les secrets utilisateur ne sont lus que dans l'environnement `Development`, et ne sont pas chiffrés : c'est un outil de poste de développement, pas un coffre-fort.

Documentation : [Secrets en développement](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0), [Paramètres de connexion Npgsql](https://www.npgsql.org/doc/connection-string-parameters.html)

### 5. Installer l'outil `dotnet ef`

`dotnet ef` n'est pas fourni avec le SDK. La documentation présente deux installations : globale, pour tout le poste, ou locale, pour un dépôt. Le cours retient l'**outil local** : sa version est écrite dans le dépôt, tous les développeurs et l'intégration continue utilisent la même.

```powershell
dotnet new tool-manifest
```

```powershell
dotnet tool install dotnet-ef --version 10.0.15
```

```powershell
dotnet ef --version
```

La première commande crée le manifeste `dotnet-tools.json`, la deuxième y inscrit l'outil, la troisième doit afficher `10.0.15`. **Versionnez `dotnet-tools.json`.** Sur un autre poste, `dotnet tool restore` installe les outils du manifeste.

> Avec le SDK 10.0.404, le manifeste est créé directement dans `src/api/`. La documentation décrit encore un sous-dossier `.config/` : ne soyez pas surpris de ne pas le trouver.

Gardez la version de l'outil alignée sur celle des paquets EF Core.

Documentation : [Outils .NET locaux](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)

### 6. Créer la première migration

```powershell
dotnet ef migrations add Initiale --project Chronos.Infrastructure --startup-project Chronos.Api
```

L'outil compile la solution, démarre `Chronos.Api` juste assez pour obtenir le contexte configuré, et écrit trois fichiers dans `Chronos.Infrastructure/Migrations/`. Sauf indication contraire, il exécute l'application dans l'environnement `Development` : il lit donc vos secrets utilisateur.

| Fichier | Rôle |
| --- | --- |
| `<horodatage>_Initiale.cs` | La migration : méthodes `Up` et `Down`. |
| `<horodatage>_Initiale.Designer.cs` | Ses métadonnées. On n'y touche pas. |
| `ChronosDbContextModelSnapshot.cs` | Le snapshot : l'état du modèle après cette migration. |

**Ouvrez la migration et lisez-la.** Vous y retrouvez votre configuration (`maxLength: 20`, `unique: true`, `onDelete: ReferentialAction.Restrict`). Vous y voyez aussi ce que vous n'avez pas encore configuré : la table `Collaborateurs`, découverte parce que `Projet` y fait référence, a des colonnes `text` et une colonne `Categorie` de type `integer`. L'exercice 1 s'en occupe.

Remarquez enfin les types : `character varying(20)`, `integer`, `NpgsqlValueGenerationStrategy`. Une migration est écrite **pour un SGBD**. Le [module 07](07-deux-sgbd-un-seul-code.md) en tirera les conséquences.

### 7. Lire le SQL, puis l'appliquer

```powershell
dotnet ef migrations script --project Chronos.Infrastructure --startup-project Chronos.Api
```

Affiche le SQL sans rien exécuter. Extrait :

```sql
CREATE TABLE "Projets" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "Code" character varying(20) NOT NULL,
    "Nature" character varying(20) NOT NULL,
    "BudgetJours" integer NOT NULL,
    "ResponsableId" integer NOT NULL,
    ...
    CONSTRAINT "FK_Projets_Collaborateurs_ResponsableId" FOREIGN KEY ("ResponsableId") REFERENCES "Collaborateurs" ("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_Projets_Code" ON "Projets" ("Code");
```

Appliquez la migration :

```powershell
dotnet ef database update --project Chronos.Infrastructure --startup-project Chronos.Api
```

La sortie se termine par `Done.` Juste avant, la première fois, une ligne `fail:` signale l'échec d'une requête sur `__EFMigrationsHistory` : la table n'existait pas encore, EF Core la crée dans la foulée. Ce n'est pas une erreur.

Vérifiez dans la base :

```powershell
docker compose exec postgres psql -U chronos -d chronos
```

puis, dans `psql`, `\dt` pour lister les tables, `\d "Projets"` pour décrire celle des projets, et `SELECT * FROM "__EFMigrationsHistory";`. Les guillemets doubles sont nécessaires : EF Core crée des noms avec majuscules, et PostgreSQL met en minuscules tout nom non entouré de guillemets.

Documentation : [Appliquer les migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

### 8. Alimenter la base de développement

Une base vide ne permet de rien essayer. EF Core appelle une fonction d'**alimentation** à la fin de `dotnet ef database update`. On la branche dans les options du contexte, sous deux formes — synchrone et asynchrone — parce que l'outil en ligne de commande appelle la première et `MigrateAsync` la seconde.

`Chronos.Infrastructure/DonneesInitiales.cs` (adaptez aux constructeurs et aux méthodes de vos entités) :

```csharp
using Chronos.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chronos.Infrastructure;

public static class DonneesInitiales
{
    public static void Alimenter(DbContext context)
    {
        if (context.Set<Collaborateur>().Any())
        {
            return;
        }

        var (responsable, client) = AjouterReferentiel(context);
        context.SaveChanges();

        context.AddRange(CreerProjets(responsable.Id, client.Id));
        context.SaveChanges();
    }

    public static async Task AlimenterAsync(DbContext context, CancellationToken cancellationToken)
    {
        if (await context.Set<Collaborateur>().AnyAsync(cancellationToken))
        {
            return;
        }

        var (responsable, client) = AjouterReferentiel(context);
        await context.SaveChangesAsync(cancellationToken);

        context.AddRange(CreerProjets(responsable.Id, client.Id));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static (Collaborateur Responsable, Client Client) AjouterReferentiel(DbContext context)
    {
        var sophie = new Collaborateur("M0001", "Martin", "Sophie", "sophie.martin@chronos.example",
            CategorieCollaborateur.Interne, "Directrice de projets", new DateOnly(2019, 9, 2));
        var client = new Client("Banque du Littoral");

        context.AddRange(sophie, client);
        return (sophie, client);
    }

    private static Projet[] CreerProjets(int responsableId, int clientId) =>
    [
        new("BDL-REFONTE", "Refonte du portail client", NatureProjet.Facturable, clientId,
            new DateOnly(2026, 3, 2), new DateOnly(2026, 12, 18), 240, responsableId),
        new("INT-FORMATION", "Formation interne", NatureProjet.Interne, null,
            new DateOnly(2026, 1, 5), null, 60, responsableId),
    ];
}
```

- La fonction commence par vérifier qu'il n'y a rien à faire : elle est rappelée **à chaque** `database update`, même sans nouvelle migration.
- Elle enregistre en deux temps : les identifiants de Sophie et du client sont générés par la base au premier `SaveChanges`, et les projets en ont besoin.

Dans `Program.cs`, l'enregistrement du contexte devient :

```csharp
builder.Services.AddDbContext<ChronosDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Chronos"));

    if (builder.Environment.IsDevelopment())
    {
        options
            .UseSeeding((context, _) => DonneesInitiales.Alimenter(context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                DonneesInitiales.AlimenterAsync(context, cancellationToken));
    }
});
```

Le deuxième paramètre des deux fonctions, ignoré ici, indique si l'opération vient de modifier le schéma. La condition sur l'environnement évite d'insérer des données de démonstration ailleurs que sur un poste de développement.

Relancez `dotnet ef database update` (étape 7), puis vérifiez dans `psql` : `SELECT "Code", "Nature", "Statut" FROM "Projets";` montre deux lignes, avec `Facturable` et `Interne` en toutes lettres.

Documentation : [Données initiales](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)

### 9. Servir `/api/projets` depuis la base

Le module 03 annonçait que le client et le responsable viendraient avec la base. Complétez les deux types de `Chronos.Api/Projets/ProjetDto.cs` :

```csharp
public record ProjetDto(
    int Id, string Code, string Libelle, NatureProjet Nature,
    int? ClientId, string? ClientNom,
    DateOnly DateDebut, DateOnly? DateFin, int BudgetJours,
    int ResponsableId, string ResponsableNom, StatutProjet Statut);

public record CreerProjetRequete(
    string Code, string Libelle, NatureProjet Nature, int? ClientId,
    DateOnly DateDebut, DateOnly? DateFin, int BudgetJours, int ResponsableId);
```

La liste devient paginée. Le format de page est commun à toute l'API ; créez `Chronos.Api/Commun/PageDto.cs` :

```csharp
namespace Chronos.Api.Commun;

public record PageDto<T>(IReadOnlyList<T> Elements, int Page, int Taille, int Total);
```

Supprimez `ProjetsEnMemoire.cs` et, dans `Program.cs`, la ligne `AddSingleton<ProjetsEnMemoire>()`. Dans `ProjetsEndpoints.cs`, retirez la route `DELETE /api/projets/{id}` et son gestionnaire (exercice 3 du module 03) : avec la base, un projet ne se supprime plus, il s'annule (`Annuler()`, module 04), et ses données restent. Les trois gestionnaires restants reçoivent maintenant le contexte par injection ; la liste garde le filtre `?statut=` du module 03 :

```csharp
private static readonly Expression<Func<Projet, ProjetDto>> VersDto = p => new ProjetDto(
    p.Id, p.Code, p.Libelle, p.Nature,
    p.ClientId, p.Client!.Nom,
    p.DateDebut, p.DateFin, p.BudgetJours,
    p.ResponsableId, p.Responsable!.Prenom + " " + p.Responsable.Nom, p.Statut);

private static async Task<Ok<PageDto<ProjetDto>>> Lister(
    ChronosDbContext db, CancellationToken cancellationToken,
    int page = 1, int taille = 20, StatutProjet? statut = null)
{
    IQueryable<Projet> requete = db.Projets;
    if (statut is not null)
    {
        requete = requete.Where(p => p.Statut == statut);
    }

    var total = await requete.CountAsync(cancellationToken);

    var elements = await requete
        .OrderBy(p => p.Code)
        .Skip((page - 1) * taille)
        .Take(taille)
        .Select(VersDto)
        .ToListAsync(cancellationToken);

    return TypedResults.Ok(new PageDto<ProjetDto>(elements, page, taille, total));
}

private static async Task<Results<Ok<ProjetDto>, NotFound>> Obtenir(
    int id, ChronosDbContext db, CancellationToken cancellationToken)
{
    var projet = await db.Projets
        .Where(p => p.Id == id)
        .Select(VersDto)
        .FirstOrDefaultAsync(cancellationToken);

    return projet is null ? TypedResults.NotFound() : TypedResults.Ok(projet);
}

private static async Task<Results<Created<ProjetDto>, Conflict>> Creer(
    CreerProjetRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
{
    if (await db.Projets.AnyAsync(p => p.Code == requete.Code, cancellationToken))
    {
        return TypedResults.Conflict();
    }

    var projet = new Projet(requete.Code, requete.Libelle, requete.Nature, requete.ClientId,
        requete.DateDebut, requete.DateFin, requete.BudgetJours, requete.ResponsableId);

    db.Projets.Add(projet);
    await db.SaveChangesAsync(cancellationToken);

    var dto = await db.Projets.Where(p => p.Id == projet.Id).Select(VersDto).SingleAsync(cancellationToken);
    return TypedResults.Created($"/api/projets/{dto.Id}", dto);
}
```

Les `using` à ajouter : `System.Linq.Expressions`, `Chronos.Api.Commun`, `Chronos.Domain`, `Chronos.Infrastructure`, `Microsoft.EntityFrameworkCore`.

- **`VersDto`** est la correspondance entité → DTO, écrite une fois, à la main. C'est une *expression* : EF Core ne l'exécute pas, il la **traduit en SQL**. `p.Client!.Nom` ne provoque donc aucune erreur quand le projet n'a pas de client : la jointure renvoie `NULL`. Les `!` ne servent qu'à dire au compilateur que ces navigations, déclarées `Client?` et `Collaborateur?`, peuvent être traversées ici.
- **Tout est asynchrone** (`ToListAsync`, `SaveChangesAsync`) et reçoit le `CancellationToken` de la requête HTTP : si le client se déconnecte, la requête SQL est abandonnée.
- **Le filtre s'applique avant `CountAsync`** : `total` compte les projets du statut demandé, pas tous les projets.
- **`OrderBy` avant `Skip`/`Take`** : sans ordre explicite, une base ne garantit aucun ordre, et deux pages pourraient contenir la même ligne. L'ordre doit porter sur une valeur unique ; le code l'est.
- **`AnyAsync` avant l'insertion** permet de répondre `409` proprement dans le cas courant. La vraie garantie reste l'index unique : entre le contrôle et l'insertion, une autre requête peut passer. Le module 08 traitera ce cas, ainsi que la validation de `page` et `taille`.
- `SaveChangesAsync` renseigne `projet.Id` avec la valeur générée par la base.

Démarrez l'API (`dotnet run --project Chronos.Api`), puis :

```powershell
curl.exe "http://localhost:5080/api/projets?page=1&taille=1"
```

La réponse contient un élément, avec `"clientNom"` et `"responsableNom"` renseignés, et `"total": 2`. Arrêtez l'API, relancez-la : les projets sont toujours là.

Documentation : [Pagination](https://learn.microsoft.com/en-us/ef/core/querying/pagination)

### 10. Lire le SQL exécuté

Regardez la console de l'API. En développement, EF Core y journalise chaque commande (catégorie `Microsoft.EntityFrameworkCore.Database.Command`, niveau `Information`). Pour la liste :

```sql
SELECT p0."Id", p0."Code", ..., c."Nom", ..., c0."Prenom" || ' ' || c0."Nom", p0."Statut"
FROM (
    SELECT p."Id", ... FROM "Projets" AS p
    ORDER BY p."Code"
    LIMIT @p1 OFFSET @p
) AS p0
LEFT JOIN "Clients" AS c ON p0."ClientId" = c."Id"
INNER JOIN "Collaborateurs" AS c0 ON p0."ResponsableId" = c0."Id"
ORDER BY p0."Code"
```

Une seule requête, deux jointures, et des `Collaborateurs` on ne lit que le nom et le prénom. Les valeurs des paramètres sont masquées (`@p='?'`) : elles peuvent contenir des données personnelles.

Pour mesurer ce que la projection évite, écrivez provisoirement trois variantes de la liste et comparez leur journal :

| Variante | Ce qu'on observe (2 projets en base) |
| --- | --- |
| `await db.Projets.ToListAsync()`, puis `p.Responsable?.Nom` | `Responsable` vaut `null` : la navigation n'est pas chargée. C'est pour cela que le module 04 l'a déclarée `Collaborateur?`. |
| La même, avec une requête sur `db.Collaborateurs` dans la boucle | 1 requête pour les projets, puis 1 par projet : le N+1. Avec 200 projets, 201 requêtes. |
| `db.Projets.Include(p => p.Responsable).Include(p => p.Client)` | 1 requête, mais **toutes** les colonnes des trois tables, et 4 entités suivies par le contexte. |

Avec la projection, le contexte ne suit aucune entité. Supprimez ces variantes une fois l'observation faite.

Ce journal est précieux en développement et bavard en production. Le niveau se règle par catégorie, dans la section `Logging:LogLevel` de `appsettings.json` :

```json
"Microsoft.EntityFrameworkCore.Database.Command": "Warning"
```

et, pour garder le journal en développement, dans celle d'`appsettings.Development.json` — sans cette seconde ligne, la valeur d'`appsettings.json` s'applique aussi en développement :

```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

Documentation : [Journalisation d'EF Core](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/extensions-logging)

### 11. Archiver sans supprimer (RG-05)

`Collaborateur` porte une propriété `EstArchive`. Créez `Configurations/CollaborateurConfiguration.cs` :

```csharp
public class CollaborateurConfiguration : IEntityTypeConfiguration<Collaborateur>
{
    public const string FiltreNonArchives = "NonArchives";

    public void Configure(EntityTypeBuilder<Collaborateur> builder)
    {
        builder.HasQueryFilter(FiltreNonArchives, c => !c.EstArchive);
    }
}
```

Désormais, toute requête sur `db.Collaborateurs` reçoit `WHERE NOT (c."EstArchive")`. Un filtre ne change pas le schéma : aucune migration n'est nécessaire.

Mais relancez une commande `dotnet ef` : un avertissement apparaît.

```text
Entity 'Collaborateur' has a global query filter defined and is the required end of a relationship with the entity 'Projet'. This may lead to unexpected results when the required entity is filtered out.
```

Il faut le prendre au sérieux. Passez `EstArchive` à `true` pour Sophie (dans `psql` : `UPDATE "Collaborateurs" SET "EstArchive" = true WHERE "Id" = 1;`), puis rappelez `/api/projets` : la liste est **vide**, alors que `total` vaut toujours 2, et `/api/projets/1` répond `404`. La jointure vers le responsable est une jointure interne, le filtre écarte Sophie, et ses projets disparaissent avec elle.

Or un projet dont le responsable est archivé existe toujours. Les requêtes sur les projets doivent donc ignorer ce filtre, et lui seul :

```csharp
IQueryable<Projet> requete = db.Projets
    .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives]);
// ...
```

Faites de même dans `Obtenir` et dans la relecture de `Creer`, puis vérifiez que les deux projets réapparaissent. Remettez ensuite `EstArchive` à `false`.

L'avertissement, lui, reste affiché à chaque commande `dotnet ef`, et donc dans le journal du workflow (exercice 5) : EF Core signale la configuration, il ne sait pas que vos requêtes la traitent. On le laisse visible, comme un rappel pour toute nouvelle requête qui traverse `Responsable`.

Le nom du filtre est une constante : une faute de frappe dans une chaîne ne serait signalée par personne.

### 12. Détecter un modèle sans migration

Modifier une configuration sans créer la migration correspondante est l'oubli le plus courant. Une commande le détecte :

```powershell
dotnet ef migrations has-pending-model-changes --project Chronos.Infrastructure --startup-project Chronos.Api
```

Elle répond `No changes have been made to the model since the last migration.` Changez une longueur dans `ProjetConfiguration` et relancez-la : elle répond `Changes have been made to the model since the last migration. Add a new migration.` et se termine avec un code de sortie différent de zéro — ce qui fait échouer un workflow. Annulez votre changement.

## À vous

1. **Configurer `Collaborateur` et `Contrat`.** Ajoutez à `Collaborateur` la navigation `Collaborateur? Manager`, à *setter* privé, comme vous l'avez fait pour le client d'un projet. Complétez `CollaborateurConfiguration` : longueurs, index uniques sur le matricule et l'adresse électronique (RG-01), catégorie enregistrée en chaîne, relation vers le manager (une relation d'une table vers elle-même, sans suppression en cascade), relation vers les contrats. Écrivez `ContratConfiguration` : type en chaîne, et quotité avec une précision adaptée à des valeurs de 0,10 à 1 (`HasPrecision` ; sans elle, un `decimal` reçoit un type par défaut qui dépend du SGBD). Créez la migration `ConfigurationCollaborateurs`, **lisez-la**, appliquez-la. Puis regardez dans `psql` la colonne `Categorie` des lignes qui existaient déjà. Que contient-elle ? L'API signale-t-elle un problème ? Essayez aussi de revenir en arrière avec `dotnet ef database update Initiale` et lisez l'erreur. Corrigez enfin les données, de la façon qui vous paraît la plus sûre — sur un poste de développement, `dotnet ef database drop` suivi de `database update` repart d'une base vide — et notez ce que vous auriez fait si la base avait été celle de production.

2. **Une règle pour toutes les énumérations.** Répéter `HasConversion<string>()` sur chaque propriété finira par être oublié une fois. La méthode `ConfigureConventions` du contexte permet de le dire une fois pour tous les types d'un genre donné ([configuration en masse](https://learn.microsoft.com/en-us/ef/core/modeling/bulk-configuration)). Mettez-la en place, retirez les conversions propriété par propriété, et prouvez avec la commande de l'étape 12 que le modèle n'a pas changé.

3. **Lister les collaborateurs.** Écrivez `GET /api/collaborateurs?page=&taille=`, dans `Collaborateurs/CollaborateursEndpoints.cs`, qui renvoie une page de `CollaborateurDto` : identité, catégorie, poste, nom du manager, et quotité du contrat en cours — celui qui n'a pas de date de fin, comme dans `AjouterContrat` ; `0` s'il n'y en a pas. Comme pour les projets, écrivez la correspondance une fois, dans un champ `VersDto` ; le module 08 la réutilise telle quelle (`.Select(VersDto)`). N'y lisez pas l'horloge (`DateTime.Now`) : la date du jour arrive au module 08, avec `TimeProvider`. Un collaborateur sans manager doit avoir un `managerNom` nul, pas `" "`. Le journal ne doit montrer que deux requêtes SQL, quel que soit le nombre de collaborateurs. Ajoutez Camille aux données initiales (contrat à 80 %, Sophie pour manager) pour avoir de quoi vérifier ; la fonction d'alimentation ne fait rien sur une base qui contient déjà des collaborateurs, il faudra donc repartir d'une base vide.

4. **Archiver.** Donnez à `Collaborateur` une méthode `Archiver()`, avec son test. Écrivez `DELETE /api/collaborateurs/{id}`, qui archive le collaborateur au lieu de le supprimer ; tant que les affectations, les saisies et les absences n'existent pas (modules 09 et 10), on archive dans tous les cas. Vérifiez ensuite quatre choses : il n'apparaît plus dans la liste ; il apparaît toujours comme responsable de ses projets ; un second `DELETE` répond `404` ; son matricule reste refusé pour un nouveau collaborateur. Archivez Sophie : que devient le `managerNom` de Camille, et pourquoi ? Ces deux derniers comportements sont-ils ceux que veut le métier ?

5. **Faire vérifier les migrations par l'intégration continue.** Ajoutez au workflow du module 05, après la compilation, une étape qui restaure les outils (`dotnet tool restore`) et une étape qui exécute la commande de l'étape 12. Le workflow compile en `Release` et ne doit pas recompiler : cherchez les options utiles dans la [référence de `dotnet ef`](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) — attention, pour `dotnet ef`, `-c` abrège `--context`, pas `--configuration`. L'intégration continue n'a ni secrets utilisateur ni base : la commande en a-t-elle besoin ? Provoquez un échec pour vérifier que l'étape joue son rôle.

6. **Compter sans charger.** Branchez `/api/clients` sur la base : supprimez `ClientsEnMemoire.cs` et sa ligne `AddSingleton` dans `Program.cs`, et réécrivez les trois gestionnaires du module 03 (liste, lecture, création) avec le contexte, comme pour les projets. `GET /api/clients` renvoie chaque client avec le nombre de ses projets en cours (`ClientDto` gagne ce nombre). Une seule requête SQL, aucun `Include`. Lisez le SQL produit : comment EF Core a-t-il traduit le comptage ?

## Critères de réussite

- `dotnet ef database update` se termine par `Done.` sur une base vide, et la base contient les tables, les index uniques et les données initiales.
- `dotnet ef migrations has-pending-model-changes` ne signale aucun changement, sur votre poste et dans le workflow.
- `GET /api/projets` renvoie `{ "elements": [...], "page": 1, "taille": 20, "total": ... }` ; les données survivent au redémarrage de l'API.
- Créer deux projets de même code renvoie `409` ; dans `psql`, l'insertion directe d'un doublon est refusée par l'index.
- Chaque endpoint de liste produit un nombre de requêtes SQL indépendant du nombre de lignes.
- Un collaborateur archivé disparaît de `/api/collaborateurs` et reste visible comme responsable dans `/api/projets`.
- Ni `appsettings.json` ni aucun fichier versionné ne contient de mot de passe ; `dotnet-tools.json`, les migrations et les fichiers `packages.lock.json` mis à jour sont versionnés.
- `Chronos.Domain` ne référence toujours aucun paquet.
- Comparez avec la solution de référence : `git diff module-05 module-06`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `dotnet package add` ou `dotnet tool install` sans `--version` | Installe une version 11.x, qui exige .NET 11. | Préciser une version 10.x ; la retrouver dans `Directory.Packages.props`. |
| Chaîne de connexion dans `appsettings.json` | Le mot de passe part dans le dépôt. | `dotnet user-secrets` en développement. |
| `context.Database.EnsureCreated()` au démarrage | Crée le schéma sans migrations ; les migrations échouent ensuite. | `dotnet ef database update`. |
| `HasData` pour toutes les données initiales | Les données entrent dans les migrations et le snapshot, avec des clés fixées à la main. | `UseSeeding` **et** `UseAsyncSeeding` ; `HasData` seulement pour des données fixes. |
| Appliquer une migration sans l'avoir lue | Un renommage devient une suppression suivie d'une création ; un changement de type peut laisser des données incohérentes (exercice 1). | Lire `Up`, lire le SQL avec `migrations script`. |
| Modifier une migration déjà appliquée ailleurs | Les bases n'ont plus le même historique. | Créer une nouvelle migration. |
| `ToListAsync()` puis `Where`, `Select` ou `Count` en mémoire | Toute la table est chargée avant d'être filtrée. | Filtrer et projeter **avant** `ToListAsync`. |
| `Include` partout « au cas où » | Colonnes et tables inutiles, entités suivies pour rien. | Projection vers un DTO. |
| Une requête dans une boucle | N+1 requêtes. | Une requête avec jointure ou projection. |
| `Skip`/`Take` sans `OrderBy` | L'ordre des lignes n'est pas garanti : pages incohérentes. | Trier sur une valeur unique. |
| `.Result` ou `.Wait()` sur une requête | Bloque un *thread* du serveur. | `await` et les méthodes `...Async`. |
| Un `DbContext` conservé dans un champ statique ou un *singleton* | Le contexte n'est pas prévu pour un usage simultané et accumule les entités suivies. | Le recevoir par injection dans chaque gestionnaire. |
| Un *repository* générique par-dessus `DbSet` | `DbSet` en est déjà un ; la couche masque `Select`, `Include` et les filtres. | Injecter `ChronosDbContext`. |
| Un filtre de requête sur une entité visée par une relation obligatoire | Les entités qui y font référence disparaissent des jointures (étape 11). | Nommer le filtre et l'ignorer là où le métier l'exige. |
| `EnableSensitiveDataLogging()` laissé actif | Les valeurs — noms, adresses — partent dans les journaux. | Le réserver à une session de débogage locale. |
| Chercher `.config/dotnet-ef.json` pour éviter de répéter `--project` | Ce fichier n'existe qu'à partir d'EF Core 11. | Répéter les options, ou écrire un script. |

Les sources sont dans le [référentiel .NET 10](../docs/references/dotnet-10.md#accès-aux-données-1).

## Pour la séance

- Le domaine ne référence pas EF Core, mais EF Core impose-t-il quand même quelque chose à vos entités ? Qu'avez-vous dû adapter, et qu'en pensez-vous ?
- RG-10 est vérifiée deux fois : par `AnyAsync` et par l'index unique. Laquelle des deux pourrait-on retirer ? Pourquoi garder les deux ?
- Énumérations en chaînes ou en entiers : quels arguments feraient pencher pour l'autre choix dans une base de plusieurs centaines de millions de lignes ?
- Le cours injecte `ChronosDbContext` directement dans les endpoints, sans couche intermédiaire. Qu'y gagne-t-on, qu'y perd-on ? À partir de quand reviendriez-vous sur ce choix ?
- À l'exercice 1, la migration s'est appliquée sans erreur et les données étaient pourtant fausses. Qu'est-ce qui, dans votre façon de travailler, aurait permis de le voir avant la production ?
- Un filtre de requête rend l'archivage invisible pour le développeur qui écrit une requête. Est-ce une sécurité ou un danger ?

## Pour aller plus loin

- [Types complexes](https://learn.microsoft.com/en-us/ef/core/modeling/complex-types) : faire d'un objet valeur comme `Periode` deux colonnes de la table qui le contient (`Periode_Debut`, `Periode_Fin`), sans table ni clé à part. Depuis EF Core 10, une structure convient, et la documentation recommande les types complexes plutôt que les anciens types possédés (`OwnsOne`). Les quatre entités de ce module exposent leurs dates séparément, avec une fin facultative : il n'y a rien à mapper pour l'instant. Si vous essayez sur une entité de votre cru, notez que `Debut` et `Fin` n'ont pas de *setter* : `ComplexProperty(x => x.Periode)` seul échoue (`No suitable constructor was found`), il faut déclarer les deux propriétés — `ComplexProperty(x => x.Periode, p => { p.Property(v => v.Debut); p.Property(v => v.Fin); })`. Essai fait avec la `Periode` du module 04 : écriture, relecture et filtre sur les bornes fonctionnent.
- [Requêtes uniques ou fractionnées](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries) : ce qui se passe quand on charge plusieurs collections avec `Include`.
- [Gérer les migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing) : renommer une colonne sans perdre ses données, retirer une migration, écrire du SQL dans une migration.
- [Nouveautés d'EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew) : filtres nommés, types complexes, `ExecuteUpdateAsync`.
- Suite dans le cours : le [module 07](07-deux-sgbd-un-seul-code.md) fait tourner la même application sur SQL Server, le [module 08](08-une-api-robuste.md) traite les erreurs de la base (`409`), la validation et la concurrence, le module 11 teste tout cela contre une vraie base, le module 25 applique les migrations en déploiement.
