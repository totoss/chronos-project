# Module 03 — Première API

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 10](../docs/references/dotnet-10.md).
> Les commandes `dotnet` et le code de ce module ont été **exécutés le 12 novembre 2026** dans un conteneur Linux `mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.404, ASP.NET Core 10.0.15, `Microsoft.AspNetCore.OpenApi` 10.0.15, `Scalar.AspNetCore` 2.21.0) : création de la solution, compilation, exécution, appels HTTP, page Scalar ouverte dans un navigateur. Module **rejoué le 12 novembre 2026 sur la solution de référence** (mêmes versions), exercices 1 à 4 compris. Les appels `curl.exe` et `Invoke-RestMethod` ont été lancés depuis Windows PowerShell 5.1. **Non exécutés** : les commandes `dotnet` sous Windows même, et l'envoi des requêtes du fichier `.http` depuis un éditeur. Signalez au mentor tout écart constaté.

Chronos a deux moitiés : une interface, que vous écrirez en Angular à partir du module 13, et une **API** qui détient les données et les règles. L'interface ne touche jamais la base : elle demande à l'API « donne-moi les projets », « crée ce projet », et l'API répond en JSON.

Ce module construit le squelette de cette API et sa première ressource, les **projets**. Il n'y a encore ni base de données ni règle de gestion : les projets vivent dans une liste en mémoire. L'objectif est de voir une requête HTTP entrer, traverser votre code et ressortir.

## Objectifs

À la fin de ce module, vous saurez :

- créer une solution .NET et un projet d'API, et dire à quoi sert chaque fichier généré ;
- fixer la version du SDK et celle des paquets NuGet pour toute la solution ;
- écrire des endpoints en Minimal API et les regrouper par ressource ;
- typer les réponses d'un endpoint et choisir le bon code HTTP (200, 201, 404) ;
- explorer et essayer l'API avec le document OpenAPI et l'interface Scalar.

## Prérequis

- Module 00 : le SDK .NET 10 est installé. `dotnet --list-sdks` doit afficher une ligne `10.0.4xx` ou supérieure.
- Module 02 : vous savez ce qu'est un projet dans Chronos, sa nature et son statut.
- Notions de HTTP : verbes `GET` et `POST`, codes de statut, corps JSON.
- Votre dépôt ignore les dossiers `bin/` et `obj/`. C'est le cas du `.gitignore` du dépôt du cours ; dans un dépôt neuf, `dotnet new gitignore` en crée un.

## Règles de gestion

Aucune. L'API de ce module accepte n'importe quel projet, même incohérent : c'est voulu. Les règles arrivent au [module 04](04-modeliser-le-domaine.md), la validation des entrées au [module 08](08-une-api-robuste.md).

## Notions

### Solution, projet, SDK

Un **projet** (`.csproj`) produit un assemblage : ici, l'exécutable de l'API. Une **solution** (`.slnx`) est la liste des projets qui vont ensemble ; `dotnet build` lancé à côté d'elle les compile tous. Chronos en comptera cinq ou six.

Le **SDK** est l'outil qui compile. Plusieurs versions peuvent être installées côte à côte, et sans consigne `dotnet` prend la plus récente. Or .NET 11 est sorti le 10 novembre 2026 : sur un poste à jour, ce serait lui. Le fichier `global.json` donne la consigne : cette solution se compile avec le SDK 10.

Documentation : [global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json), [format `.slnx`](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default)

### Minimal API : une route, un gestionnaire

Un **endpoint** associe un verbe HTTP et une route à une fonction, le **gestionnaire** (*route handler*) :

```csharp
app.MapGet("/api/projets", () => "la liste des projets");
```

C'est tout ce qu'il faut pour répondre à `GET /api/projets`. Ce style s'appelle **Minimal API**. La documentation le recommande pour tout nouveau projet.

Vous croiserez dans du code existant l'autre style, les **contrôleurs** : des classes qui héritent de `ControllerBase`, dont les méthodes portent des attributs `[HttpGet]`, `[Route]`. Ils sont toujours pris en charge et font le même travail ; le cours ne les enseigne pas. Savoir les reconnaître suffit.

Documentation : [Vue d'ensemble des API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-10.0), [Gestionnaires de routes](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers?view=aspnetcore-10.0)

### D'où viennent les paramètres d'un gestionnaire

Vous ne lisez jamais la requête « à la main ». Vous déclarez des paramètres, et ASP.NET Core les remplit d'après leur type et leur nom :

| Paramètre | Rempli à partir de |
| --- | --- |
| `int id`, avec une route `/{id}` | l'URL : `/api/projets/3` |
| un type simple absent de la route (`string recherche`) | la chaîne de requête : `?recherche=acme` |
| un objet (`CreerProjetRequete requete`) | le corps JSON de la requête |
| un type enregistré comme **service** (`ProjetsEnMemoire projets`) | le conteneur d'injection de dépendances |

Le dernier cas mérite un mot. Un gestionnaire est appelé à chaque requête ; ce dont il a besoin et qui doit vivre plus longtemps — aujourd'hui la liste des projets, demain l'accès à la base — est un **service**. On l'enregistre une fois au démarrage (`builder.Services.Add…`), et ASP.NET Core le fournit à qui le demande. C'est l'**injection de dépendances** : le gestionnaire dit ce qu'il lui faut, pas comment le fabriquer.

Documentation : [Liaison des paramètres](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0), [Injection de dépendances](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0)

### DTO et `record`

Ce que l'API envoie et reçoit est un **DTO** (*data transfer object*) : un objet sans comportement, qui décrit la forme du JSON. En C#, on l'écrit avec un `record` : une ligne déclare les propriétés, et l'objet ne peut plus être modifié une fois créé.

`ProjetDto` est ce que l'API renvoie ; `CreerProjetRequete` est ce qu'elle reçoit pour une création. Ce sont deux types distincts : le client ne choisit ni l'identifiant ni le statut d'un nouveau projet.

Documentation : [Records](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records)

### `TypedResults` : la réponse fait partie de la signature

Un gestionnaire peut renvoyer directement un objet : ASP.NET Core répond 200 avec son JSON. Dès qu'il y a plusieurs issues possibles — trouvé ou introuvable —, on renvoie un **résultat typé** : `TypedResults.Ok(projet)`, `TypedResults.NotFound()`, `TypedResults.Created(...)`.

Le type de retour les énumère : `Results<Ok<ProjetDto>, NotFound>` se lit « soit 200 avec un projet, soit 404 ». Deux bénéfices :

- le compilateur refuse un `return` qui ne figure pas dans la liste ;
- le document OpenAPI décrit ces deux réponses sans une ligne de plus.

Documentation : [Réponses](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0)

### OpenAPI et Scalar

**OpenAPI** est un format standard qui décrit une API : ses routes, leurs paramètres, la forme des réponses. ASP.NET Core génère ce document à partir de votre code, à l'adresse `/openapi/v1.json`. Quiconque écrit un client de l'API — vous-même, à partir du module 16 — s'en sert comme contrat.

Ce document est du JSON, peu lisible tel quel. **Scalar** est une interface web qui le présente et permet d'envoyer des requêtes d'essai. Les deux ne sont exposés qu'en développement : en production, ils renseigneraient un attaquant sur la structure de l'API.

Documentation : [OpenAPI dans ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0), [Utiliser le document généré](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0)

## Pas à pas

Toutes les commandes se lancent depuis `src/api`, sauf mention contraire.

### 1. Fixer la version du SDK

À la racine de votre dépôt :

```powershell
mkdir src/api
```

Placez-vous dans ce dossier (`cd src/api`), puis :

```powershell
dotnet new globaljson --sdk-version 10.0.400 --roll-forward latestFeature
```

Le fichier `global.json` créé contient :

```json
{
  "sdk": {
    "rollForward": "latestFeature",
    "version": "10.0.400"
  }
}
```

- **`version`** est un minimum : il faut au moins le SDK 10.0.400.
- **`rollForward: latestFeature`** autorise toute version 10.0 plus récente installée sur le poste, et aucune autre : ni la 9, ni la 11.

```powershell
dotnet --version
```

Dans ce dossier, la commande doit maintenant afficher `10.0.4xx`, même si le SDK 11 est installé. Si elle affiche une erreur, aucun SDK 10 assez récent n'est installé : retournez au module 00.

### 2. Créer la solution et le projet

```powershell
dotnet new sln --name Chronos
```

Crée `Chronos.slnx`, une solution vide. Avant .NET 10, la commande créait un fichier `.sln` ; ce format fonctionne toujours, mais le `.slnx`, un XML de quelques lignes, est le nouveau défaut.

```powershell
dotnet new webapi --name Chronos.Api --no-https
```

Crée le projet d'API dans le dossier `Chronos.Api`.

`--no-https` retire du modèle la redirection vers HTTPS et le profil de lancement correspondant. Ce n'est pas un renoncement à la sécurité : en production, Chronos tournera dans un conteneur, derrière un serveur frontal qui portera le HTTPS ([module 23](23-conteneuriser-l-application.md)). La documentation prévoit ce cas : quand un serveur frontal gère le HTTPS, l'application n'a pas à le faire.

```powershell
dotnet sln add Chronos.Api
```

Inscrit le projet dans la solution.

Documentation : [Modèles de `dotnet new`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates), [Imposer HTTPS](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0)

### 3. Lire ce que le modèle a généré

Ouvrez chacun des six fichiers du dossier `Chronos.Api`.

| Fichier | Rôle |
| --- | --- |
| `Chronos.Api.csproj` | Le projet. `Sdk="Microsoft.NET.Sdk.Web"` en fait une application web ; `net10.0` est la version de .NET ciblée ; une seule dépendance, `Microsoft.AspNetCore.OpenApi`. |
| `Program.cs` | Le point d'entrée, et pour l'instant tout le code. |
| `Properties/launchSettings.json` | Comment `dotnet run` lance l'application **sur votre poste** : l'adresse d'écoute et l'environnement `Development`. Ce fichier ne sert pas en production. |
| `appsettings.json` | La configuration de l'application ; pour l'instant, le niveau des journaux. |
| `appsettings.Development.json` | Ce qui s'y ajoute ou le remplace quand l'environnement est `Development`. |
| `Chronos.Api.http` | Des requêtes HTTP prêtes à envoyer depuis l'éditeur (étape 12). |

`Program.cs` se lit en deux temps, séparés par la ligne `var app = builder.Build();` :

- **avant**, on enregistre les services dont l'application a besoin : `builder.Services.AddOpenApi()` ;
- **après**, on décrit comment traiter les requêtes : `app.MapOpenApi()`, `app.MapGet(...)`, puis `app.Run()` démarre le serveur.

Il n'y a ni classe `Program`, ni méthode `Main`, ni fichier `Startup.cs` : le fichier est exécuté de haut en bas. Le modèle ajoute un endpoint d'exemple, `/weatherforecast`, que vous supprimerez à l'étape 6.

### 4. Fixer le port et lancer l'API

Le modèle tire un port au hasard. Tout le cours suppose que l'API écoute sur **`http://localhost:5080`**. Dans `Properties/launchSettings.json`, donnez cette valeur à `applicationUrl`, et faites de même sur la première ligne de `Chronos.Api.http`. Puis :

```powershell
dotnet run --project Chronos.Api
```

La commande compile, puis affiche `Now listening on: http://localhost:5080` et `Hosting environment: Development`. Elle ne rend pas la main : l'API tourne. Dans un second terminal :

```powershell
curl.exe http://localhost:5080/weatherforecast
```

Vous recevez un tableau JSON de prévisions météo. Écrivez bien `curl.exe` : dans Windows PowerShell, `curl` tout court désigne une autre commande.

Arrêtez l'API avec `Ctrl+C`. Vous devrez l'arrêter et la relancer après chaque modification du code.

### 5. Centraliser les réglages et les versions

Le `.csproj` généré porte trois réglages et un numéro de version. Avec cinq projets, il faudrait les tenir identiques dans cinq fichiers. Deux fichiers placés à côté de la solution s'appliquent d'office à tous les projets des sous-dossiers.

`Directory.Build.props`, pour les réglages de compilation :

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

`Directory.Packages.props`, pour les versions des paquets :

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.15" />
  </ItemGroup>
</Project>
```

Reprenez le numéro de version qui figure dans votre `.csproj`. Celui-ci se réduit alors à :

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
  </ItemGroup>

</Project>
```

Un projet dit **quels** paquets il utilise (`PackageReference`, sans version) ; `Directory.Packages.props` dit **dans quelle version** (`PackageVersion`). C'est la gestion centralisée des versions.

```powershell
dotnet build
```

doit se terminer par `0 Warning(s)` et `0 Error(s)`.

> **Le piège des versions 11.** Sans numéro de version, `dotnet package add` installe la dernière version publiée. Pour les paquets `Microsoft.AspNetCore.*` et `Microsoft.EntityFrameworkCore.*`, c'est désormais une 11.x, faite pour .NET 11. Essai du 12 novembre 2026 : `dotnet package add Microsoft.AspNetCore.OpenApi` a installé la 11.0.0 dans un projet `net10.0` sans le moindre avertissement, et la compilation a ensuite échoué (`CS1061: 'IServiceCollection' does not contain a definition for 'AddOpenApi'`). Pour ces paquets, précisez toujours la version : `dotnet package add <paquet> --version 10.0.15 --project Chronos.Api`.

Documentation : [Gestion centralisée des paquets](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management), [Personnaliser la compilation par dossier](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory)

### 6. Décrire un projet : le DTO

Dans `Program.cs`, supprimez tout ce qui concerne la météo : le tableau `summaries`, l'appel `app.MapGet("/weatherforecast", ...)` et le `record WeatherForecast` en bas du fichier.

Le code d'une ressource est rangé dans un dossier à son nom. Créez `Chronos.Api/Projets/ProjetDto.cs` :

```csharp
namespace Chronos.Api.Projets;

public enum NatureProjet { Facturable, Interne }

public enum StatutProjet { EnPreparation, EnCours, Termine, Annule }

public record ProjetDto(
    int Id,
    string Code,
    string Libelle,
    NatureProjet Nature,
    DateOnly DateDebut,
    DateOnly? DateFin,
    int BudgetJours,
    StatutProjet Statut);

public record CreerProjetRequete(
    string Code,
    string Libelle,
    NatureProjet Nature,
    DateOnly DateDebut,
    DateOnly? DateFin,
    int BudgetJours);
```

- Les dates sont des `DateOnly` : un jour, sans heure ni fuseau horaire. `DateOnly?` signale que la date de fin peut manquer.
- Le client et le responsable du projet viendront avec la base de données.
- Les deux énumérations vivent ici provisoirement. Au module 04, elles rejoindront le domaine.

### 7. Une liste en mémoire, fournie par injection

Créez `Chronos.Api/Projets/ProjetsEnMemoire.cs` :

```csharp
namespace Chronos.Api.Projets;

public class ProjetsEnMemoire
{
    private readonly Lock _verrou = new();
    private readonly List<ProjetDto> _projets =
    [
        new(1, "CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
            new DateOnly(2026, 1, 5), null, 120, StatutProjet.EnCours),
        new(2, "ACME-MIGR", "Migration du SI commercial", NatureProjet.Facturable,
            new DateOnly(2026, 3, 2), new DateOnly(2026, 9, 30), 300, StatutProjet.EnPreparation),
    ];
    private int _prochainId = 3;

    public IReadOnlyList<ProjetDto> Lister()
    {
        lock (_verrou)
        {
            return [.. _projets];
        }
    }

    public ProjetDto? Trouver(int id)
    {
        lock (_verrou)
        {
            return _projets.Find(p => p.Id == id);
        }
    }

    public ProjetDto Ajouter(CreerProjetRequete requete)
    {
        lock (_verrou)
        {
            var projet = new ProjetDto(_prochainId++, requete.Code, requete.Libelle, requete.Nature,
                requete.DateDebut, requete.DateFin, requete.BudgetJours, StatutProjet.EnPreparation);
            _projets.Add(projet);
            return projet;
        }
    }
}
```

Cette classe tient lieu de base de données jusqu'au [module 06](06-persister-avec-ef-core.md). Deux points à comprendre, le reste est jetable :

- **Le verrou.** Le serveur traite plusieurs requêtes en même temps, et `List<T>` ne supporte pas d'être modifiée par deux fils d'exécution à la fois. `lock` ne laisse entrer qu'un fil à la fois dans les blocs qu'il protège. `Lister` renvoie une copie de la liste (`[.. _projets]`), que l'appelant peut parcourir sans verrou.
- **`ProjetDto?`** : `Trouver` peut ne rien trouver, et son type de retour le dit. L'appelant sera obligé de traiter le cas.

Documentation : [Instruction `lock`](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)

Dans `Program.cs`, enregistrez la classe comme service, sous la ligne `AddOpenApi()` :

```csharp
builder.Services.AddSingleton<ProjetsEnMemoire>();
```

`AddSingleton` crée **une seule** instance pour toute la vie de l'application : c'est ce qu'il faut pour que la liste survive d'une requête à l'autre.

### 8. Les endpoints de lecture

Créez `Chronos.Api/Projets/ProjetsEndpoints.cs` :

```csharp
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chronos.Api.Projets;

public static class ProjetsEndpoints
{
    public static RouteGroupBuilder MapProjets(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/projets").WithTags("Projets");

        groupe.MapGet("/", Lister);
        groupe.MapGet("/{id:int}", Obtenir);

        return groupe;
    }

    private static Ok<IReadOnlyList<ProjetDto>> Lister(ProjetsEnMemoire projets) =>
        TypedResults.Ok(projets.Lister());

    private static Results<Ok<ProjetDto>, NotFound> Obtenir(int id, ProjetsEnMemoire projets) =>
        projets.Trouver(id) is { } projet
            ? TypedResults.Ok(projet)
            : TypedResults.NotFound();
}
```

- **`MapGroup("/api/projets")`** crée un groupe : toutes les routes déclarées dessus commencent par ce préfixe. `WithTags` les range sous un même titre dans la documentation.
- **`MapProjets`** est une méthode d'extension : elle s'appellera comme si elle faisait partie d'ASP.NET Core. Chaque ressource aura la sienne, dans son dossier.
- **Les gestionnaires** sont ici des méthodes nommées plutôt que des lambdas : leur signature se lit mieux. `Lister` reçoit le service enregistré à l'étape 7 ; `Obtenir` reçoit en plus l'`id` de la route.
- **`{id:int}`** est une contrainte de route : la route ne correspond que si le segment est un entier.
- **`is { } projet`** se lit « si le résultat n'est pas nul, nomme-le `projet` ».

`Program.cs` tient maintenant en quelques lignes :

```csharp
using Chronos.Api.Projets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<ProjetsEnMemoire>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapProjets();

app.Run();
```

Relancez l'API, puis :

```powershell
curl.exe -i http://localhost:5080/api/projets/1
```

L'option `-i` affiche aussi la ligne de statut et les en-têtes. Vous devez obtenir `HTTP/1.1 200 OK` et le projet `CHRONOS`. Essayez ensuite `/api/projets/99` (404), `/api/projets/abc` (404 aussi : la contrainte `:int` écarte la route) et `/api/projets` (la liste).

### 9. L'endpoint de création

Dans `MapProjets`, ajoutez la route :

```csharp
        groupe.MapPost("/", Creer);
```

et, dans la classe, son gestionnaire :

```csharp
    private static Created<ProjetDto> Creer(CreerProjetRequete requete, ProjetsEnMemoire projets)
    {
        var projet = projets.Ajouter(requete);
        return TypedResults.Created($"/api/projets/{projet.Id}", projet);
    }
```

`requete` est lu dans le corps JSON de la requête. La réponse est un **201 Created** : elle contient le projet créé et un en-tête `Location` qui donne son adresse. Un `POST` qui crée une ressource répond 201, pas 200.

Relancez l'API. Sous PowerShell, le plus simple pour envoyer du JSON est `Invoke-RestMethod` :

```powershell
$corps = '{ "code": "FORM-NET", "libelle": "Formation .NET 10", "nature": 1, "dateDebut": "2026-11-16", "dateFin": null, "budgetJours": 15 }'
```

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5080/api/projets -ContentType 'application/json' -Body $corps
```

La réponse affiche le projet avec `id : 3`. Vérifiez avec `curl.exe http://localhost:5080/api/projets/3`, puis arrêtez et relancez l'API : le projet 3 a disparu. La mémoire n'est pas une base de données.

### 10. Des énumérations lisibles

Regardez le JSON de l'étape 8 : `"nature":1,"statut":1`. Par défaut, une énumération est sérialisée par son rang. C'est illisible pour qui consomme l'API, et fragile : insérer une valeur au milieu de l'énumération changerait le sens des données déjà échangées.

Dans `Program.cs`, sous `AddOpenApi()` :

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

avec, en tête de fichier, `using System.Text.Json.Serialization;`.

Relancez et rappelez `/api/projets/1` :

```json
{"id":1,"code":"CHRONOS","libelle":"Refonte du suivi des temps","nature":"Interne","dateDebut":"2026-01-05","dateFin":null,"budgetJours":120,"statut":"EnCours"}
```

Les énumérations sont maintenant des chaînes, en lecture comme en écriture : un `POST` s'écrit avec `"nature": "Interne"`. Notez au passage les deux autres conventions du JSON de Chronos, que vous n'avez pas eu à régler : les noms de propriétés en *camelCase* et les dates au format `2026-01-05`.

Documentation : [`JsonStringEnumConverter`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonstringenumconverter)

### 11. Explorer l'API avec Scalar

```powershell
dotnet package add Scalar.AspNetCore --project Chronos.Api
```

Scalar n'est pas un paquet Microsoft et ne suit pas les numéros de .NET : le piège des versions 11 ne le concerne pas. La commande ajoute la référence au `.csproj` et, parce que la gestion centralisée est active, écrit la version dans `Directory.Packages.props`. Ouvrez les deux fichiers pour le constater.

Dans `Program.cs`, ajoutez `using Scalar.AspNetCore;` en tête de fichier et, dans le bloc réservé au développement, la ligne `app.MapScalarApiReference();` sous `app.MapOpenApi();`.

Relancez l'API et ouvrez dans un navigateur :

- `http://localhost:5080/openapi/v1.json` — le document OpenAPI. Cherchez-y `/api/projets/{id}` : les réponses 200 et 404 y figurent, déduites du type `Results<Ok<ProjetDto>, NotFound>`. Cherchez `NatureProjet` : ses deux valeurs y sont, en chaînes.
- `http://localhost:5080/scalar` — la même information, lisible. Le groupe **Projets** liste vos trois endpoints ; le bouton **Test Request** envoie une requête sans quitter la page.

Documentation : [Scalar pour ASP.NET Core](https://github.com/scalar/scalar/blob/main/documentation/integrations/aspnetcore/integration.md)

### 12. Garder ses requêtes dans le fichier `.http`

Scalar est pratique pour explorer. Pour rejouer toujours les mêmes essais, remplacez le contenu de `Chronos.Api/Chronos.Api.http` :

```http
@Chronos.Api_HostAddress = http://localhost:5080

GET {{Chronos.Api_HostAddress}}/api/projets/1
Accept: application/json

###

POST {{Chronos.Api_HostAddress}}/api/projets
Content-Type: application/json

{
  "code": "FORM-NET",
  "libelle": "Formation .NET 10",
  "nature": "Interne",
  "dateDebut": "2026-11-16",
  "dateFin": null,
  "budgetJours": 15
}
```

`###` sépare les requêtes ; la première ligne définit une variable. Visual Studio affiche un lien *Send Request* au-dessus de chaque requête ; Visual Studio Code fait de même avec l'extension REST Client, dont ce format est issu. Le fichier est versionné : c'est une documentation exécutable de l'API.

Documentation : [Fichiers `.http`](https://learn.microsoft.com/en-us/aspnet/core/test/http-files?view=aspnetcore-10.0)

## À vous

1. **Filtrer par statut.** Faites accepter à la liste un paramètre facultatif : `GET /api/projets?statut=EnCours` ne renvoie que les projets en cours, `GET /api/projets` les renvoie tous. Il suffit d'un paramètre de plus au gestionnaire : lequel, et de quel type ? Essayez ensuite `?statut=encours` et `?statut=Bidon` : quel code recevez-vous ? Est-ce le bon ?

2. **Refuser les rangs.** Envoyez un `POST` avec `"nature": 1`. Est-il accepté ? Trouvez dans la page de référence de `JsonStringEnumConverter` le paramètre de constructeur qui l'interdit, appliquez-le, et vérifiez le code de statut obtenu.

3. **Supprimer.** Ajoutez `DELETE /api/projets/{id}`. Quel code renvoyer quand la suppression réussit, et quand le projet n'existe pas ? Écrivez le type de retour avec `Results<…>` avant d'écrire le corps.

4. **Une deuxième ressource.** Créez le dossier `Clients` : un `ClientDto` (identifiant et nom), une liste en mémoire, une méthode `MapClients` avec le groupe `/api/clients`, les endpoints de liste, de lecture et de création. `Program.cs` ne doit gagner que deux instructions (plus une directive `using`). Vérifiez que Scalar affiche deux groupes.

5. **Changer d'environnement.** Lancez l'API avec `dotnet run --project Chronos.Api --no-launch-profile --urls http://localhost:5080`. Quel environnement le journal de démarrage annonce-t-il, et pourquoi ? Que répondent maintenant `/scalar` et `/openapi/v1.json` ? Envoyez un `POST` au JSON invalide dans les deux environnements et comparez les réponses : laquelle voudriez-vous montrer à un inconnu ?

6. **Lire un contrôleur.** La page [Vue d'ensemble des API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/apis?view=aspnetcore-10.0) montre la même API écrite avec des contrôleurs. Sans l'exécuter, retrouvez-y l'équivalent de chaque élément de votre code : le préfixe de route, le gestionnaire, le paramètre lu dans la route, le service injecté, le résultat 404.

## Critères de réussite

- Dans `src/api`, `dotnet --version` affiche une version 10.0, et `dotnet build` se termine sans avertissement ni erreur.
- `GET /api/projets` répond 200 avec une liste ; `GET /api/projets/1` répond 200 ; `GET /api/projets/99` répond 404.
- `POST /api/projets` répond 201 avec un en-tête `Location`, et un `GET` sur cette adresse renvoie le projet créé.
- Dans le JSON, `nature` et `statut` sont des chaînes.
- `http://localhost:5080/scalar` affiche les endpoints, et la réponse 404 de `GET /api/projets/{id}` y est documentée.
- Aucun `.csproj` ne contient de numéro de version ni de `TargetFramework`.
- `Program.cs` ne contient aucun gestionnaire : il enregistre des services et appelle des méthodes `Map…`.
- Comparez votre travail à la solution de référence : `git diff module-02 module-03`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `dotnet package add Microsoft.AspNetCore.…` sans version | Installe une 11.x dans un projet `net10.0` : la compilation casse, avec une erreur sans rapport apparent. | Ajouter `--version 10.0.x`. |
| `Version="…"` dans un `.csproj` alors que `Directory.Packages.props` existe | La restauration échoue avec l'erreur `NU1008`. | La version va dans `Directory.Packages.props`, sur un élément `PackageVersion`. |
| `AddSwaggerGen()`, `UseSwagger()`, `UseSwaggerUI()` | Swashbuckle n'est plus fourni par les modèles depuis .NET 9. | `AddOpenApi()`, `MapOpenApi()`, et Scalar pour l'interface. |
| `.WithOpenApi()` sur un endpoint | Obsolète en .NET 10. | Le supprimer : il n'est plus nécessaire. |
| `Results.Ok(projet)` avec un retour `IResult` | Fonctionne, mais le document OpenAPI ignore alors la forme de la réponse, et rien ne vérifie les `return`. | `TypedResults.Ok(projet)` et un type de retour `Results<…>`. |
| Une classe `Startup` avec `ConfigureServices` et `Configure` | Style antérieur à .NET 6, que la documentation ne propose plus pour du code neuf. | Tout dans `Program.cs`, avec `WebApplication.CreateBuilder`. |
| `app.UseHttpsRedirection()` avec le seul profil `http` | Avertissement à chaque lancement : `Failed to determine the https port for redirect`. | Créer le projet avec `--no-https` quand un serveur frontal porte le HTTPS, ou lancer le profil `https`. |
| `MapOpenApi()` et `MapScalarApiReference()` hors du bloc `IsDevelopment()` | La structure de l'API est publiée en production. | Les laisser dans le bloc. |
| `curl` dans Windows PowerShell | C'est un alias d'`Invoke-WebRequest`, qui n'accepte pas les mêmes options. | `curl.exe`, ou `Invoke-RestMethod`. |
| Chercher `/swagger`, ou `/scalar/v1` comme l'indiquent certains tutoriels | La première adresse n'existe pas ; l'adresse documentée de Scalar est `/scalar`. | `http://localhost:5080/scalar`. |
| Une énumération renvoyée sous forme de nombre | Illisible, et fragile si l'énumération évolue. | `JsonStringEnumConverter`, réglé une fois dans `ConfigureHttpJsonOptions`. |

Les sources de ces constats sont dans le [référentiel .NET 10](../docs/references/dotnet-10.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Suivez une requête `GET /api/projets/1` de son arrivée à sa réponse : quelles lignes de votre code s'exécutent, et dans quel ordre ? Lesquelles ne s'exécutent qu'une fois, au démarrage ?
- `ProjetDto` et `CreerProjetRequete` se ressemblent beaucoup. Pourquoi ne pas se contenter d'un seul type ? Que pourrait envoyer un client mal intentionné si c'était le cas ?
- Qu'apporte `Results<Ok<ProjetDto>, NotFound>` par rapport à un retour `IResult` ? Qui en profite : vous, le compilateur, celui qui écrit l'interface ?
- Votre API a accepté un projet sans code, ou dont la fin précède le début. À quel endroit ces contrôles devraient-ils vivre : dans le gestionnaire, dans le DTO, ailleurs ?
- `global.json` fixe le SDK et `Directory.Packages.props` les paquets. Que se passerait-il, dans six mois, sur le poste d'un nouveau venu, si ces deux fichiers n'existaient pas ?
- Pourquoi Scalar et le document OpenAPI ne sont-ils exposés qu'en développement ? Comment celui qui écrit l'interface obtient-il alors le contrat de l'API ?

## Pour aller plus loin

- [Tutoriel officiel : créer une Minimal API](https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0) : le même parcours sur un autre exemple, avec une base de données.
- [`dotnet watch`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-watch) : relancer l'API automatiquement à chaque modification du code.
- [Environnements d'exécution](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments?view=aspnetcore-10.0) : ce que change `Development`, et comment `launchSettings.json` le fixe.
- [Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0) : comment `appsettings.json`, les variables d'environnement et la ligne de commande se combinent.
- [Routage](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-10.0) : toutes les contraintes de route.
- [API avec contrôleurs](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0) : pour lire du code existant.
- Suite dans le cours : le module 04 écrit les règles des projets, le module 06 remplace la liste en mémoire par une base, le module 08 valide les entrées et pagine la liste.
