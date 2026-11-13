# Module 12 — Sécurité et traçabilité

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 11](../docs/references/dotnet-11.md).
> Le module a été **rejoué le 12 novembre 2026 sur la solution de référence** (SDK 10.0.404, ASP.NET Core Identity et EF Core 10.0.15, Npgsql 10.0.3, PostgreSQL 18 et SQL Server 2025) : l'extrait de l'étape 9 dans le `switch` du module 07, les deux commandes `dotnet ef migrations add`, et 110 tests d'intégration verts sur chacun des deux SGBD. Le contrat de l'étape 5 (cookies, jeton anti-CSRF, 401, 403, 404, déconnexion) et le périmètre sur trois niveaux hiérarchiques ont en outre été vérifiés par des appels `curl` sur l'API lancée. **Non exécuté** : le dialogue avec un vrai navigateur et avec Angular, le passage par un serveur frontal en HTTPS (seul l'attribut `Secure` du cookie de session a été constaté, en environnement `Production`), la commande `dotnet user-secrets` (le mot de passe de démonstration a été passé par la configuration). Signalez au mentor tout écart constaté.
> **Aligné sur .NET 11 le 13 novembre 2026** (SDK 11.0.100, ASP.NET Core 11.0, EF Core et Identity restés en 10.0.15). Ont été rejoués en .NET 11 : `dotnet package add Microsoft.AspNetCore.Identity.EntityFrameworkCore` sans version (étape 1 : `NU1605`) ; un endpoint JSON marqué `RequireAntiforgeryTokenAttribute`, appelé sans jeton, avec et sans `UseAntiforgery()` (encadré de l'étape 5) ; l'état final de la solution lancé sur PostgreSQL 18, avec les appels `curl` de la rubrique « CSRF » (écriture par cookie sans jeton, avec un `Origin` étranger, avec un jeton valide, sans cookie) ; les mêmes appels sur une copie de travail **sans** le filtre de l'étape 5, avec et sans `UseAntiforgery()`, pour savoir ce que le framework fait seul ; l'absence de redirection vers une page de connexion (401). Les 144 tests d'intégration de l'état final passent en .NET 11 (vérifié par le coordinateur, non rejoué ici). La matrice CSRF a en outre été rejouée une seconde fois, indépendamment, à la relecture du 13 novembre 2026 (avec et sans le filtre, méthodes `POST`, `PUT`, `DELETE`, en-têtes `same-origin`, `same-site`, `cross-site`). **N'ont pas été rejoués** : l'écriture pas à pas des étapes 1 à 4 et 6 à 9 depuis le tag `module-11`, ni SQL Server, ni le dialogue avec un vrai navigateur (le comportement décrit pour `Sec-Fetch-Site` vient de la documentation et d'en-têtes posés à la main avec `curl`, pas d'un navigateur).

Jusqu'ici, n'importe qui peut appeler l'API et lire toutes les fiches. Chronos contient des données RH : le cahier des charges exige que chacun ne voie que ce qui le concerne, que les actions soient tracées, et qu'on ne collecte que le nécessaire. Ce module donne à l'API des utilisateurs, et lui apprend à leur dire non.

## Objectifs

À la fin de ce module, vous saurez :

- distinguer authentification et autorisation, et les réponses 401, 403 et 404 ;
- authentifier les utilisateurs avec ASP.NET Core Identity et un cookie, en n'exposant que la connexion ;
- protéger une API à cookies contre les requêtes forgées (CSRF) ;
- autoriser par rôle avec des politiques, et par **périmètre** en filtrant les requêtes ;
- tracer qui a créé et modifié chaque donnée, sans y penser dans chaque endpoint ;
- ne renvoyer à chacun que les données dont il a besoin.

## Prérequis

- Module 11 : le projet `Chronos.Api.Tests`, sa fabrique et son horloge pilotée. Vous y ajouterez les tests de ce module.
- Module 10 : les feuilles de temps et leur cycle de validation.
- Modules 06 et 07 : données initiales, choix du fournisseur par `Chronos:Fournisseur`, un projet de migrations par SGBD.

## Règles de gestion

- **RG-36** — Seul le manager du collaborateur (ou un gestionnaire RH) valide ou rejette sa feuille ; nul ne valide la sienne.
- **RG-50** — Un collaborateur ne voit que ses propres données. **RG-51** — Un manager voit en plus celles de son équipe, directe et indirecte.
- **RG-52** — Les informations contractuelles ne sont modifiables que par un gestionnaire RH.
- **RG-53** — Toute création, modification ou validation est tracée : qui, quand.
- Exigence « données personnelles » (§ 6) : ne collecter et ne montrer que le nécessaire.

## Notions

### Authentifier, autoriser

**Authentifier**, c'est établir qui fait la requête. **Autoriser**, c'est décider si cette personne a le droit de faire ce qu'elle demande. L'API répond par trois codes différents :

| Code | Signification | Exemple dans Chronos |
| --- | --- | --- |
| 401 | « Je ne sais pas qui vous êtes. » | Aucun cookie de session. |
| 403 | « Je sais qui vous êtes, et vous n'avez pas ce droit. » | Un collaborateur tente de créer une fiche. |
| 404 | « Pour vous, cela n'existe pas. » | Un manager demande la fiche d'une personne hors de son équipe. |

### Un cookie, pas un jeton fabriqué maison

Presque tous les tutoriels font écrire un endpoint `login` qui vérifie un mot de passe, fabrique un JWT et le renvoie au navigateur, lequel le range dans le `localStorage`. La documentation d'ASP.NET Core le déconseille explicitement : ne jamais créer un jeton d'accès à partir d'un identifiant et d'un mot de passe, et réserver les jetons aux protocoles normalisés (OpenID Connect, OAuth).

Pour une application qui tourne dans le navigateur, elle recommande le **cookie** : le navigateur le renvoie tout seul, et marqué `HttpOnly`, il est illisible par JavaScript — un script malveillant injecté dans la page ne peut pas le voler.

Documentation : [Identity pour une SPA](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-11.0), [Authentification par jeton JWT](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-11.0)

### ASP.NET Core Identity

Identity est la brique d'ASP.NET Core qui gère les comptes : hachage des mots de passe, rôles, verrouillage, cookie de session. Elle range ses données dans sept tables (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`…) à travers EF Core.

`MapIdentityApi<TUser>()` expose des endpoints JSON prêts à l'emploi. Il en crée **dix**, ni plus ni moins : `POST /register`, `POST /login`, `POST /refresh`, `GET /confirmEmail`, `POST /resendConfirmationEmail`, `POST /forgotPassword`, `POST /resetPassword`, `POST /manage/2fa`, `GET` et `POST /manage/info`. Trois conséquences pour Chronos :

- **l'inscription est libre** (`/register`). Dans un outil RH, c'est l'administrateur qui crée les comptes ; or `MapIdentityApi` n'a aucune option pour choisir ses endpoints. On les neutralisera à l'étape 3 ;
- **il n'y a pas de déconnexion** : la documentation demande de l'écrire soi-même ;
- **les passkeys** (connexion sans mot de passe) sont gérées par Identity 10, mais seul le modèle de projet Blazor les met en œuvre : `MapIdentityApi` n'expose aucun endpoint pour elles.

`POST /login` reçoit `{ "email", "password" }`. Avec `?useCookies=true`, il pose un cookie persistant ; avec `?useSessionCookies=true`, un cookie qui disparaît à la fermeture du navigateur ; sans paramètre, il renvoie un jeton propre à Identity — qui n'est pas un JWT, et que Chronos n'utilise pas.

Documentation : [Introduction à Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-11.0), [Personnaliser le modèle d'Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-11.0), [Passkeys](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-11.0)

### CSRF : la contrepartie du cookie

Puisque le navigateur envoie le cookie tout seul, un site tiers peut tenter d'en profiter : une page piégée qui déclenche une requête vers Chronos la ferait partir avec la session de la victime. C'est une **requête forgée** (CSRF).

Deux protections se complètent :

- l'attribut **`SameSite`** du cookie, qui demande au navigateur de ne pas le joindre aux requêtes venues d'un autre site ;
- un **jeton anti-CSRF** : le serveur remet au client une valeur que le client doit renvoyer dans un **en-tête** à chaque écriture. Un site tiers peut provoquer l'envoi du cookie, mais ne peut ni lire cette valeur ni ajouter l'en-tête.

Les deux documentations s'accordent sur le mécanisme. Côté ASP.NET Core : écrire le jeton dans un cookie `XSRF-TOKEN` lisible par JavaScript, et l'attendre dans l'en-tête `X-XSRF-TOKEN`. Côté Angular : `HttpClient` lit par défaut le cookie `XSRF-TOKEN` et ajoute l'en-tête `X-XSRF-TOKEN` aux requêtes d'écriture vers une URL relative. Avec ces noms, l'interface n'a rien à configurer.

> **.NET 11 ajoute une protection automatique, et elle ne couvre pas l'API JSON de Chronos.** ASP.NET Core 11 enregistre de lui-même, dans toute application créée par `WebApplication.CreateBuilder`, un middleware qui compare les en-têtes `Sec-Fetch-Site` et `Origin` de la requête à son destinataire, sans jeton. Une écriture venue d'un autre site est marquée « invalide » ; mais le middleware ne répond pas lui-même : le refus (400) n'est prononcé que par un composant qui **lit un formulaire** (endpoint Minimal API qui lie un formulaire, action MVC protégée par antiforgery, Blazor SSR, ou tout code qui lit le formulaire de la requête). Un endpoint qui lit un corps JSON, comme tous ceux de Chronos, s'exécute malgré un verdict invalide. Une requête qui ne porte ni `Sec-Fetch-Site` ni `Origin` (`curl`, appel d'un serveur) est acceptée ; `same-site` est traité comme `cross-site`. Le mécanisme se désactive globalement par la clé `DisableCsrfProtection`, ou par endpoint avec `.DisableAntiforgery()` ou `[IgnoreAntiforgeryToken]` ; il n'y a aucune raison de le faire ici. Vous lirez donc, à propos de cette protection, des phrases vraies pour un formulaire et trompeuses pour Chronos. Le détail, constaté, est dans le tableau ci-dessous.

Ce que cela donne sur l'API de Chronos en .NET 11 (cookie de session valide ; les en-têtes `Origin` et `Sec-Fetch-Site` étrangers sont posés à la main avec `curl`, comme le ferait un navigateur sur une page piégée) :

| Écriture `POST /api/clients` | Avec le filtre de l'étape 5 | Sans le filtre (essai) |
| --- | --- | --- |
| cookie, sans jeton, sans `Origin` | 400 | 201 |
| cookie, sans jeton, `Origin` étranger et `Sec-Fetch-Site: cross-site` | 400 | **201** |
| cookie, jeton `X-XSRF-TOKEN` valide, `Origin` étranger | 201 | — |
| sans cookie | 401 | — |

La dernière colonne est celle qui compte : **sans le filtre, une écriture JSON venue d'un autre site passe**. Le middleware d'origine du framework n'y change rien, et la même requête envoyée à un endpoint qui lie un formulaire reçoit 400 (constaté aussi, avec et sans `UseAntiforgery()`). Quand `UseAntiforgery()` est présent, son verdict l'emporte sur celui du middleware d'origine : un formulaire sans jeton est refusé même s'il vient de la même origine. Chronos garde donc sa vérification : elle n'est pas redondante, c'est elle (avec `SameSite=Strict`, qui retient déjà le cookie sur les requêtes d'un autre site dans un navigateur) qui protège les écritures JSON. Ce que le framework fait en plus de lui-même ne concerne, dans Chronos, que les éventuels endpoints à formulaire, et il n'y en a aucun.

Documentation : [Prévenir les attaques CSRF](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-11.0) (section « Automatic CSRF protection in ASP.NET Core »), [Sécurité dans Angular](https://angular.dev/best-practices/security), [cookies `SameSite`](https://learn.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-11.0)

### Le rôle ouvre la fonction, le périmètre dit sur qui

Les quatre rôles (`Collaborateur`, `Manager`, `GestionnaireRh`, `Administrateur`) répondent à la question « cette personne a-t-elle accès à cette fonction ? ». Une **politique** d'autorisation l'exprime, et on l'attache à un endpoint.

Mais RG-50 et RG-51 posent une autre question : « sur **quelles données** ? ». Deux managers ont le même rôle et ne voient pas les mêmes fiches. Aucune politique par rôle ne peut le dire : la réponse dépend de la hiérarchie enregistrée en base. On la traite donc **dans la requête** : un service, `PerimetreUtilisateur`, fournit la liste des collaborateurs visibles, et chaque lecture est filtrée par cette liste.

Ce qui est hors périmètre n'apparaît pas dans les listes, et sa lecture directe renvoie 404 : la même requête filtrée sert aux deux, et l'API ne révèle pas qu'une fiche existe.

Documentation : [Sécurité des Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-11.0), [Autorisation par politiques](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-11.0)

### Tracer sans y penser

RG-53 concerne toutes les écritures. Si chaque endpoint devait renseigner « qui » et « quand », l'un d'eux l'oublierait. Un **intercepteur** EF Core s'exécute à chaque `SaveChanges` : il parcourt les entités ajoutées ou modifiées et renseigne lui-même les quatre propriétés de l'interface `ITracable`.

Documentation : [Intercepteurs](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)

## Pas à pas

### 1. Le compte utilisateur et ses tables

Ajoutez à `Directory.Packages.props` le paquet `Microsoft.AspNetCore.Identity.EntityFrameworkCore` en version 10.0.15, et référencez-le dans `Chronos.Infrastructure`. La version est celle d'EF Core, pas celle d'ASP.NET Core 11 : sans version, `dotnet package add` prend la `11.0.0`, et la compilation échoue sur `NU1605` (« Detected package downgrade: Microsoft.EntityFrameworkCore.Relational from 11.0.0 to 10.0.15 », rejoué avec le SDK 11.0.100). Créez-y `Identite/Utilisateur.cs` :

```csharp
public class Utilisateur : IdentityUser<int>
{
    public int? CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }
}
```

`IdentityUser<int>` fixe une clé `int`, comme le reste du modèle (par défaut, c'est une chaîne). Le compte **référence** un collaborateur sans se confondre avec lui : un administrateur peut n'être rattaché à aucune fiche, et `Chronos.Domain` continue de ne rien connaître d'Identity.

Faites hériter le contexte du contexte d'Identity :

```csharp
public class ChronosDbContext(DbContextOptions<ChronosDbContext> options)
    : IdentityDbContext<Utilisateur, IdentityRole<int>, int>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // … vos configurations existantes, puis :
        modelBuilder.Entity<Utilisateur>(u =>
        {
            u.HasOne(x => x.Collaborateur).WithMany().HasForeignKey(x => x.CollaborateurId).OnDelete(DeleteBehavior.Restrict);
            u.HasIndex(x => x.CollaborateurId).IsUnique();
        });
    }
}
```

`base.OnModelCreating` doit être appelé **en premier** : c'est lui qui déclare les tables d'Identity. L'index unique garantit qu'une fiche n'a qu'un compte. La migration viendra à l'étape 9, avec les colonnes de traçabilité.

### 2. Brancher Identity

Dans `Program.cs` :

```csharp
builder.Services.AddIdentityApiEndpoints<Utilisateur>(options => options.User.RequireUniqueEmail = true)
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<ChronosDbContext>()
    .AddClaimsPrincipalFactory<FabriqueDeClaims>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "chronos.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
```

- **`AddRoles`** ajoute la gestion des rôles, absente par défaut de `AddIdentityApiEndpoints`.
- **`HttpOnly`** : JavaScript ne peut pas lire le cookie. **`SameSite=Strict`** : il n'accompagne aucune requête venue d'un autre site ; l'interface et l'API partagent la même origine (module 13), ce réglage ne la gêne donc pas.
- **Huit heures glissantes** : une session inactive expire dans la journée.

**`Secure`** demande au navigateur de ne renvoyer le cookie qu'en HTTPS. Le réglage dépend de l'environnement, parce que l'API a été créée avec `--no-https` (module 03) et n'écoute jamais qu'en HTTP :

| | Développement | Production (module 23) |
| --- | --- | --- |
| Entre le navigateur et l'origine | HTTP, sur `localhost` | HTTPS, porté par le serveur frontal nginx |
| Ce que voit l'API | Une requête HTTP | Une requête HTTP aussi, relayée par nginx |
| Cookie de session | Sans `Secure` (`SameAsRequest`) | `Secure` **toujours** (`Always`), quoi que voie l'API |

Avec `SameAsRequest` en production, l'API, qui ne voit que du HTTP, émettrait un cookie de session sans `Secure` : d'où `Always`. À l'inverse, `Always` en développement gênerait les tests du module 11 : leur client HTTP ne renvoie un cookie `Secure` que vers une adresse `https://`.

Le cookie contient l'identité sous forme de *claims* (identifiant, adresse, rôles), chiffrés par le serveur. Ajoutons-y l'identifiant du collaborateur, pour ne pas le relire en base à chaque requête — `Securite/FabriqueDeClaims.cs`, dans `Chronos.Api` :

```csharp
public sealed class FabriqueDeClaims(UserManager<Utilisateur> userManager,
    RoleManager<IdentityRole<int>> roleManager, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<Utilisateur, IdentityRole<int>>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Utilisateur user)
    {
        var identite = await base.GenerateClaimsAsync(user);
        if (user.CollaborateurId is { } collaborateurId)
        {
            identite.AddClaim(new Claim(ClaimsChronos.CollaborateurId,
                collaborateurId.ToString(CultureInfo.InvariantCulture)));
        }

        return identite;
    }
}
```

`ClaimsChronos.CollaborateurId` est une constante (`"chronos:collaborateur_id"`). Créez de même une classe `Roles` qui porte les quatre noms de rôles en constantes, et un tableau `Roles.Tous` : un nom de rôle ne s'écrit jamais deux fois à la main.

Documentation : [Claims](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims?view=aspnetcore-11.0), [Configurer Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-11.0)

### 3. Tout protéger, n'exposer que la connexion

Toujours dans `Program.cs` :

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Politiques.GestionRh, politique => politique.RequireRole(Roles.GestionnaireRh))
    .AddPolicy(Politiques.Validation, politique => politique.RequireRole(Roles.Manager, Roles.GestionnaireRh));
```

La **politique de repli** (*fallback*) s'applique à tout endpoint qui ne déclare rien : un endpoint ajouté demain sera protégé sans que personne ait à y penser. On ouvre ce qui doit l'être, au lieu de fermer ce qu'on n'a pas oublié.

Après `app.UseStatusCodePages()` :

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/compte")
    .AllowAnonymous()
    .AddEndpointFilter(async (contexte, suivant) =>
        contexte.HttpContext.Request.Path.Equals("/api/compte/login", StringComparison.OrdinalIgnoreCase)
            ? await suivant(contexte)
            : TypedResults.NotFound())
    .MapIdentityApi<Utilisateur>();
```

Le filtre d'endpoint laisse passer la connexion et répond 404 à tout le reste : inscription, mot de passe oublié, jetons, gestion du compte. `AllowAnonymous()` est nécessaire pour se connecter, et sans danger pour les neuf autres routes, puisqu'elles ne répondent plus. C'est une **liste d'autorisation** : si une version future d'Identity ajoute un endpoint, il sera neutralisé d'office. Une nuance constatée : le corps de la requête est lu **avant** les filtres, si bien qu'une requête mal formée vers une route neutralisée répond 400 au lieu de 404 ; elle n'exécute rien pour autant.

La politique de repli ferme aussi le document OpenAPI et Scalar. Ils ne contiennent aucune donnée et ne sont publiés qu'en développement : rouvrez-les explicitement.

```csharp
app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference().AllowAnonymous();
```

> **L'alternative** est de ne pas appeler `MapIdentityApi` et d'écrire sa propre connexion avec `SignInManager.PasswordSignInAsync`. C'est légitime, et c'est ce qu'il faudra faire le jour où la connexion devra se comporter autrement. Tant que l'endpoint fourni convient, on ne le réécrit pas.

Créez `Compte/CompteEndpoints.cs` pour ce qu'Identity ne fournit pas :

```csharp
public record MoiDto(int? CollaborateurId, string? Nom, string? Prenom, string Email, IReadOnlyList<string> Roles);

public static class CompteEndpoints
{
    public const string CookieJetonCsrf = "XSRF-TOKEN";

    public static RouteGroupBuilder MapCompte(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/compte").WithTags("Compte");
        groupe.MapGet("/moi", Moi);
        groupe.MapPost("/logout", Deconnecter);
        return groupe;
    }

    private static async Task<Ok<MoiDto>> Moi(
        ClaimsPrincipal principal, IUtilisateurCourant utilisateur, ChronosDbContext db,
        IAntiforgery antiforgery, HttpContext contexte, CancellationToken ct)
    {
        var jetons = antiforgery.GetAndStoreTokens(contexte);
        contexte.Response.Cookies.Append(CookieJetonCsrf, jetons.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Secure = contexte.Request.IsHttps,
            Path = "/",
        });

        var identite = await db.Collaborateurs
            .Where(c => c.Id == utilisateur.CollaborateurId)
            .Select(c => new { c.Nom, c.Prenom })
            .FirstOrDefaultAsync(ct);

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).Order().ToList();
        return TypedResults.Ok(new MoiDto(utilisateur.CollaborateurId, identite?.Nom, identite?.Prenom,
            principal.FindFirstValue(ClaimTypes.Email) ?? "", roles));
    }

    private static async Task<Results<NoContent, BadRequest>> Deconnecter(
        SignInManager<Utilisateur> signInManager, HttpContext contexte, [FromBody] object? vide)
    {
        if (vide is null)
        {
            return TypedResults.BadRequest();
        }

        await signInManager.SignOutAsync();
        contexte.Response.Cookies.Delete(CookieJetonCsrf);
        return TypedResults.NoContent();
    }
}
```

- **`/moi`** dit à l'interface qui est connecté et avec quels rôles. Il remet aussi le jeton anti-CSRF (étape 5) : l'interface appelle `/moi` à son démarrage et juste après la connexion, c'est-à-dire exactement aux moments où ce jeton doit être émis. `IUtilisateurCourant` arrive à l'étape 7.
- **`Secure = contexte.Request.IsHttps`** : le cookie du jeton suit le schéma de la requête **tel que l'API le voit**. En développement, c'est HTTP. En production, ce sera HTTPS à condition que nginx transmette le schéma d'origine (`X-Forwarded-Proto`) et que l'API le lise (`UseForwardedHeaders`, qui n'est pas actif par défaut) : c'est le travail du [module 23](23-conteneuriser-l-application.md). Voir [ASP.NET Core derrière un serveur frontal](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-11.0).
- **`/logout`** reprend la forme recommandée par la documentation : la requête doit porter un corps JSON, même vide (`{}`).
- `MapCompte` suit la forme du module 03 : il renvoie son groupe, et ses gestionnaires sont des méthodes privées.

### 4. Adapter les tests, et constater le 401

Relancez les tests du module 11 : ils échouent tous avec 401. C'est le comportement voulu, et c'est celui d'une API : **aucune redirection** vers une page de connexion. Avec `AddIdentityApiEndpoints`, la réponse porte l'en-tête `WWW-Authenticate: Bearer` et un corps `ProblemDetails`. Depuis .NET 10, l'authentification par cookie seule se comporte de même sur les endpoints d'API ; les tutoriels qui interceptent `OnRedirectToLogin` pour « corriger » une redirection 302 sont périmés.

Donnez à la fabrique de quoi créer un compte et s'en servir. Dans `ChronosApiFactory`, créez les rôles à la fin de `InitializeAsync` (`RoleManager<IdentityRole<int>>.CreateAsync` pour chaque nom de `Roles.Tous`), puis ajoutez :

```csharp
    public const string MotDePasse = "Chronos-Test-2026!";

    public async Task<HttpClient> ClientConnecteAsync(string email, int? collaborateurId, params string[] roles)
    {
        using (var scope = Services.CreateScope())
        {
            var comptes = scope.ServiceProvider.GetRequiredService<UserManager<Utilisateur>>();
            if (await comptes.FindByEmailAsync(email) is null)
            {
                var compte = new Utilisateur { UserName = email, Email = email, CollaborateurId = collaborateurId };
                var creation = await comptes.CreateAsync(compte, MotDePasse);
                Assert.True(creation.Succeeded, string.Join(", ", creation.Errors.Select(e => e.Description)));
                await comptes.AddToRolesAsync(compte, roles);
            }
        }

        var client = CreateClient();
        var connexion = await client.PostAsJsonAsync("/api/compte/login?useSessionCookies=true",
            new { email, password = MotDePasse });
        connexion.EnsureSuccessStatusCode();

        var moi = await client.GetAsync("/api/compte/moi");
        moi.EnsureSuccessStatusCode();
        var jeton = moi.Headers.GetValues("Set-Cookie")
            .Select(cookie => cookie.Split(';')[0])
            .Single(cookie => cookie.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split('=', 2)[1];
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", Uri.UnescapeDataString(jeton));

        return client;
    }
```

Le client de test fait ce que fera l'interface : il se connecte par le vrai endpoint, conserve le cookie, appelle `/moi`, puis renvoie le jeton dans l'en-tête. Rien n'est contourné. Dans les classes du module 11, remplacez `factory.CreateClient()` par un client connecté en gestionnaire RH ; la classe de tests implémente alors `IAsyncLifetime` pour le créer.

> **Piège constaté : l'horloge pilotée pilote aussi l'authentification.** Le `FakeTimeProvider` du module 11 remplace l'horloge de toute l'application, cookie compris. Avec `?useCookies=true`, le cookie persistant porte une date d'expiration calculée à partir du 4 mai 2026 : pour le `HttpClient`, qui vit à la date réelle, il est déjà périmé, et la requête suivante reçoit 401. Les tests se connectent donc avec `?useSessionCookies=true`, dont le cookie n'a pas de date. Autre effet : un test qui avance l'horloge de plus de huit heures fait expirer la session ; il se reconnecte ensuite.

Écrivez le premier test de sécurité : un client non connecté reçoit 401 sur `GET /api/collaborateurs` ; `POST /api/compte/register` renvoie 404, connecté ou non, et ne crée aucun compte.

### 5. Le jeton anti-CSRF

Dans `Program.cs` :

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
```

Le système anti-CSRF pose son propre cookie, `HttpOnly`, en plus du cookie `XSRF-TOKEN` que vous écrivez dans `/moi`. `SameAsRequest` lui applique la règle vue à l'étape 3 : `Secure` dès que l'API voit une requête HTTPS.

Créez `Securite/FiltreAntiforgery.cs` :

```csharp
public sealed class FiltreAntiforgery(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var requete = context.HttpContext.Request;
        if (HttpMethods.IsGet(requete.Method) || HttpMethods.IsHead(requete.Method) || HttpMethods.IsOptions(requete.Method))
        {
            return await next(context);
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Jeton anti-CSRF absent ou invalide");
        }

        return await next(context);
    }
}
```

Puis rassemblez vos endpoints sous un groupe qui porte ce filtre :

```csharp
var api = app.MapGroup("").AddEndpointFilter<FiltreAntiforgery>();
api.MapCompte();
api.MapCollaborateurs();
api.MapProjets();
// … les autres ressources
```

Toute requête d'écriture (`POST`, `PUT`, `PATCH`, `DELETE`) doit désormais porter l'en-tête. La connexion reste hors du groupe : avant elle, il n'y a pas de session à détourner.

> **Pourquoi un filtre écrit à la main ?** `app.UseAntiforgery()` ne valide automatiquement que les endpoints qui lisent un **formulaire**, et seulement pour `POST`, `PUT` et `PATCH`. L'essai le confirme, en .NET 11 comme avant : un endpoint JSON marqué comme exigeant le jeton s'exécute quand même sans lui. Pour les autres cas, la documentation renvoie à l'appel explicite de `ValidateRequestAsync` ; c'est ce que fait le filtre, pour toutes les écritures. Le middleware d'origine de .NET 11 (voir « CSRF : la contrepartie du cookie ») ne le remplace pas : il ne fait refuser que les requêtes dont l'endpoint lit un formulaire. Le filtre s'ajoute donc à ce que le framework fait seul, il ne le double pas.

**Ce que l'interface devra faire** (module 19) tient en cinq lignes :

| Moment | Requête | Résultat |
| --- | --- | --- |
| Connexion | `POST /api/compte/login?useCookies=true`, corps `{ "email", "password" }` | 200 sans corps et cookie `chronos.session` ; 401 si les identifiants sont faux |
| Démarrage, et juste après la connexion | `GET /api/compte/moi` | 200 avec `{ collaborateurId, nom, prenom, email, roles }` et cookie `XSRF-TOKEN` ; 401 si personne n'est connecté |
| Toute écriture | En-tête `X-XSRF-TOKEN` égal au cookie `XSRF-TOKEN` | Angular l'ajoute seul aux URL relatives ; sinon 400 |
| Déconnexion | `POST /api/compte/logout`, corps `{}` | 204 ; les deux cookies sont supprimés |
| Partout | — | 401 : renvoyer vers `/connexion` ; 403 : action interdite ; 404 : hors périmètre ou inexistant |

Documentation : [Filtres d'endpoint](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-11.0)

### 6. Autoriser par rôle (RG-52)

Dans `CollaborateursEndpoints` :

```csharp
groupe.MapPost("/", Creer).RequireAuthorization(Politiques.GestionRh);
```

La fiche d'un collaborateur porte ses informations contractuelles : sa création, sa modification et son archivage reviennent au gestionnaire RH. Un utilisateur connecté sans ce rôle reçoit 403. Remarquez que le rôle `Administrateur` n'y figure pas : RG-52 ne le cite pas.

Testez les deux cas : un collaborateur reçoit 403, un gestionnaire RH reçoit 201 ; et le même gestionnaire RH reçoit 400 si le test retire l'en-tête `X-XSRF-TOKEN`.

### 7. Autoriser par périmètre (RG-50, RG-51)

L'intercepteur de traçabilité, dans `Chronos.Infrastructure`, aura lui aussi besoin de connaître l'utilisateur. Déclarez-y donc l'interface, dans `Tracabilite/IUtilisateurCourant.cs` :

```csharp
public interface IUtilisateurCourant
{
    string Identifiant { get; }
    int? CollaborateurId { get; }
    bool APourRole(string role);
}
```

Et son implémentation dans `Chronos.Api`, `Securite/UtilisateurCourant.cs` :

```csharp
public sealed class UtilisateurCourant(IHttpContextAccessor accesseur) : IUtilisateurCourant
{
    private ClaimsPrincipal? Principal => accesseur.HttpContext?.User;

    public string Identifiant => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "systeme";

    public int? CollaborateurId =>
        int.TryParse(Principal?.FindFirstValue(ClaimsChronos.CollaborateurId), CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    public bool APourRole(string role) => Principal?.IsInRole(role) ?? false;
}
```

`IHttpContextAccessor` est le moyen que recommande la documentation pour atteindre la requête en cours depuis un service. On l'enferme dans **une seule classe** : le reste du code dépend de `IUtilisateurCourant`, qui se remplace facilement dans un test. Hors requête (données initiales, tâche de fond), l'identifiant vaut `"systeme"`.

Créez `Securite/PerimetreUtilisateur.cs` :

```csharp
public sealed class PerimetreUtilisateur(IUtilisateurCourant utilisateur, ChronosDbContext db)
{
    public bool VoitTout =>
        utilisateur.APourRole(Roles.GestionnaireRh) || utilisateur.APourRole(Roles.Administrateur);

    public async Task<IReadOnlySet<int>> IdentifiantsVisiblesAsync(CancellationToken ct = default)
    {
        var visibles = new HashSet<int>();
        if (utilisateur.CollaborateurId is not { } moi)
        {
            return visibles;
        }

        visibles.Add(moi);

        var liens = await db.Collaborateurs
            .Where(c => c.ManagerId != null)
            .Select(c => new { c.Id, ManagerId = c.ManagerId!.Value })
            .ToListAsync(ct);
        var equipes = liens.ToLookup(l => l.ManagerId, l => l.Id);

        var aVisiter = new Queue<int>([moi]);
        while (aVisiter.TryDequeue(out var manager))
        {
            foreach (var membre in equipes[manager])
            {
                if (visibles.Add(membre))
                {
                    aVisiter.Enqueue(membre);
                }
            }
        }

        return visibles;
    }

    public async Task<bool> PeutVoirAsync(int collaborateurId, CancellationToken ct = default) =>
        VoitTout || (await IdentifiantsVisiblesAsync(ct)).Contains(collaborateurId);
}
```

L'équipe indirecte se calcule en chargeant les couples (collaborateur, manager) — deux entiers par personne — puis en descendant la hiérarchie en mémoire. Pour une société de quelques centaines de personnes, c'est une requête simple, identique sur PostgreSQL et SQL Server. Le périmètre vient de la **hiérarchie**, pas du rôle `Manager` : celui qui n'encadre personne ne voit que lui-même, quel que soit son rôle.

Enregistrez les services :

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUtilisateurCourant, UtilisateurCourant>();
builder.Services.AddScoped<PerimetreUtilisateur>();
```

Et filtrez la liste des collaborateurs, en ajoutant `PerimetreUtilisateur perimetre` aux paramètres du gestionnaire :

```csharp
        if (!perimetre.VoitTout)
        {
            var visibles = await perimetre.IdentifiantsVisiblesAsync(ct);
            requete = requete.Where(c => visibles.Contains(c.Id));
        }
```

Dans la lecture d'une fiche, commencez par `if (!await perimetre.PeutVoirAsync(id, ct)) return TypedResults.NotFound();`.

Le test décisif construit une hiérarchie A → B → C et une personne D sans lien : A voit A, B et C ; C ne voit que C ; le gestionnaire RH voit tout le monde ; A reçoit 404 sur la fiche de D.

### 8. RG-36 : valider la feuille d'un autre

Ajoutez à `PerimetreUtilisateur` :

```csharp
    public async Task<bool> PeutValiderPourAsync(int collaborateurId, CancellationToken ct = default)
    {
        // Sans fiche, pas de valideur à enregistrer : la validation est refusée.
        if (utilisateur.CollaborateurId is not { } moi || collaborateurId == moi)
        {
            return false;
        }

        return utilisateur.APourRole(Roles.GestionnaireRh)
            || await db.Collaborateurs.AnyAsync(c => c.Id == collaborateurId && c.ManagerId == moi, ct);
    }
```

Et protégez la validation sur trois niveaux :

```csharp
groupe.MapPost("/{id:int}/validation", Valider).RequireAuthorization(Politiques.Validation);
```

```csharp
        var feuille = await db.FeuillesDeTemps.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feuille is null || !await perimetre.PeutVoirAsync(feuille.CollaborateurId, ct))
        {
            return TypedResults.NotFound();
        }

        if (!await perimetre.PeutValiderPourAsync(feuille.CollaborateurId, ct))
        {
            return TypedResults.Forbid();
        }
```

La politique écarte ceux qui n'ont pas la fonction (403). Le périmètre cache les feuilles des autres équipes (404). La dernière vérification refuse sa propre feuille, ou celle d'un collaborateur dont on n'est pas le manager direct (403). Le type de retour devient `Results<Ok<FeuilleDeTempsDto>, NotFound, ForbidHttpResult>`.

L'API sait désormais qui l'appelle : le valideur, c'est `utilisateur.CollaborateurId`. Supprimez `ValiderFeuilleRequete` (module 10), le `ValideurId` de `RejeterFeuilleRequete` et le `CollaborateurId` de `CreerAbsenceRequete` : un identifiant fourni par le client serait un moyen d'agir au nom d'un autre.

### 9. Tracer (RG-53)

Dans `Chronos.Domain` :

```csharp
public interface ITracable
{
    DateTimeOffset CreeLe { get; }
    string CreePar { get; }
    DateTimeOffset? ModifieLe { get; }
    string? ModifiePar { get; }
}
```

RG-53 vise **toute** création, modification ou validation : faites-la implémenter par chaque entité que l'API écrit — `Collaborateur`, `FeuilleDeTemps`, `Absence` (son approbation est une validation), `Affectation`, `Projet`, `Client` et `JourFerie` —, avec des accesseurs `private set` : aucun code métier ne doit pouvoir réécrire la trace. `Contrat` et `SaisieDeTemps` n'en ont pas besoin : ils ne s'écrivent qu'à travers leur fiche ou leur feuille, qui renouvelle sa `Version` et porte donc la trace. Puis, dans `Chronos.Infrastructure`, `Tracabilite/TracabiliteInterceptor.cs` :

```csharp
public sealed class TracabiliteInterceptor(TimeProvider horloge, IUtilisateurCourant utilisateur)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Tracer(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Tracer(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Tracer(DbContext? contexte)
    {
        if (contexte is null)
        {
            return;
        }

        var maintenant = horloge.GetUtcNow();
        foreach (var entree in contexte.ChangeTracker.Entries<ITracable>())
        {
            if (entree.State == EntityState.Added)
            {
                entree.Property(nameof(ITracable.CreeLe)).CurrentValue = maintenant;
                entree.Property(nameof(ITracable.CreePar)).CurrentValue = utilisateur.Identifiant;
            }
            else if (entree.State == EntityState.Modified)
            {
                entree.Property(nameof(ITracable.ModifieLe)).CurrentValue = maintenant;
                entree.Property(nameof(ITracable.ModifiePar)).CurrentValue = utilisateur.Identifiant;
            }
        }
    }
}
```

- Les **deux** méthodes sont redéfinies : la trace ne dépend pas de la façon dont `SaveChanges` est appelé.
- L'heure vient de `TimeProvider` (UTC, comme l'exige le cahier des charges) : les tests la maîtrisent.
- `CreePar` reçoit **l'identifiant du compte**, pas un nom ni une adresse : la trace ne recopie pas de donnée personnelle.

Branchez-le dans l'enregistrement du contexte écrit au [module 07](07-deux-sgbd-un-seul-code.md). Deux choses changent, le reste est conservé tel quel :

```csharp
builder.Services.AddScoped<TracabiliteInterceptor>();

builder.Services.AddDbContext<ChronosDbContext>((services, options) =>
{
    _ = fournisseur switch
    {
        // … les trois branches du module 07, inchangées
    };

    options.AddInterceptors(services.GetRequiredService<TracabiliteInterceptor>());

    // … les données initiales du module 06, inchangées
});
```

- **`(services, options)`** : cette forme d'`AddDbContext` donne accès aux services de la requête en cours, donc à un intercepteur qui connaît l'utilisateur courant.
- **`AddInterceptors`** s'applique quel que soit le fournisseur : on l'écrit une fois, après le `switch`.

Les données initiales passent elles aussi par l'intercepteur : elles sont tracées au nom de `"systeme"`.

Limitez la longueur de `CreePar` et `ModifiePar` dans la configuration (64 caractères suffisent), puis créez la migration **dans chaque jeu**, sous la forme du module 07 :

```powershell
dotnet ef migrations add AjoutIdentiteEtTracabilite --project Chronos.Migrations.PostgreSql --startup-project Chronos.Api -- --Chronos:Fournisseur PostgreSql
```

```powershell
dotnet ef migrations add AjoutIdentiteEtTracabilite --project Chronos.Migrations.SqlServer --startup-project Chronos.Api -- --Chronos:Fournisseur SqlServer
```

Lisez-les avant de les appliquer : sept tables `AspNet…`, et quatre colonnes par entité tracée, avec les types propres à chaque SGBD. Le projet de démarrage reste l'API : c'est elle qui configure Identity. Sur une base qui contient déjà des lignes, celles-ci reçoivent les valeurs par défaut de la migration (`CreePar` vide, `CreeLe` au 1er janvier de l'an 1) : en développement, supprimez la base et réappliquez les migrations, pour que les données initiales soient tracées au nom de `"systeme"`.

Le test : un gestionnaire RH crée un collaborateur ; relu par `ChronosDbContext`, celui-ci porte `CreeLe` égal à `factory.Horloge.GetUtcNow()` et `CreePar` égal à l'identifiant du compte.

### 10. Ne montrer que le nécessaire

Un manager a besoin de savoir qui est dans son équipe, pas de connaître les dates ni la quotité du contrat : RG-52 les range parmi les informations contractuelles. Séparez en deux le `CollaborateurDto` du module 08 :

```csharp
public record CollaborateurDto(
    int Id, string Matricule, string Nom, string Prenom, string Email,
    CategorieCollaborateur Categorie, string Poste, int? ManagerId, string? ManagerNom,
    Guid Version);

public record CollaborateurRhDto(
    int Id, string Matricule, string Nom, string Prenom, string Email,
    CategorieCollaborateur Categorie, string Poste, int? ManagerId, string? ManagerNom,
    DateOnly DateEntree, DateOnly? DateSortie, decimal Quotite, Guid Version);
```

Les deux gardent `Version` : le jeton de concurrence du module 08 n'est pas une information contractuelle, et l'interface en a besoin pour toute modification.

Retenez ce que la liste réduite ne sait plus dire : qui a quitté la société. Elle sait encore filtrer les sortis, mais plus les désigner. Le manque n'apparaîtra qu'avec le premier écran qui s'en sert : le [module 16](16-dialoguer-avec-l-api.md) y reviendra, et complétera ces deux DTO d'un indicateur sans date.

La lecture d'une fiche renvoie `Results<Ok<CollaborateurRhDto>, Ok<CollaborateurDto>, NotFound>` : la version complète pour le gestionnaire RH et pour l'intéressé lui-même (`id == utilisateur.CollaborateurId`), la version réduite pour les autres. La liste renvoie toujours la version réduite. **Les champs absents ne sont pas masqués par l'interface : ils ne quittent pas le serveur.**

La même discipline vaut ailleurs : une absence porte un type (`MAL`), jamais un motif médical — il n'y a pas de champ pour cela, et il ne faut pas en ajouter. Et les journaux citent des identifiants (`Feuille {FeuilleId} validée par {UtilisateurId}`), pas des noms ni des adresses : un fichier de journal est copié, archivé et lu par bien plus de monde que la base.

### 11. Des comptes pour le développement

Les données initiales du module 06 n'ont pas de gestionnaire RH ; or il lui faut une fiche pour valider des feuilles et saisir les siennes. Ajoutez-en une à `DonneesInitiales` (Hugo Bernard, matricule `M0003`, `hugo.bernard@chronos.example`, avec un contrat), dans un bloc qui vérifie d'abord que le matricule n'existe pas : une base déjà alimentée la reçoit aussi.

Créez `Securite/ComptesDeDemonstration.cs` : une méthode `CreerAsync(IServiceProvider services, string motDePasse)` qui crée les rôles manquants, puis, pour quelques collaborateurs des données initiales, un compte relié à leur fiche par l'adresse électronique. Elle utilise les mêmes appels que la fabrique de tests (`RoleExistsAsync`, `FindByEmailAsync`, `CreateAsync`, `AddToRolesAsync`) et ne recrée pas ce qui existe. Prévoyez un compte par rôle : Camille (`Collaborateur`), Sophie (`Collaborateur`, `Manager`), Hugo (`Collaborateur`, `GestionnaireRh`) et un administrateur sans fiche. Appelez-la avant `app.Run()` :

```csharp
if (app.Environment.IsDevelopment() && app.Configuration["Chronos:Demo:MotDePasse"] is { Length: > 0 } motDePasseDemo)
{
    await ComptesDeDemonstration.CreerAsync(app.Services, motDePasseDemo);
}
```

Le mot de passe n'est écrit dans aucun fichier versionné : il vient des secrets de développement.

```powershell
dotnet user-secrets set "Chronos:Demo:MotDePasse" "<votre mot de passe>" --project Chronos.Api
```

Il doit respecter les règles par défaut d'Identity : six caractères au moins, avec majuscule, minuscule, chiffre et caractère spécial. Hors développement, ce code ne s'exécute pas : les comptes réels sont créés par un administrateur — le tout premier l'étant créé au démarrage à partir d'un secret, ce que le [module 25](25-exploiter-l-application.md) met en place —, et aucun environnement partagé ne doit connaître un mot de passe par défaut.

> **Piège constaté : les tests lisent aussi vos secrets.** L'hôte de test tourne en environnement `Development` et charge donc les secrets du poste. Ce code s'exécute alors au démarrage de l'hôte, avant le `MigrateAsync` de la fabrique : la base n'existe pas encore, et tous les tests échouent. Dans `ConfigureWebHost` de `ChronosApiFactory`, neutralisez la clé :
>
> ```csharp
> builder.UseSetting("Chronos:Demo:MotDePasse", "");
> ```

Documentation : [Secrets de développement](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-11.0)

## À vous

1. **Protéger le reste.** Appliquez les politiques aux autres ressources : qui crée un projet, une affectation, un jour férié ? Le cahier des charges (§ 2) répond. Écrivez pour chaque ressource un test « rôle insuffisant → 403 ».

2. **Le périmètre partout.** Filtrez par `IdentifiantsVisiblesAsync` les feuilles de temps, les absences, les affectations et le plan de charge. Les routes qui recevaient `collaborateurId` en paramètre doivent maintenant le vérifier : que répond `GET /api/absences?collaborateurId=…` pour un collaborateur hors périmètre ? Ajoutez la contrainte de RG-04 à vos tests : que ferait le parcours de la hiérarchie si A manageait B et B manageait A ?

3. **RG-36 jusqu'au bout.** Appliquez la même protection au rejet d'une feuille, puis à l'approbation et au refus d'une absence. Tracez la validation : après l'appel, `ModifieLe` et `ModifiePar` de la feuille, ou de l'absence, désignent le valideur.

4. **L'administrateur.** `VoitTout` donne aujourd'hui toutes les fiches à l'administrateur. Est-ce conforme à son rôle dans le cahier des charges, et à la minimisation ? Décidez, corrigez, et écrivez le test qui fixe votre décision.

5. **Les comptes.** L'inscription est neutralisée : il faut donc un endpoint `POST /api/comptes`, réservé à l'administrateur, qui crée un compte pour un collaborateur et lui attribue des rôles. Quel mot de passe initial ? Lisez ce qu'Identity propose pour [confirmer un compte et réinitialiser un mot de passe](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-11.0) avant de choisir.

6. **Session ou cookie persistant ?** Le contrat de l'API retient `useCookies=true`. Sur un poste partagé, que se passe-t-il quand le navigateur est fermé sans déconnexion ? Comparez avec `useSessionCookies=true` et proposez un choix argumenté pour un outil RH.

## Critères de réussite

- Sans connexion, toute route de l'API autre que la connexion répond 401, sans redirection ; en développement, `/openapi/v1.json` répond 200.
- `POST /api/compte/register` répond 404 et ne crée aucun compte.
- Une écriture sans en-tête `X-XSRF-TOKEN` répond 400 ; avec l'en-tête, elle réussit.
- Un collaborateur ne voit que sa fiche ; un manager voit son équipe directe et indirecte ; hors périmètre, 404.
- Seul le manager direct ou un gestionnaire RH valide une feuille ; personne ne valide la sienne (403).
- Toute entité `ITracable` créée ou modifiée par l'API porte l'heure de l'horloge et l'identifiant du compte.
- La fiche lue par un manager ne contient pas `dateEntree` ; lue par le gestionnaire RH, elle la contient.
- Aucun mot de passe dans `appsettings.json`, dans le code de l'API ni dans l'historique git.
- Tous les tests du module 11 passent encore, avec un client connecté.
- Comparez avec la solution de référence : `git diff module-11 module-12`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| Un endpoint `login` qui fabrique un JWT, rangé dans le `localStorage` | Explicitement déconseillé ; le jeton est lisible par tout script de la page. | Cookie `HttpOnly` avec Identity, ou un fournisseur OpenID Connect. |
| `MapIdentityApi` tel quel | L'inscription, la réinitialisation et la gestion du compte sont ouvertes à tous. | Neutraliser tout sauf la connexion, ou écrire sa propre connexion. |
| `.AllowAnonymous()` sur le résultat de `MapIdentityApi`, sans filtre | Ouvre aussi `/manage/info` aux anonymes. | La liste d'autorisation de l'étape 3. |
| Intercepter `OnRedirectToLogin` pour renvoyer 401 | Inutile depuis .NET 10 : une API répond déjà 401 ou 403 (constaté en .NET 11 : 401 sans redirection). | Supprimer ce code. |
| `RequireAuthorization()` ajouté endpoint par endpoint | Le jour où on l'oublie, l'endpoint est public. | Politique de repli, et `AllowAnonymous()` sur les exceptions. |
| Croire que `UseAntiforgery()` protège une API JSON | Il ne valide que les endpoints à formulaire, et pas `DELETE`. | Valider explicitement, comme le filtre de l'étape 5. |
| Croire que la protection automatique de .NET 11 protège une API JSON à cookie | Elle ne refuse que ce qui lit un formulaire : constaté, une écriture JSON `cross-site` passe sans le filtre. | Garder le filtre de l'étape 5. |
| `CookieSecurePolicy.SameAsRequest` pour le cookie de session en production | Derrière nginx, l'API ne voit que du HTTP : le cookie part sans `Secure`. | `Always` hors développement. |
| `app.UseHttpsRedirection()` ajouté « pour sécuriser » | L'API n'écoute qu'en HTTP (`--no-https`) ; c'est le serveur frontal qui porte le HTTPS. | Ne pas l'ajouter ; régler le HTTPS au module 23. |
| `SameSite=None` « pour que ça marche » | Le cookie accompagne les requêtes de n'importe quel site. | Même origine pour l'interface et l'API, et `Strict`. |
| Vérifier le rôle `Manager` pour montrer une fiche | Tous les managers voient alors toutes les équipes. | Filtrer la requête par le périmètre. |
| Masquer un champ dans l'interface seulement | La donnée est dans la réponse HTTP, lisible dans les outils du navigateur. | Un DTO qui ne la contient pas. |
| Renseigner `CreePar` dans chaque endpoint | Un oubli suffit à rompre la trace. | L'intercepteur. |
| `DateTime.UtcNow` dans l'intercepteur | Trace impossible à tester. | `TimeProvider`. |
| `IHttpContextAccessor` injecté dans dix services | Le code métier dépend de HTTP et ne se teste plus seul. | Une seule classe l'utilise, derrière `IUtilisateurCourant`. |
| Un mot de passe de démonstration dans `appsettings.json` ou dans les données initiales versionnées | Il finit en production, ou dans l'historique git. | Secrets de développement, et création réservée à l'environnement `Development`. |
| Journaliser l'adresse ou le nom de l'utilisateur | Les journaux deviennent un second fichier de données personnelles. | Journaliser des identifiants. |
| Tests connectés avec `useCookies=true` et une horloge pilotée | Le cookie paraît expiré au client de test : 401 incompréhensible. | `useSessionCookies=true` dans les tests. |

## Pour la séance

- 403 ou 404 pour une fiche hors périmètre : que révèle chacun, et à qui ? Y a-t-il des cas où 403 serait préférable ?
- Le périmètre est calculé à chaque requête en chargeant toute la hiérarchie. À partir de quelle taille d'entreprise changeriez-vous de méthode, et pour laquelle ?
- Les rôles voyagent dans le cookie. Que se passe-t-il si l'administrateur retire un rôle à quelqu'un qui est connecté ?
- La trace dit qui a modifié une fiche, pas ce qui a changé. Est-ce suffisant pour RG-53 ? Que coûterait un historique complet ?
- Qui doit pouvoir lire les colonnes de traçabilité ? Sont-elles elles-mêmes des données personnelles ?
- Qu'apporterait un fournisseur d'identité d'entreprise (OpenID Connect) à la place d'Identity, et que coûterait-il ?

## Pour aller plus loin

- [Autorisation fondée sur la ressource](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased?view=aspnetcore-11.0) : exprimer « peut valider cette feuille » comme une exigence d'autorisation, évaluée par `IAuthorizationService`.
- Requêtes récursives : calculer l'équipe indirecte dans la base, avec `WITH RECURSIVE` sur [PostgreSQL](https://www.postgresql.org/docs/18/queries-with.html) et `WITH` sur [SQL Server](https://learn.microsoft.com/en-us/sql/t-sql/queries/with-common-table-expression-transact-sql) — deux syntaxes différentes, à passer par [`FromSql`](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries).
- [Authentification multifacteur](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/mfa?view=aspnetcore-11.0) et [passkeys](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/passkeys/?view=aspnetcore-11.0) : ce qu'Identity sait faire au-delà du mot de passe.
- [Rôles](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-11.0) dans ASP.NET Core.
- [CSRF, vu par l'OWASP](https://owasp.org/www-community/attacks/csrf), et les [principes du RGPD](https://www.cnil.fr/fr/reglement-europeen-protection-donnees/chapitre2) (article 5 : minimisation des données).
- [Disparition des redirections de connexion pour les API](https://learn.microsoft.com/en-us/aspnet/core/breaking-changes/10/cookie-authentication-api-endpoints) en .NET 10.
- Suite dans le cours : le module 19 construit la connexion côté Angular à partir du tableau de l'étape 5 ; le module 25 revient sur la journalisation.
