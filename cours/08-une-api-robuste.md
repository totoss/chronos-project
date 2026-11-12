# Module 08 — Une API robuste

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 10](../docs/references/dotnet-10.md).
> Le code du pas à pas a été **compilé et exécuté le 12 novembre 2026** dans un conteneur `mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.404, ASP.NET Core et EF Core 10.0.15, Npgsql 10.0.3) contre PostgreSQL 18.6, puis rejoué contre SQL Server 2025 (17.0.5045) : les réponses HTTP reproduites ici sont celles qui ont été obtenues. Les commandes `curl.exe` et `Invoke-RestMethod` ont été essayées sous Windows PowerShell 5.1. **Non exécutés** : les migrations (le schéma de l'essai a été créé sans elles), l'envoi depuis un fichier `.http`, les exercices. Les méthodes de `Collaborateur` utilisées ici sont celles que vous avez écrites dans les exercices du module 04 : leurs noms peuvent différer des vôtres.

À la fin du module 07, l'API de Chronos fonctionne tant qu'on lui parle poliment. Envoyez-lui un collaborateur sans nom, une quotité de 5 %, un matricule déjà pris, ou deux modifications de la même fiche à une seconde d'intervalle : elle répond par une erreur 500, par un message différent à chaque fois, ou — pire — elle enregistre.

Une API est un contrat. Celui qui l'appelle — demain l'interface Angular — doit savoir, sans lire le code du serveur, **ce qu'il a mal fait** et **quoi afficher** à l'utilisateur. Ce module rend les refus de Chronos aussi prévisibles que ses succès.

## Objectifs

À la fin de ce module, vous saurez :

- séparer ce que l'API reçoit, ce qu'elle renvoie et ce qu'elle enregistre (requêtes, DTO, entités) ;
- valider la forme des entrées avec les annotations de données et `AddValidation()`, et dire ce que ce mécanisme ne vérifie pas ;
- renvoyer toutes les erreurs au même format (`ProblemDetails`), avec le bon code : 400, 404, 409, 422 ;
- traduire une `RegleMetierException` en réponse HTTP par un `IExceptionHandler` ;
- paginer, filtrer et trier une liste sans jamais laisser le client écrire un morceau de requête ;
- empêcher qu'une modification en écrase une autre, par la concurrence optimiste.

## Prérequis

- Modules 03, 04, 06 et 07 : l'API sert `/api/projets` et `/api/collaborateurs` depuis la base, avec `PageDto<T>`, des projections `VersDto`, les index uniques et le filtre d'archivage.
- Vous savez ce que signifient les familles de codes HTTP 2xx, 4xx et 5xx.

## Règles de gestion

| Règle | Dans ce module |
| --- | --- |
| **RG-02**, **RG-03** — date de sortie, quotité | Vérifiées par le domaine depuis le module 04 ; l'API les renvoie maintenant en `422` (pas à pas). |
| **RG-11**, **RG-12** — client et dates d'un projet | Même traitement, sur `/api/projets` (exercice 1). |
| **RG-01**, **RG-10** — unicité | Garanties par les index uniques du module 06 ; l'API les traduit en `409` (pas à pas, exercice 1). |
| **RG-04** — pas de boucle dans la chaîne de managers | Elle demande de lire la base : vérifiée à la modification (pas à pas, étape 8 ; exercice 5). |
| **RG-05** — archiver plutôt que supprimer | `DELETE /api/collaborateurs/{id}` choisit entre les deux (pas à pas, étape 9). |

## Notions

### Requête, DTO, entité : trois types pour trois rôles

L'entité `Collaborateur` protège ses invariants ; elle n'est pas faite pour voyager. La renvoyer telle quelle exposerait toutes ses colonnes — y compris celles qu'on ajoutera plus tard —, lierait le JSON public au schéma de la base, et laisserait un client envoyer `"estArchive": true` dans une création.

Chronos distingue donc trois types par ressource : `CreerCollaborateurRequete` et `ModifierCollaborateurRequete` (ce que le client a le droit d'envoyer), `CollaborateurDto` (ce qu'il a le droit de lire), `Collaborateur` (ce qui est vrai). La correspondance s'écrit à la main, comme au module 06.

### Trois façons de refuser

| Ce qui ne va pas | Qui le détecte | Réponse |
| --- | --- | --- |
| La **forme** : champ absent, texte trop long, adresse mal écrite, JSON illisible | Les annotations sur la requête, avant que votre code ne s'exécute | `400`, avec `errors` par champ |
| Une **règle de gestion** : quotité de 5 %, sortie avant l'entrée | Le domaine, qui lève `RegleMetierException` | `422`, avec `regle` |
| Un **conflit avec l'état de la base** : matricule déjà pris, fiche modifiée entre-temps | La base (index unique, jeton de concurrence) | `409` |

Les annotations ne remplacent pas le domaine : un import de fichier ou un traitement de nuit n'y passera pas. Le domaine ne remplace pas les annotations : il s'arrête à la première règle violée, quand un formulaire a besoin de toutes les erreurs d'un coup.

Documentation : [422 Unprocessable Content](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Status/422)

### `ProblemDetails`

`ProblemDetails` est le format normalisé d'une erreur d'API HTTP (RFC 9457, qui remplace la RFC 7807) : un objet JSON servi en `application/problem+json`, avec les membres `type`, `title`, `status`, `detail`, `instance`, et le droit d'en ajouter. Chronos en ajoute deux : `errors` (posé par ASP.NET Core sur les erreurs de validation) et `regle`.

Documentation : [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457), [Gérer les erreurs dans les API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0)

### `AddValidation()`

Depuis .NET 10, les Minimal APIs valident elles-mêmes les paramètres annotés. Un générateur de source lit la signature de vos gestionnaires à la compilation et prépare la validation des types qu'il y trouve ; si elle échoue, le gestionnaire n'est pas appelé et le client reçoit un `400`.

Documentation : [Validation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/validation?view=aspnetcore-10.0), [Validation dans les Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0#validation-support-in-minimal-apis)

### Gérer les exceptions à un seul endroit

Trois pièces se complètent :

- `UseExceptionHandler()` attrape toute exception non traitée et produit une réponse ;
- un `IExceptionHandler` est consulté d'abord : il reconnaît **une** famille d'exceptions et écrit la réponse qui lui convient ;
- `UseStatusCodePages()` donne un corps aux réponses d'erreur qui n'en ont pas, comme un `TypedResults.NotFound()`.

Aucun `try`/`catch` dans les endpoints : ils restent lisibles, et une exception oubliée ne fuit pas vers le client.

Documentation : [Gestion des erreurs — `IExceptionHandler`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)

### Concurrence optimiste

Deux gestionnaires RH ouvrent la fiche de Camille. L'un corrige son poste, l'autre son adresse ; chacun envoie la fiche entière. Sans précaution, le second écrase le travail du premier sans que personne le sache.

La concurrence **optimiste** ne verrouille rien. Chaque fiche porte un **jeton**, renouvelé à chaque modification. Le client renvoie le jeton qu'il a lu ; la mise à jour ne s'applique que si le jeton en base est encore celui-là (`UPDATE … WHERE "Id" = … AND "Version" = …`). Sinon, zéro ligne modifiée : EF Core lève `DbUpdateConcurrencyException`, et l'API répond `409`.

Le module 07 a présenté les jetons propres à chaque SGBD. Chronos prend un jeton géré par l'application, un `Guid Version` : il se comporte de la même façon sur PostgreSQL et sur SQL Server.

Documentation : [Gérer les conflits de concurrence](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)

## Pas à pas

Les commandes se lancent depuis `src/api`. L'API tourne dans un terminal (`dotnet run --project Chronos.Api --launch-profile http`) ; relancez-la après chaque étape.

### 1. Décrire ce que le client envoie

Dans `Chronos.Api/Collaborateurs/`, créez `CreerCollaborateurRequete.cs` :

```csharp
using System.ComponentModel.DataAnnotations;
using Chronos.Domain;

namespace Chronos.Api.Collaborateurs;

public record CreerCollaborateurRequete(
    [Required, StringLength(20)] string Matricule,
    [Required, StringLength(100)] string Nom,
    [Required, StringLength(100)] string Prenom,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required] CategorieCollaborateur? Categorie,
    [Required, StringLength(100)] string Poste,
    [Required] DateOnly? DateEntree,
    [Required] TypeContrat? TypeContrat,
    [Required] decimal? Quotite,
    int? ManagerId);
```

- **Ni `Id`, ni `EstArchive`, ni `DateSortie`** : le client ne les choisit pas à la création.
- **Les longueurs** sont celles de `CollaborateurConfiguration` (module 06) : la requête est refusée avant que la base ne le fasse.
- **Aucune annotation sur la plage de la quotité.** RG-03 appartient au domaine ; ici on exige seulement qu'elle soit fournie.
- **`DateOnly?` avec `[Required]`**, et non `DateOnly` : c'est le point le plus contre-intuitif du module, constaté à l'étape 2.

### 2. Activer la validation et regarder ce qu'elle fait

Dans `Program.cs` :

```csharp
builder.Services.AddValidation();
```

Ajoutez à `MapCollaborateurs` la route `groupe.MapPost("/", Creer);` avec, pour l'instant, un gestionnaire qui renvoie ce qu'il reçoit : `private static Ok<CreerCollaborateurRequete> Creer(CreerCollaborateurRequete requete) => TypedResults.Ok(requete);`. Puis ajoutez à `Chronos.Api.http` (module 03) :

```http
###

POST {{Chronos.Api_HostAddress}}/api/collaborateurs
Content-Type: application/json

{}
```

Réponse `400 Bad Request`, de type `application/problem+json` :

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Matricule": ["The Matricule field is required."],
    "Nom": ["The Nom field is required."],
    "DateEntree": ["The DateEntree field is required."],
    "Quotite": ["The Quotite field is required."]
  }
}
```

(La réponse réelle liste les neuf champs obligatoires.) Votre gestionnaire n'a pas été appelé. Faites maintenant l'expérience inverse : remplacez un instant `[Required] DateOnly? DateEntree` par `DateOnly DateEntree` et renvoyez `{}`. L'erreur sur `DateEntree` disparaît, et la date vaudrait `0001-01-01` : un type valeur non nullable **a toujours une valeur**, celle par défaut, et `[Required]` n'y voit rien. Pire pour une énumération : `Categorie` vaudrait silencieusement `Consultant`. Remettez `DateOnly?`.

Ce qui a été constaté en .NET 10.0.15 :

| `AddValidation()`… | Constat |
| --- | --- |
| valide le corps JSON, les paramètres de requête et d'en-tête annotés | Oui, y compris les types réunis par `[AsParameters]` (étape 6). |
| valide les `record`, les objets imbriqués et les éléments de liste | Oui ; la clé d'erreur devient alors `Saisies[0].Duree`. |
| nomme les erreurs comme le JSON | **Non** : les clés sont les noms C# (`Matricule`), pas ceux du JSON (`matricule`). L'interface devra en tenir compte. |
| traduit les messages | **Non** : ils sont en anglais. Écrivez `[Required(ErrorMessage = "Le matricule est obligatoire.")]` là où le message sera montré tel quel. |
| applique `[Required]` à un **paramètre** nullable (`int? x` dans la signature) | **Non** : l'absence passe. `[Range]` sur ce même paramètre est bien vérifié quand une valeur est fournie. Limite connue de .NET 10, corrigée en .NET 11 ([dotnet/aspnetcore#67033](https://github.com/dotnet/aspnetcore/issues/67033)). Une **propriété** `DateOnly?` d'un corps JSON n'est pas concernée : `[Required]` y fonctionne. |
| appelle la base ou un service asynchrone | **Non** en .NET 10 : l'unicité se vérifie ailleurs (étape 5). |

### 3. Le même format pour toutes les erreurs

Toujours dans `Program.cs` :

```csharp
builder.Services.AddProblemDetails();
```

et, juste après `var app = builder.Build();` :

```csharp
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException requeteIllisible
        ? requeteIllisible.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
```

Trois observations à faire :

```powershell
curl.exe -i http://localhost:5080/api/collaborateurs/999
```

`404`, avec un corps `{"type":"…","title":"Not Found","status":404,"traceId":"…"}`. Votre `TypedResults.NotFound()` ne contenait rien : `UseStatusCodePages` l'a complété. Il en va de même pour une adresse qui n'existe pas.

Envoyez ensuite `"categorie": "Stagiaire"`, ou un JSON tronqué : `400`, titre `Bad Request`, sans `errors`. Ici, ce n'est pas la validation qui refuse : le JSON n'a pas pu être lu.

Retirez enfin le `StatusCodeSelector` (écrivez `app.UseExceptionHandler();`) et recommencez : en `Development`, la même requête répond **500**. Dans cet environnement, une requête illisible lève une `BadHttpRequestException`, et le gestionnaire d'exceptions de .NET 10 traduit par défaut toute exception en 500 ; en production la réponse est bien 400. Un client qui se trompe ne doit pas faire croire à une panne du serveur, et le développement ne doit pas se comporter autrement que la production : remettez le sélecteur.

Documentation : [`StatusCodeSelector`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.builder.exceptionhandleroptions.statuscodeselector?view=aspnetcore-10.0)

### 4. Une règle de gestion violée : 422

Créez `Chronos.Api/Erreurs/RegleMetierExceptionHandler.cs` :

```csharp
using Chronos.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Chronos.Api.Erreurs;

internal sealed class RegleMetierExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not RegleMetierException regle)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Règle de gestion non respectée",
                Detail = regle.Message,
                Extensions = { ["regle"] = regle.CodeRegle },
            },
        });
    }
}
```

et enregistrez-le dans `Program.cs`, sous `AddProblemDetails()` :

```csharp
builder.Services.AddExceptionHandler<RegleMetierExceptionHandler>();
```

- **`return false`** : « cette exception n'est pas la mienne ». Le suivant est consulté, puis le traitement par défaut.
- **`return true`** n'a de sens qu'après avoir écrit la réponse **et** son code de statut.
- **`regle.Message` est renvoyé au client** parce que c'est vous qui l'avez écrit, pour lui. Ce n'est vrai d'aucune autre exception.
- Les gestionnaires sont des singletons, appelés dans l'ordre de leur enregistrement, et seulement si `UseExceptionHandler` est en place.

**Et les deux autres exceptions du domaine ?** Le module 04 en lève trois. `ArgumentException` (code vide, budget négatif) signale un défaut du programme : les annotations de l'étape 1 doivent l'avoir intercepté avant, et si elle passe quand même, c'est une anomalie à corriger chez nous. `InvalidOperationException` est levée par le domaine, mais aussi par EF Core et par .NET pour toutes sortes de raisons. On ne les traduit donc **pas** globalement : elles suivent le traitement par défaut, un `500` dont le corps ne contient ni message ni pile d'appels, et dont le détail est dans le journal du serveur.

### 5. Créer un collaborateur : 201, 409 ou 422

Remplacez le gestionnaire provisoire :

```csharp
private static async Task<Results<Created<CollaborateurDto>, ProblemHttpResult>> Creer(
    CreerCollaborateurRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
{
    var dejaPris = await db.Collaborateurs
        .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
        .AnyAsync(c => c.Matricule == requete.Matricule || c.Email == requete.Email, cancellationToken);
    if (dejaPris)
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            detail: "Un collaborateur porte déjà ce matricule ou cette adresse électronique (RG-01).");
    }

    var collaborateur = new Collaborateur(requete.Matricule, requete.Nom, requete.Prenom, requete.Email,
        requete.Categorie!.Value, requete.Poste, requete.DateEntree!.Value);
    collaborateur.AjouterContrat(requete.TypeContrat!.Value, requete.Quotite!.Value, requete.DateEntree.Value);
    collaborateur.RattacherA(requete.ManagerId);

    db.Collaborateurs.Add(collaborateur);
    await db.SaveChangesAsync(cancellationToken);

    var dto = await db.Collaborateurs
        .Where(c => c.Id == collaborateur.Id).Select(VersDto).SingleAsync(cancellationToken);
    return TypedResults.Created($"/api/collaborateurs/{dto.Id}", dto);
}
```

- **Les `!.Value`** sont légitimes ici, et ici seulement : la validation a garanti que ces valeurs sont présentes.
- **`IgnoreQueryFilters`** : l'index unique voit aussi les collaborateurs archivés ; la vérification doit les voir également.
- **La casse** : PostgreSQL distingue `m010` de `M010`, SQL Server non (module 07). La vérification compare donc des valeurs que le domaine a déjà normalisées — c'était l'exercice 1 du module 07.
- **`AjouterContrat` lève RG-03** si la quotité sort des bornes. Essayez `"quotite": 0.05` :

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Règle de gestion non respectée",
  "status": 422,
  "detail": "La quotité est comprise entre 10 % et 100 %.",
  "regle": "RG-03"
}
```

Avec un corps valide, la réponse est `201 Created`, porte l'en-tête `Location: /api/collaborateurs/4` et le DTO. Renvoyez la même requête : `409`.

**La vérification préalable ne suffit pas.** Entre `AnyAsync` et `SaveChangesAsync`, une autre requête peut insérer le même matricule : l'index unique refuse alors, EF Core lève `DbUpdateException`, et le client reçoit un 500. Il faut un filet. Chaque SGBD signale cette violation par son propre code ; on enferme ce savoir dans l'infrastructure. `Chronos.Infrastructure/ErreursBaseDeDonnees.cs` :

```csharp
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Chronos.Infrastructure;

public static class ErreursBaseDeDonnees
{
    public static bool EstViolationUnicite(DbUpdateException exception) => exception.InnerException switch
    {
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => true,
        SqlException { Number: 2601 or 2627 } => true,
        _ => false,
    };
}
```

Puis un second gestionnaire, `Chronos.Api/Erreurs/ConflitExceptionHandler.cs`, construit comme le premier. Seul le début de `TryHandleAsync` change :

```csharp
var detail = exception switch
{
    DbUpdateConcurrencyException =>
        "La ressource a été modifiée ou supprimée depuis que vous l'avez lue. Rechargez-la, puis recommencez.",
    DbUpdateException e when ErreursBaseDeDonnees.EstViolationUnicite(e) =>
        "Une ressource porte déjà cette valeur, qui doit être unique.",
    _ => null,
};

if (detail is null)
{
    return false;
}
```

La suite écrit un `ProblemDetails` de statut `409` et de titre `Conflit`. Enregistrez-le avec `AddExceptionHandler<ConflitExceptionHandler>()`. Le premier cas servira à l'étape 7 ; il est écrit **avant** le second parce que `DbUpdateConcurrencyException` hérite de `DbUpdateException`.

Une `DbUpdateException` qui n'est pas une violation d'unicité (une clé étrangère inconnue, par exemple) reste une erreur 500 : on ne la déguise pas en conflit.

Documentation : [`PostgresErrorCodes`](https://www.npgsql.org/doc/api/Npgsql.PostgresErrorCodes.html), [erreurs 2601 et 2627 de SQL Server](https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors-2000-to-2999)

### 6. Paginer, filtrer, trier

La pagination existe depuis le module 06 (`PageDto<T>`, `page`, `taille`). Il lui manque des bornes, un filtre et un tri. Les paramètres de la liste deviennent un type. `Chronos.Api/Collaborateurs/FiltreCollaborateurs.cs` :

```csharp
public record FiltreCollaborateurs(
    [Range(1, int.MaxValue)] int Page = 1,
    [Range(1, 100)] int Taille = 20,
    [StringLength(100)] string? Recherche = null,
    string? Statut = null,
    string? Tri = null);
```

Le gestionnaire le reçoit avec `[AsParameters]` : chaque propriété est lue dans la chaîne de requête, les valeurs par défaut s'appliquent, et les annotations sont validées (`?taille=500` répond `400`, avec la clé `Taille`).

```csharp
private static async Task<Results<Ok<PageDto<CollaborateurDto>>, ValidationProblem>> Lister(
    [AsParameters] FiltreCollaborateurs filtre, ChronosDbContext db, TimeProvider horloge,
    CancellationToken cancellationToken)
{
    var requete = db.Collaborateurs.AsQueryable();

    if (!string.IsNullOrWhiteSpace(filtre.Recherche))
    {
        var recherche = filtre.Recherche.Trim().ToLower();
        requete = requete.Where(c =>
            c.Nom.ToLower().Contains(recherche) ||
            c.Prenom.ToLower().Contains(recherche) ||
            c.Matricule.ToLower().Contains(recherche));
    }

    var aujourdhui = DateOnly.FromDateTime(horloge.GetLocalNow().DateTime);
    switch (filtre.Statut)
    {
        case null or "":
            break;
        case "actif":
            requete = requete.Where(c => c.DateSortie == null || c.DateSortie >= aujourdhui);
            break;
        case "sorti":
            requete = requete.Where(c => c.DateSortie < aujourdhui);
            break;
        default:
            return Invalide("statut", "Valeurs acceptées : actif, sorti.");
    }

    IOrderedQueryable<Collaborateur> triee;
    switch (filtre.Tri)
    {
        case null or "" or "nom":
            triee = requete.OrderBy(c => c.Nom).ThenBy(c => c.Prenom);
            break;
        case "-nom":
            triee = requete.OrderByDescending(c => c.Nom).ThenByDescending(c => c.Prenom);
            break;
        case "matricule":
            triee = requete.OrderBy(c => c.Matricule);
            break;
        default:
            return Invalide("tri", "Valeurs acceptées : nom, -nom, matricule.");
    }

    var total = await requete.CountAsync(cancellationToken);

    var elements = await triee
        .ThenBy(c => c.Id)
        .Skip((filtre.Page - 1) * filtre.Taille)
        .Take(filtre.Taille)
        .Select(VersDto)
        .ToListAsync(cancellationToken);

    return TypedResults.Ok(new PageDto<CollaborateurDto>(elements, filtre.Page, filtre.Taille, total));
}

private static ValidationProblem Invalide(string parametre, string message) =>
    TypedResults.ValidationProblem(new Dictionary<string, string[]> { [parametre] = [message] });
```

- **Une requête se compose** : chaque `Where` s'ajoute, rien n'est exécuté avant `CountAsync` et `ToListAsync`. Deux requêtes SQL partent, quel que soit le filtre.
- **Le tri est une liste blanche.** La chaîne `tri` ne sert qu'à **choisir** parmi des expressions écrites par vous. Elle n'est jamais collée dans une requête : `?tri=email;drop table` répond `400`, rien de plus.
- **`ThenBy(c => c.Id)`** rend l'ordre total. Deux collaborateurs de même nom n'ont sinon aucun ordre garanti entre eux, et l'un d'eux peut apparaître sur deux pages ou sur aucune.
- **`Taille` est bornée** : sans plafond, `?taille=1000000` devient un moyen simple de mettre l'API à genoux.
- **`ToLower()` des deux côtés** donne une recherche insensible à la casse sur les deux SGBD (PostgreSQL y est sensible par défaut, SQL Server non). Le prix : aucun index ne sert. C'est acceptable pour quelques centaines de collaborateurs.
- **`TimeProvider`** fournit la date du jour. Enregistrez-le avec `builder.Services.AddSingleton(TimeProvider.System);` ; le [module 09](09-affectations-et-calcul-de-charge.md) explique pourquoi on ne lit pas l'horloge directement.

Essayez `/api/collaborateurs?recherche=DU&tri=-nom&taille=2`, puis `?page=0`, `?tri=email`, `?statut=parti`.

Documentation : [`[AsParameters]`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0), [Pagination](https://learn.microsoft.com/en-us/ef/core/querying/pagination), [Collations et casse](https://learn.microsoft.com/en-us/ef/core/miscellaneous/collations-and-case-sensitivity)

### 7. Modifier sans écraser le travail d'un autre

Dans `Collaborateur`, ajoutez le jeton, et renouvelez-le dans **chaque** méthode qui modifie la fiche :

```csharp
public Guid Version { get; private set; } = Guid.NewGuid();
```

```csharp
Version = Guid.NewGuid();
```

Dans `CollaborateurConfiguration` :

```csharp
builder.Property(c => c.Version).IsConcurrencyToken();
```

Le modèle a changé : c'est la routine à deux jeux du module 07.

```powershell
dotnet ef migrations add VersionCollaborateur --project Chronos.Migrations.PostgreSql --startup-project Chronos.Api -- --Chronos:Fournisseur PostgreSql
```

```powershell
dotnet ef migrations add VersionCollaborateur --project Chronos.Migrations.SqlServer --startup-project Chronos.Api -- --Chronos:Fournisseur SqlServer
```

Lisez les migrations avant de les appliquer : les lignes existantes reçoivent toutes le même jeton, `00000000-…`. Ce n'est pas gênant — il sera renouvelé à leur première modification.

Ajoutez `Guid Version` en dernière position de `CollaborateurDto` et de sa projection `VersDto`. Votre DTO est né d'un exercice du module 06 ; c'est le moment de lui donner sa forme de référence, celle sur laquelle s'appuient les modules suivants et l'interface :

```csharp
public record CollaborateurDto(
    int Id, string Matricule, string Nom, string Prenom, string Email,
    CategorieCollaborateur Categorie, string Poste, int? ManagerId, string? ManagerNom,
    DateOnly DateEntree, DateOnly? DateSortie, decimal Quotite, Guid Version);
```

`Quotite` est la quotité du contrat en cours — celui qui n'a pas de date de fin —, en fraction (`0.8`) ; c'est déjà ce que calcule votre projection `VersDto` du module 06. Puis la requête de modification :

```csharp
public record ModifierCollaborateurRequete(
    [Required, StringLength(100)] string Nom,
    [Required, StringLength(100)] string Prenom,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(100)] string Poste,
    DateOnly? DateSortie,
    int? ManagerId,
    [Required] Guid? Version);
```

et le gestionnaire, branché par `groupe.MapPut("/{id:int}", Modifier);` :

```csharp
private static async Task<Results<Ok<CollaborateurDto>, NotFound, ProblemHttpResult>> Modifier(
    int id, ModifierCollaborateurRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
{
    var collaborateur = await db.Collaborateurs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    if (collaborateur is null)
    {
        return TypedResults.NotFound();
    }

    // Même vérification d'unicité qu'à l'étape 5, sur l'adresse seule et en excluant c.Id == id.

    db.Entry(collaborateur).Property(c => c.Version).OriginalValue = requete.Version!.Value;

    collaborateur.Modifier(requete.Nom, requete.Prenom, requete.Email, requete.Poste,
        requete.DateSortie, requete.ManagerId);

    await db.SaveChangesAsync(cancellationToken);

    return TypedResults.Ok(await db.Collaborateurs
        .Where(c => c.Id == id).Select(VersDto).SingleAsync(cancellationToken));
}
```

La ligne décisive est celle d'`OriginalValue`. EF Core compare le jeton en base à la **valeur d'origine** de la propriété, c'est-à-dire à celle qu'il vient de lire. Or ce qui compte, c'est la version que **le client** avait sous les yeux. On remplace donc l'une par l'autre, et l'`UPDATE` devient (journal SQL du module 06) :

```sql
UPDATE "Collaborateurs" SET "Poste" = @p0, "Version" = @p1
WHERE "Id" = @p2 AND "Version" = @p3;
```

Faites l'expérience. Lisez `/api/collaborateurs/2` et notez `version`. Envoyez un `PUT` avec cette version : `200`, et une **nouvelle** version dans la réponse. Renvoyez exactement le même `PUT` :

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflit",
  "status": 409,
  "detail": "La ressource a été modifiée ou supprimée depuis que vous l'avez lue. Rechargez-la, puis recommencez."
}
```

Le `ConflitExceptionHandler` de l'étape 5 a fait le travail. Envoyez enfin une `dateSortie` antérieure à la date d'entrée, avec la bonne version : `422`, `"regle": "RG-02"`.

Documentation : [Jeton de concurrence et valeur d'origine](https://learn.microsoft.com/en-us/aspnet/core/data/ef-rp/concurrency?view=aspnetcore-10.0) (tutoriel Razor Pages : la ligne `OriginalValue` y est expliquée)

### 8. RG-04 : pas de boucle dans la chaîne de managers

Le module 04 a laissé cette règle de côté : pour savoir si Camille deviendrait son propre manager « par une chaîne », il faut lire d'autres collaborateurs. C'est donc ici, à la modification, qu'on la vérifie : on remonte la chaîne à partir du manager proposé, et on refuse si l'on retombe sur le collaborateur modifié.

```csharp
private static async Task VerifierChaineDeManagers(
    ChronosDbContext db, int collaborateurId, int? managerId, CancellationToken cancellationToken)
{
    var dejaVus = new HashSet<int>();
    var courant = managerId;

    while (courant is not null && dejaVus.Add(courant.Value))
    {
        if (courant == collaborateurId)
        {
            throw new RegleMetierException("RG-04",
                "Un collaborateur ne peut pas être son propre manager, ni directement ni par une chaîne de managers.");
        }

        courant = await db.Collaborateurs
            .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
            .Where(c => c.Id == courant)
            .Select(c => c.ManagerId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

Appelez-la dans `Modifier`, juste avant la ligne d'`OriginalValue` : `await VerifierChaineDeManagers(db, id, requete.ManagerId, cancellationToken);`.

- **La règle lève `RegleMetierException`** bien qu'elle soit écrite dans l'API : c'est une règle numérotée, le client doit recevoir `422` et `"regle": "RG-04"`, et le gestionnaire de l'étape 4 s'en charge.
- **`dejaVus`** arrête la boucle si les données contiennent déjà un cycle : sans lui, une base abîmée ferait tourner la requête sans fin.
- **Une requête par niveau** de hiérarchie. C'est acceptable pour cinq ou six niveaux, sur une opération rare.
- **`IgnoreQueryFilters`** : un manager archivé fait toujours partie de la chaîne.

Vérifiez : rattachez Bob à Camille, elle-même rattachée à Alex, puis essayez de rattacher Alex à Bob. La réponse est `422`, `RG-04`. Rattachez enfin Bob à un manager qui n'existe pas (`"managerId": 999`) : `500`. La clé étrangère a refusé, et le filet de l'étape 5 a eu raison de ne pas en faire un conflit — mais c'est une erreur du client, qui mérite mieux (exercice 5).

### 9. Supprimer ou archiver (RG-05)

Votre `DELETE` du module 06 archive toujours. RG-05 n'interdit pourtant la suppression qu'à celui qui « possède » des données. Aujourd'hui, les seules données liées qui existent sont les collaborateurs qu'il manage et les projets dont il est responsable :

```csharp
var aDesDonneesLiees =
    await db.Collaborateurs
        .IgnoreQueryFilters([CollaborateurConfiguration.FiltreNonArchives])
        .AnyAsync(c => c.ManagerId == id, cancellationToken)
    || await db.Projets.AnyAsync(p => p.ResponsableId == id, cancellationToken);

if (aDesDonneesLiees)
{
    collaborateur.Archiver();
}
else
{
    db.Collaborateurs.Remove(collaborateur);
}

await db.SaveChangesAsync(cancellationToken);
return TypedResults.NoContent();
```

Dans les deux cas le client reçoit `204`, et un `GET` suivant répond `404`. Les modules 09 et 10 ajouteront les affectations, les saisies et les absences à la condition.

## À vous

1. **Les projets.** Donnez à `/api/projets` le même traitement : `CreerProjetRequete` annotée (de sorte qu'aucun `ArgumentException` du constructeur de `Projet` ne puisse plus être atteint depuis l'API — essayez un budget négatif avant et après), `ModifierProjetRequete`, `PUT /api/projets/{id}` avec jeton de concurrence, filtres `recherche` et `statut`, tri à liste blanche. Vérifiez que RG-11 et RG-12 répondent `422` avec le bon `regle`, et qu'un code déjà pris répond `409` (RG-10).

2. **Les clients.** Écrivez le CRUD complet de `/api/clients`. Que répond `DELETE` pour un client qui a des projets ? Lisez l'exception dans le journal, décidez du code HTTP, et traitez ce cas **avant** d'appeler la base.

3. **Changer le statut d'un projet.** `Demarrer()`, `Terminer()` et `Annuler()` lèvent `InvalidOperationException` quand la transition est impossible. Constatez ce que reçoit le client aujourd'hui. Proposez une réponse plus juste, et mettez-la en œuvre sans traduire globalement toutes les `InvalidOperationException`.

4. **Des messages pour l'utilisateur.** Donnez un `ErrorMessage` en français aux annotations de `CreerCollaborateurRequete`. Puis envoyez à `PUT /api/collaborateurs/{id}` un corps sans `version` : la réponse est-elle celle que vous attendiez ? Et avec `"version": "abc"` ?

5. **Finir RG-04.** Un `managerId` inconnu répond aujourd'hui `500` : faites-en un `400` avec une entrée `errors`, à la création comme à la modification. La création a-t-elle besoin de `VerifierChaineDeManagers` ? Pourquoi ? Comptez dans le journal les requêtes émises pour une chaîne de trois niveaux. Enfin, deux gestionnaires RH modifient au même instant deux fiches différentes, chacun rattachant l'une à l'autre : la vérification suffit-elle ? Gardez la question pour le module 10.

6. **Le filet, pour de vrai.** Provoquez la violation d'index unique sans passer par la vérification préalable (retirez-la un instant) et vérifiez que la réponse est bien `409` sur PostgreSQL **et** sur SQL Server. Quelle exception interne voyez-vous dans le journal de chacun ?

## Critères de réussite

- `POST /api/collaborateurs` répond `400` avec un `errors` par champ pour `{}`, `422` avec `"regle": "RG-03"` pour une quotité de 5 %, `201` avec `Location` pour une requête valide, `409` pour un matricule ou une adresse déjà pris — y compris ceux d'un collaborateur archivé.
- Un JSON illisible répond `400` en `Development` comme en production ; une exception inattendue répond `500` sans message ni pile d'appels dans le corps.
- Toutes les réponses d'erreur ont le type `application/problem+json`, `404` compris.
- `GET /api/collaborateurs` refuse `taille=500`, `page=0` et un `tri` inconnu par un `400`, et n'émet que deux requêtes SQL.
- Deux `PUT` envoyés avec la même `version` donnent `200` puis `409` ; le journal montre `AND "Version" = …` dans l'`UPDATE`.
- Rattacher un manager à l'un de ses subordonnés, direct ou indirect, répond `422` avec `"regle": "RG-04"`.
- Aucun endpoint ne contient de `try`/`catch`, et aucun ne renvoie une entité.
- `dotnet build` se termine sans avertissement ; `dotnet ef migrations has-pending-model-changes` ne signale rien, pour chacun des deux jeux de migrations.
- Comparez votre travail à la solution de référence : `git diff module-07 module-08`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| Renvoyer ou recevoir l'entité EF Core | Le JSON public suit le schéma de la base ; le client peut fixer des propriétés qu'il ne devrait pas toucher. | Une requête en entrée, un DTO en sortie. |
| Valider à la main dans chaque endpoint, ou par un filtre d'endpoint | Pratique d'avant .NET 10 : du code répété, des réponses différentes d'un endpoint à l'autre. | Annotations et `AddValidation()`. |
| `[Required]` sur un `DateOnly`, un `int` ou une énumération non nullables | Le champ absent prend la valeur par défaut (`0001-01-01`, `0`, la première valeur) sans erreur. Constaté. | `DateOnly?`, `int?`, `Enum?` avec `[Required]`. |
| `[Required] int? x` en paramètre de requête | Ignoré en .NET 10 quand le paramètre est absent. | Une valeur par défaut et `[Range]`, ou un test dans le gestionnaire. |
| Un `try`/`catch` par endpoint pour choisir le code HTTP | Répété, donc oublié une fois ; les formats divergent. | `IExceptionHandler`, enregistré une fois. |
| Renvoyer `exception.Message` ou la pile d'appels pour « aider » | Fuite d'informations internes : noms de tables, requêtes, chemins. | Le `500` par défaut ; le détail va dans le journal. |
| `AddExceptionHandler<T>()` sans `app.UseExceptionHandler()` | Les gestionnaires ne sont jamais appelés. | Les deux. |
| `app.UseExceptionHandler()` sans option en .NET 10 | En `Development`, un JSON illisible répond 500. Constaté. | Le `StatusCodeSelector` de l'étape 3. |
| Citer la RFC 7807 | Elle est remplacée par la RFC 9457 ; le format est le même. | RFC 9457. |
| `OrderBy` construit à partir du texte reçu (SQL concaténé, LINQ « dynamique ») | Injection, ou tri sur une colonne qu'on ne voulait pas exposer. | Une liste blanche : `switch` sur des valeurs connues. |
| `Skip`/`Take` sans tri, ou avec un tri non unique | Des lignes en double ou manquantes d'une page à l'autre. | `OrderBy(…).ThenBy(c => c.Id)`. |
| Vérifier l'unicité par `AnyAsync` seulement | Deux requêtes simultanées passent toutes les deux la vérification. | L'index unique, et le filet sur `DbUpdateException`. |
| `catch (DbUpdateException)` → 409, sans regarder la cause | Une clé étrangère inconnue devient un « conflit ». | Tester le code d'erreur du SGBD, dans l'infrastructure. |
| Comparer `Version` dans le gestionnaire, puis enregistrer | Entre la comparaison et l'`UPDATE`, un autre a le temps de passer. | `OriginalValue`, pour que la base fasse la comparaison. |
| Oublier de renouveler `Version` dans une méthode de l'entité | Les modifications faites par cette méthode ne sont plus protégées. | Un test par méthode : la version a changé. |

Les sources sont dans le [référentiel .NET 10](../docs/references/dotnet-10.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- RG-03 est vérifiée par le domaine. Fallait-il **aussi** l'écrire en annotation sur la requête ? Que gagne l'utilisateur, que risque-t-on le jour où la règle change ?
- 400, 409, 422 : pour chacun, que doit faire l'interface ? Lequel peut-elle corriger seule, lequel demande de relire des données, lequel doit être montré tel quel ?
- Les clés de `errors` portent les noms C# (`Matricule`) et les messages sont en anglais. Est-ce à l'API ou à l'interface de s'adapter ? Qu'est-ce que cela change pour le module 17 ?
- Après un `409` de concurrence, que proposez-vous à l'utilisateur qui vient de saisir dix champs ?
- Le jeton est renouvelé par l'entité. Un intercepteur de `SaveChanges` pourrait le faire pour toutes les entités. Qu'y gagnerait-on, qu'y perdrait-on ?
- Un collaborateur archivé garde son matricule, et le matricule reste refusé. Est-ce ce que veut le métier ?
- La recherche par `ToLower().Contains(…)` n'utilise aucun index. À partir de quand est-ce un problème, et comment le sauriez-vous ?

## Pour aller plus loin

- [Créer des réponses dans les Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0) : tous les `TypedResults`, et la personnalisation des erreurs de validation par `IProblemDetailsService`.
- [Pagination](https://learn.microsoft.com/en-us/ef/core/querying/pagination) : la pagination par clé (*keyset*), plus efficace que `Skip`/`Take` quand on ne saute pas de page.
- [Filtres de requête globaux](https://learn.microsoft.com/en-us/ef/core/querying/filters) : les filtres nommés, et le piège des relations obligatoires rencontré au module 06.
- [Requêtes efficaces](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying) : index, projections, taille des résultats.
- Suite dans le cours : le module 11 teste ces réponses de bout en bout ; le module 16 les lit depuis Angular, le module 17 affiche `errors` dans un formulaire.
