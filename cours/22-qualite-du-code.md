# Module 22 — Qualité du code

> Pratiques vérifiées le 12 novembre 2026 — voir les référentiels [.NET 11](../docs/references/dotnet-11.md), [Angular 22](../docs/references/angular-22.md) et [GitHub Actions](../docs/references/github-actions.md).
> Les commandes de ce module ont été **exécutées le 12 novembre 2026** dans des conteneurs Linux, d'abord sur des projets d'essai (dont proviennent les exemples `Essai.cs`), puis **rejouées sur la solution de référence de Chronos** (état du module 21 : sept projets .NET, 348 tests ; application Angular, 262 tests et 33 parcours) avec le SDK .NET 10.0.404, EF Core 10.0.15, Node.js 24.22, Angular CLI 22.2.7, angular-eslint 22.6.0, ESLint 10.14 et Prettier 3.10. Les nombres et messages cités pour Chronos sont ceux qui ont été obtenus. **Non exécutés** : les étapes ajoutées au workflow, sur GitHub, et l'ensemble sous Windows. Si votre code diffère de la solution de référence, la liste des avertissements différera aussi : c'est attendu.
> **Aligné sur .NET 11 le 13 novembre 2026** (SDK 11.0.100). **Rejoués en .NET 11**, à partir de l'état du tag `module-21` auquel on a appliqué la plomberie .NET 11 : l'état des lieux de l'étape 1 (`dotnet build`, 0 avertissement ; `dotnet format --verify-no-changes` : 118 lignes `WHITESPACE` dans neuf fichiers et 17 lignes `CHARSET`, code de sortie 2) ; `Migrations/.editorconfig` ; `dotnet format` (neuf fichiers modifiés, dont huit de tests) ; les conventions de l'étape 3 ; l'étape 4 (`Essai.cs` : neuf avertissements ; solution : 266 avertissements, dont 243 `CA1707`, 6 `CA1816`, 5 `CA1304`, 5 `CA1311`, 5 `CA1862`, 1 `CA1305` et 1 `CA1725`, et les neuf `CS8618` de `dotnet format`) ; la correction d'`Essai.cs`, qui laisse `CA1848` et `CA1873` ; le réglage de `CA1707` (25 avertissements restent) ; l'erreur `CS8602` de l'étape 6 ; les exercices 1 et 2, sur l'état final de la solution (1 : le message `EnableGenerateDocumentationFile`, puis `CS1591` ; 2 : `latest-all` sur le domaine, 18 avertissements sans les suppressions de la solution, dont les trois `CA1032` de `RegleMetierException`). **Les avertissements de la solution sont les mêmes qu'en .NET 10**, mêmes règles, mêmes nombres. Deux points diffèrent du texte d'origine : les décomptes de règles par mode sont ceux du SDK 11.0.100 (le texte d'origine donnait ceux du SDK 10.0.404), et `dotnet format` appliqué aux `DbSet` donne, à ce stade (sans `TreatWarningsAsErrors`), 95 avertissements de nullabilité, là où le texte d'origine annonçait 80 erreurs (écart non revérifié en .NET 10). **Non rejoués en .NET 11** : le détail de l'étape 5 sur la solution complète (corrections de `CA1816`, `CA1305`, `CA1725`, `#pragma` des `ToLower`), l'avertissement `NU1901` à `NU1904` avec un paquet vulnérable, le job `api` de l'étape 7 sur GitHub, et tout ce qui concerne l'interface (étapes 8 à 12), qui n'est pas touchée par ce changement.

Chronos compile, ses tests passent, l'interface fonctionne. Ouvrez pourtant deux fichiers écrits à trois mois d'écart : les accolades ne sont pas au même endroit, ici une variable ne sert à rien, là une comparaison de chaînes dépend de la langue du serveur. Rien de tout cela ne fait échouer un test. Tout cela ralentit la relecture, et une partie finira en anomalie.

Une machine sait repérer ces défauts, sans fatigue et sans discussion. Ce module la met au travail : sur votre poste d'abord, puis dans l'intégration continue, pour que la règle soit la même pour tous.

## Objectifs

À la fin de ce module, vous saurez :

- distinguer formatage, style et analyse de qualité, et dire quel outil s'occupe de quoi ;
- régler les analyseurs de .NET pour toute la solution et traiter les avertissements comme des erreurs ;
- vérifier et corriger le formatage du code C# avec `dotnet format` ;
- ajouter ESLint à l'application Angular et vérifier son formatage avec Prettier ;
- décider du sort d'un avertissement : corriger, régler, reporter ou supprimer — et le justifier ;
- faire échouer l'intégration continue quand une règle n'est pas respectée.

## Prérequis

- Module 00 : le fichier `.editorconfig` existe à la racine de votre dépôt.
- Module 05 : le workflow `ci.yml` et son job `api` ; `src/api/Directory.Build.props` existe.
- Module 20 : le job `web` du workflow.
- La solution compile et tous les tests passent. On ne règle pas la qualité d'un code qui ne fonctionne pas.

## Règles de gestion

Aucune. Ce module ne change pas le comportement de l'application ; il change la façon dont son code est contrôlé.

## Notions

### Trois contrôles, trois outils

| Contrôle | Question posée | Côté .NET | Côté Angular |
| --- | --- | --- | --- |
| **Formatage** | Les espaces, retraits et sauts de ligne sont-ils réguliers ? | `dotnet format` | Prettier |
| **Style** | Le code suit-il les conventions de l'équipe ? | Règles `IDExxxx` | ESLint |
| **Qualité** | Le code contient-il un défaut probable ? | Règles `CAxxxx`, avertissements `CSxxxx` du compilateur | ESLint, compilateur TypeScript |

Le formatage se corrige tout seul, et personne ne devrait en débattre en relecture. La qualité demande de réfléchir. Les mélanger est la première cause d'abandon de ces outils.

### Les analyseurs de .NET

Le SDK contient des **analyseurs** : des programmes qui examinent votre code pendant la compilation. Vous n'avez rien à installer.

- Les règles **`CAxxxx`** (qualité) sont actives par défaut, mais seule une poignée produit un avertissement.
- Les règles **`IDExxxx`** (style) ne sont **pas** contrôlées par `dotnet build` par défaut : elles n'apparaissent que dans l'éditeur.

Deux propriétés changent cela. **`EnforceCodeStyleInBuild`** fait appliquer le style à la compilation. **`AnalysisLevel`** choisit un jeu de règles de qualité :

| Mode | Ce qu'il active | Règles en avertissement |
| --- | --- | --- |
| `default` | Le strict minimum. | une trentaine, d'après la documentation |
| `minimum` | Les règles jugées indispensables. | 98 |
| `recommended` | Davantage de règles. | 151 |
| `all` | Presque toutes. | 286 |

Les trois derniers nombres ont été relevés dans les fichiers de configuration du SDK 11.0.100 (ceux du SDK 10.0.404 donnaient 92, 145 et 280).

La valeur s'écrit `<version>-<mode>` : `latest-recommended` désigne les règles recommandées de la dernière version connue du SDK — c'est-à-dire, grâce à votre `global.json`, celles du SDK 11. Le sens de `latest` change donc avec le SDK : passer du SDK 10 au SDK 11 ajoute six règles à `recommended` (`CA1517`, `CA1876`, `CA1877`, `CA2026`, `CA2027`, `CA2028`). Aucune ne se manifeste sur Chronos (constaté), mais un projet qui avait zéro avertissement peut en avoir après une mise à jour du SDK ; `AnalysisLevel` à `10-recommended` fige le jeu de règles.

Documentation : [analyse du code dans .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview), [propriétés d'analyse](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#code-analysis-properties)

### Sévérité

Chaque règle a une **sévérité**, que vous pouvez modifier :

| Valeur | Effet à la compilation |
| --- | --- |
| `error` | Erreur : la compilation échoue. |
| `warning` | Avertissement : la compilation réussit, sauf si les avertissements sont traités comme des erreurs. |
| `suggestion` | Simple message ; trois points gris dans l'éditeur. |
| `silent`, `none` | Invisible ; `none` désactive complètement la règle. |

La sévérité se règle dans `.editorconfig`, règle par règle : `dotnet_diagnostic.CA1707.severity = none`.

Un fichier `.editorconfig` s'applique à son dossier et à ses sous-dossiers. S'il en existe plusieurs, le plus proche du fichier analysé l'emporte. On peut donc poser une règle générale à la racine et l'assouplir pour un dossier.

Documentation : [options de configuration](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options), [fichiers de configuration](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files)

### Traiter les avertissements comme des erreurs

Un avertissement qu'on peut ignorer sera ignoré. Dans une solution qui en affiche deux cents, personne ne voit le deux cent unième, qui était le seul important.

**`TreatWarningsAsErrors`** supprime la catégorie : il reste des erreurs, et le reste. Chaque règle active est alors une décision — on la respecte, ou on change explicitement sa sévérité.

Cela vaut aussi pour les **types référence nullables**, activés dans vos projets depuis le module 03 par `<Nullable>enable</Nullable>`. Le compilateur signale toute variable qui pourrait valoir `null` là où vous avez déclaré que c'était impossible ; avec `TreatWarningsAsErrors`, ce signalement arrête la compilation.

Documentation : [options du compilateur](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings), [types référence nullables](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types)

### ESLint, Prettier et TypeScript strict

Côté Angular, trois outils se partagent le travail.

- **Le compilateur TypeScript**, en mode strict. Depuis TypeScript 6, `strict` vaut `true` par défaut : `ng new` ne l'écrit plus dans `tsconfig.json`, et c'est normal. Les templates sont eux aussi vérifiés strictement par défaut.
- **ESLint**, pour le style et la qualité. La CLI Angular ne l'installe pas : `ng lint` ne fonctionne qu'après l'ajout du paquet **angular-eslint**, qui apporte les règles propres à Angular, templates compris.
- **Prettier**, pour le formatage. `ng new` l'installe et crée son fichier `.prettierrc`.

La documentation de Prettier résume la répartition : Prettier pour le formatage, le linter pour la qualité. angular-eslint suit la même ligne et ne maintient aucune règle de formatage.

Documentation : [`ng lint`](https://angular.dev/cli/lint), [angular-eslint](https://github.com/angular-eslint/angular-eslint), [Prettier et les linters](https://prettier.io/docs/integrating-with-linters), [`strict`](https://www.typescriptlang.org/tsconfig/strict.html)

## Pas à pas

Les étapes 1 à 7 se déroulent dans `src/api`, les étapes 8 à 12 dans `src/web`.

### 1. Faire l'état des lieux

```powershell
dotnet build --no-incremental
```

`--no-incremental` force une recompilation complète : sans lui, les projets déjà compilés ne réaffichent pas leurs avertissements. Notez le nombre annoncé en fin de sortie.

```powershell
dotnet format --verify-no-changes
```

Cette commande ne modifie rien. Elle liste ce que `dotnet format` changerait et, s'il y a quoi que ce soit, se termine par un code de sortie non nul — `2` lors de l'essai. Affichez-le en tapant `$LASTEXITCODE`.

Il y a presque toujours quelque chose. Sur la solution de référence : 118 lignes `WHITESPACE` dans neuf fichiers — surtout des objets anonymes écrits sur une ligne dans les tests, que le formateur veut voir à raison d'une propriété par ligne — et 17 lignes `CHARSET`, une par fichier de migration :

```text
…/Chronos.Api.Tests/AutorisationTests.cs(116,31): error WHITESPACE: Fix whitespace formatting. Replace 1 characters with '\n\s\s\s\s\s\s\s\s\s\s\s\s'.
…/Chronos.Migrations.PostgreSql/Migrations/20261112160157_Initiale.cs(1,1): error CHARSET: Fix file encoding.
```

**Le code que vous n'écrivez pas.** Les migrations du module 07 sont écrites par `dotnet ef`, dans un style qui n'est pas le vôtre : encodage avec BOM, espaces de noms à accolades, tableaux littéraux. On ne corrige pas à la main — ni par `dotnet format` — du code généré, qui sera réécrit à la prochaine migration : on le déclare comme tel, **avant** de formater. Dans chacun des deux projets de migrations, créez `Migrations/.editorconfig` :

```ini
[*.cs]
generated_code = true
```

Le formateur et les analyseurs ignorent alors ces fichiers, et eux seulement. Relancez `dotnet format --verify-no-changes` : les 17 lignes `CHARSET` ont disparu. Sans ce fichier, l'étape 2 réécrirait les dix-sept migrations, et l'étape 4 y ajouterait 25 avertissements (`IDE0161`, `CA1861`).

Documentation : [options de configuration de l'analyse de code](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options)

### 2. Formater une fois pour toutes

```powershell
dotnet format
```

Relisez le résultat avec `git diff` : seuls des espaces et des sauts de ligne doivent avoir changé — sur la solution de référence, neuf fichiers, dont huit de tests. Validez ensuite **ce changement seul**, dans un commit dont le message dit qu'il ne s'agit que de formatage. Mêlé à une modification de fond, il la rendrait illisible en relecture.

```powershell
dotnet format --verify-no-changes
```

Plus aucune ligne, et `$LASTEXITCODE` vaut `0`.

`dotnet format` a lu le `.editorconfig` de la racine du dépôt, deux dossiers plus haut : retrait de quatre espaces, saut de ligne final. C'est le même fichier que lit votre éditeur.

Documentation : [`dotnet format`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format)

### 3. Écrire les conventions de style

Ajoutez une section à la fin du `.editorconfig` de la racine :

```ini
[*.cs]
# Style : espaces de noms sur une ligne, accolades systématiques.
csharp_style_namespace_declarations = file_scoped:warning
csharp_prefer_braces = true:warning
```

Chaque ligne suit la forme `option = valeur:sévérité`. Relancez la vérification :

```powershell
dotnet format --verify-no-changes
```

Si un fichier s'écarte de ces deux conventions, il apparaît :

```text
…/Chronos.Domain/Projet.cs(2,11): warning IDE0161: Convert to file-scoped namespace
…/Chronos.Domain/Projet.cs(10,13): warning IDE0011: Add braces to 'if' statement.
```

`dotnet format` sait corriger ces deux règles : relancez-le, relisez, validez. Sur la solution de référence, rien n'apparaît : le code suivait déjà ces deux conventions, et les migrations sont exclues depuis l'étape 1.

Deux conventions suffisent pour comprendre le mécanisme. La [liste des options de style](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-style-rule-options) en compte des dizaines, et `dotnet new editorconfig` produit un fichier complet de près de 400 lignes qui peut servir de catalogue. N'en adoptez que ce que l'équipe a réellement décidé.

### 4. Élever le niveau d'analyse

Dans `src/api/Directory.Build.props`, ajoutez deux lignes au `PropertyGroup` existant :

```xml
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

Placées dans ce fichier, elles valent pour tous les projets de la solution.

Pour observer les analyseurs sur un terrain connu, créez temporairement `Chronos.Api/Essai.cs` :

```csharp
namespace Chronos.Api;

public static class Essai
{
    public static bool CommencePar(string code, string recherche, ILogger logger)
    {
        logger.LogInformation($"Recherche : {recherche}");
        return code.ToLower().StartsWith(recherche.ToLower());
    }
}
```

```powershell
dotnet build --no-incremental
```

Deux lignes de code, neuf avertissements. En voici quatre, abrégés :

```text
Essai.cs(7,31): warning CA2254: The logging message template should not vary between calls to 'LoggerExtensions.LogInformation(…)'
Essai.cs(7,9): warning CA1848: For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogInformation(…)'
Essai.cs(8,16): warning CA1310: The behavior of 'string.StartsWith(string)' could vary based on the current user's locale settings. […]
Essai.cs(8,21): warning CA1311: Specify a culture or use an invariant version to avoid implicit dependency on current culture
```

Chaque message se termine par l'adresse de la page qui explique la règle. **Lisez-la avant de toucher au code** : elle dit pourquoi la règle existe, comment la respecter et dans quels cas il est légitime de la désactiver.

Le reste de votre solution produit aussi ses avertissements. Ne les corrigez pas encore. Sur la solution de référence, sans `Essai.cs` : **266 avertissements**, dont 243 `CA1707`, 6 `CA1816`, 5 `CA1304`, 5 `CA1311`, 5 `CA1862`, 1 `CA1305` et 1 `CA1725`.

**Une surprise de `dotnet format`.** Relancez `dotnet format --verify-no-changes` : avec ces réglages, il exécute aussi les analyseurs, et signale neuf avertissements `CS8618` que la compilation ne montre pas :

```text
…/Chronos.Infrastructure/ChronosDbContext.cs(13,33): warning CS8618: Non-nullable property 'Collaborateurs' must contain a non-null value when exiting constructor. […]
```

Les propriétés `DbSet<T> … { get; set; }` du module 06 ne sont jamais nulles : EF Core les remplit lui-même, et un *suppresseur* fourni par EF Core retire cet avertissement à la compilation. `dotnet format` ne connaît pas ce suppresseur. **Ne lancez pas `dotnet format` pour « corriger »** : il rend les neuf propriétés nullables (`DbSet<Collaborateur>?`), et la compilation signale ensuite chaque usage : sur Chronos, 95 avertissements de nullabilité (`CS8604`, `CS8602`), qui deviendront des erreurs à l'étape 6. Écrivez plutôt chaque propriété comme le recommande la documentation d'EF Core, en lecture seule :

```csharp
    public DbSet<Collaborateur> Collaborateurs => Set<Collaborateur>();
```

Le modèle ne change pas : `dotnet ef migrations has-pending-model-changes` répond toujours `No changes have been made to the model since the last migration.` Retenez la leçon : `dotnet format` sans sous-commande applique aussi les correctifs des analyseurs ; relisez son `git diff` comme celui d'un collègue.

Documentation : [EF Core et les types référence nullables](https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types)

### 5. Décider du sort de chaque avertissement

Pour chaque règle qui se manifeste, quatre décisions sont possibles. Aucune n'est « ne rien faire ».

| Décision | Quand | Comment |
| --- | --- | --- |
| **Corriger** | La règle désigne un vrai défaut. C'est le cas le plus fréquent. | On modifie le code. |
| **Régler pour un dossier** | La règle est bonne en général, inadaptée à une partie du code. | Un `.editorconfig` dans ce dossier. |
| **Reporter** | La règle a raison, mais la correction est un chantier à part entière. | Sévérité `suggestion`, avec un commentaire qui dit quand ce sera traité. |
| **Supprimer pour une ligne** | Un cas précis, compris, légitime. | `#pragma warning disable`, avec la raison. |

**Corriger.** `CA1310` et `CA1311` signalent que le résultat de `ToLower()` et de `StartsWith()` dépend de la langue du système : la même recherche peut réussir sur votre poste et échouer sur un serveur réglé autrement. `CA2254` signale un message de journal construit par interpolation, que les outils d'exploitation ne pourront pas regrouper. Les deux se corrigent :

```csharp
        logger.LogInformation("Recherche : {Recherche}", recherche);
        return code.StartsWith(recherche, StringComparison.OrdinalIgnoreCase);
```

Il reste deux avertissements, `CA1848` et `CA1873`, sur la ligne de journalisation.

**Reporter.** Ces deux règles demandent de passer par l'attribut `[LoggerMessage]`. C'est justement le sujet du module 25. Si votre API journalisait déjà, on l'écrirait, à la fin de la section `[*.cs]` du `.editorconfig` de la racine :

```ini
# CA1848, CA1873 : journalisation par [LoggerMessage], traitée au module 25.
dotnet_diagnostic.CA1848.severity = suggestion
dotnet_diagnostic.CA1873.severity = suggestion
```

Chronos ne journalise encore rien : une fois `Essai.cs` supprimé, ces deux règles ne se manifestent nulle part. Ne les reportez donc pas — un report sans objet éteindrait le contrôle pour rien. Elles restent actives, et le module 25 écrira directement ses messages avec `[LoggerMessage]`.

**Régler pour un dossier.** Depuis le module 04, vos méthodes de test portent des noms en forme de phrase — `Un_projet_facturable_sans_client_est_refuse`. La règle `CA1707` réclame le retrait des tirets bas : 243 avertissements sur la solution de référence. Elle est utile pour le code de l'application, pas pour des noms de tests. Créez `Chronos.Domain.Tests/.editorconfig`, et le même fichier dans `Chronos.Api.Tests/` :

```ini
[*.cs]
# Les noms de tests sont des phrases : les tirets bas y sont voulus.
dotnet_diagnostic.CA1707.severity = none
```

Ce fichier ne contient pas `root = true` : il complète celui de la racine au lieu de le remplacer.

**Supprimer pour une ligne.** Si un seul appel était concerné, pour une raison durable, on le dirait sur place plutôt que pour tout le dépôt. En dernier recours, et jamais sans raison écrite :

```csharp
#pragma warning disable CA1848, CA1873 // Appel unique, hors de tout traitement répété.
        logger.LogInformation("Recherche : {Recherche}", recherche);
#pragma warning restore CA1848, CA1873
```

Appliquez maintenant cette grille à **tous** les avertissements de votre solution, jusqu'à ce que `dotnet build --no-incremental` annonce `0 Warning(s)`. Supprimez ensuite `Essai.cs`. Sur la solution de référence, après `CA1707` :

- **`CA1816`** (6, les `DisposeAsync` des classes de tests) : corrigé, par `GC.SuppressFinalize(this);` ;
- **`CA1305`** (un `ToString("yyyy-MM-dd")` dans un test) et **`CA1725`** (`OnModelCreating` doit garder le nom de paramètre `builder` de `IdentityDbContext`) : corrigés ;
- **`CA1304`, `CA1311`, `CA1862`** (les cinq `ToLower().Contains(…)` des recherches de collaborateurs et de projets) : supprimés par `#pragma`, avec la raison. Ce lambda n'est jamais exécuté en .NET : EF Core le traduit en SQL (`LOWER(…) LIKE …`). Les écritures que les règles proposent — `Contains(…, StringComparison.OrdinalIgnoreCase)`, `ToLower(CultureInfo.InvariantCulture)`, `ToLowerInvariant()` — ne sont traduites ni pour PostgreSQL ni pour SQL Server (vérifié), et `EF.Functions.Like` seul distingue les majuscules sur PostgreSQL, pas sur SQL Server.

Documentation : [supprimer un avertissement](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings)

### 6. Transformer les avertissements en erreurs

La solution ne produit plus d'avertissement : verrouillez cet état. Dans `Directory.Build.props` :

```xml
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsNotAsErrors>$(WarningsNotAsErrors);NU1901;NU1902;NU1903;NU1904</WarningsNotAsErrors>
```

Éprouvez la première ligne. Ajoutez à une classe du domaine une méthode qui ne se soucie pas de `null` :

```csharp
    public static int LongueurDuNom(Client? client)
    {
        return client.Nom.Length;
    }
```

```powershell
dotnet build
```

```text
error CS8602: Dereference of a possibly null reference.
```

La compilation échoue : ce qui était un avertissement qu'on pouvait laisser passer est devenu un obstacle. Retirez la méthode.

La seconde ligne mérite une explication. À chaque restauration, NuGet compare vos paquets à une base de vulnérabilités connues et émet les avertissements `NU1901` à `NU1904`. Avec `TreatWarningsAsErrors` seul, ils deviennent des erreurs — vérifié lors de l'essai avec un paquet vulnérable : `dotnet restore` échoue. Une faille publiée un mardi ferait donc échouer, le mercredi, la compilation d'un code que personne n'a modifié. `WarningsNotAsErrors` les maintient à l'état d'avertissements : visibles, mais non bloquants. C'est la ligne que propose la documentation de NuGet ; ce choix se discute, et il figure dans les questions de séance.

Documentation : [audit des paquets NuGet](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)

### 7. Faire respecter les règles par le job `api`

Dans `.github/workflows/ci.yml`, insérez une étape entre la restauration et la compilation :

```yaml
      - name: Restaurer les paquets
        run: dotnet restore --locked-mode

      - name: Vérifier le formatage
        run: dotnet format --verify-no-changes --no-restore

      - name: Compiler
        run: dotnet build --no-restore --configuration Release
```

- **`--verify-no-changes`** : la machine d'intégration continue ne corrige rien, elle constate. Un code de sortie non nul fait échouer l'étape, donc le job.
- **`--no-restore`** : la restauration vient d'être faite, en mode verrouillé ; inutile de la refaire.
- **L'étape de compilation ne change pas.** Les analyseurs et `TreatWarningsAsErrors` sont dans `Directory.Build.props` : la même commande applique désormais les mêmes règles sur votre poste et sur GitHub.

Aucune action n'a été ajoutée : le fichier de verrouillage des actions (module 05) reste valable.

### 8. Ajouter ESLint à l'application Angular

Placez-vous dans `src/web`.

```powershell
npx ng lint
```

La commande répond qu'aucune cible `lint` n'existe et suggère elle-même la suite :

```powershell
npx ng add angular-eslint
```

`ng add` cherche la version du paquet qui correspond à votre version d'Angular, vous demande de confirmer l'installation, puis configure le projet. Regardez ce qui a changé avec `git status` et `git diff` :

- **`package.json`** : quatre dépendances de développement (`angular-eslint`, `eslint`, `typescript-eslint`, `@eslint/js`) et un script `lint` ;
- **`angular.json`** : une cible `lint`, qui porte sur les fichiers `.ts` et `.html` de `src`, et `angular-eslint` en tête de `cli.schematicCollections` — `ng generate` consultera d'abord ses schémas (une nouvelle application ou bibliothèque recevrait ainsi sa configuration de lint) ;
- **`eslint.config.js`** : la configuration.

Les parcours du module 20 sont du code, eux aussi : ajoutez `"e2e/**/*.ts"` aux `lintFilePatterns` de la cible `lint`.

```powershell
npx ng lint
```

Sur une application tout juste créée, la commande répond `All files pass linting.` Sur la solution de référence, elle trouve six erreurs : quatre `@typescript-eslint/no-unused-vars` dans des tests et deux `@typescript-eslint/no-empty-function` (des `error: () => {}` dont le commentaire suivait l'accolade). Aucune règle d'accessibilité des templates ne se déclenche.

### 9. Lire la configuration et l'éprouver

Ouvrez `eslint.config.js`. En voici l'essentiel, tel que `ng add` l'a créé — les quatre lignes d'import et la règle `directive-selector`, bâtie comme sa voisine, sont omises :

```js
module.exports = defineConfig([
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
]);
```

Ce qu'il faut y lire :

- **Deux blocs**, un pour le TypeScript, un pour les templates HTML. `processInlineTemplates` soumet aussi au second les templates écrits dans les fichiers `.ts`.
- **`extends`** empile des jeux de règles tout faits : JavaScript, TypeScript, Angular. Le dernier, `templateAccessibility`, contrôle une partie de ce que vous avez fait à la main au module 21.
- **`rules`** ajuste règle par règle. Les deux règles posées par `ng add` imposent le préfixe `app` aux sélecteurs.
- Les sévérités d'ESLint sont `'off'`, `'warn'` et `'error'`.

Éprouvez-la avec un composant volontairement fautif, `src/app/essai.ts` :

```ts
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'liste-collaborateurs',
  template: `<img src="logo.png"><div (click)="recharger()">Recharger</div>`,
})
export class ListeCollaborateurs implements OnInit {
  donnees: any = [];

  ngOnInit(): void {}

  recharger() {
    var compteur = 0;
  }
}
```

```powershell
npx ng lint
```

La sortie, dont les colonnes ont été resserrées :

```text
   4:13  error  The selector should start with one of these prefixes: "app"       @angular-eslint/component-selector
   8:12  error  Unexpected any. Specify a different type                          @typescript-eslint/no-explicit-any
  10:3   error  Lifecycle methods should not be empty                             @angular-eslint/no-empty-lifecycle-method
  10:20  error  Unexpected empty method 'ngOnInit'                                @typescript-eslint/no-empty-function
  13:5   error  Unexpected var, use let or const instead                          no-var
  13:9   error  'compteur' is assigned a value but never used                     @typescript-eslint/no-unused-vars
   5:14  error  <img/> element must have a text alternative                       @angular-eslint/template/alt-text
   5:34  error  click must be accompanied by either keyup, keydown or keypress event for accessibility  @angular-eslint/template/click-events-have-key-events
   5:34  error  Elements with interaction handlers must be focusable              @angular-eslint/template/interactive-supports-focus

✖ 9 problems (9 errors, 0 warnings)
```

Neuf erreurs, dont trois dans le template : une image sans texte de remplacement, et un `<div>` cliquable qu'on ne peut ni atteindre ni actionner au clavier. La commande se termine par un code de sortie `1`.

`npx ng lint --fix` corrige ce qui peut l'être sans risque — ici, seulement le `var`. Le reste demande votre jugement. Supprimez le fichier d'essai, puis traitez les erreurs de votre application avec la grille de l'étape 5 : la décision « régler » s'écrit dans le bloc `rules`. Sur la solution de référence : les deux fonctions vides reçoivent leur commentaire **dans** leurs accolades ; les quatre variables « inutilisées » sont celles qu'un test écarte par déstructuration (`const { quotite, ...sansQuotite } = camille;`), ce que l'option `ignoreRestSiblings` de la règle autorise, commentaire à l'appui :

```js
      '@typescript-eslint/no-unused-vars': ['error', { ignoreRestSiblings: true }],
```

Documentation : [fichiers de configuration d'ESLint](https://eslint.org/docs/latest/use/configure/configuration-files)

### 10. Vérifier le formatage avec Prettier

`ng new` a créé `.prettierrc`. Ouvrez-le : il fixe cent caractères par ligne (`printWidth`), les guillemets simples (`singleQuote`) et l'analyseur de templates Angular pour les fichiers HTML. Vérifiez :

```powershell
npx prettier --check .
```

Prettier liste les fichiers qui s'écartent de son format et se termine par le code `1` s'il y en a. Constat de l'essai : sur une application qui sort de `ng new`, neuf fichiers sont signalés, dont `angular.json` et `package.json`. Les fichiers produits par la CLI ne respectent donc pas tous le format qu'elle configure ; ne soyez pas surpris. Sur la solution de référence, trois restaient à reprendre : `src/main.ts`, `tsconfig.app.json` et `tsconfig.spec.json`.

```powershell
npx prettier --write .
```

Comme à l'étape 2 : relisez avec `git diff`, puis validez ce changement seul. `npx prettier --check .` répond ensuite `All matched files use Prettier code style!`.

Prettier ignore de lui-même `node_modules` et ce que liste `.gitignore` — donc `dist`.

Documentation : [Prettier en ligne de commande](https://prettier.io/docs/cli)

### 11. Constater que TypeScript est strict

Rien à régler ici, mais il faut l'avoir vu. Créez `src/app/essai.ts` :

```ts
export function longueur(texte: string | null) {
  return texte.length;
}
```

```powershell
npx ng build
```

```text
✘ [ERROR] TS18047: 'texte' is possibly 'null'.
```

C'est le pendant exact de l'erreur `CS8602` de l'étape 6. Cherchez `"strict"` dans `tsconfig.json` : il n'y est pas, et le contrôle s'applique quand même. Supprimez le fichier d'essai.

### 12. Faire respecter les règles par le job `web`

Dans le job `web` de `ci.yml`, ajoutez deux étapes juste après celle qui exécute `npm ci`, avant les tests :

```yaml
      - name: Vérifier le formatage
        run: npx prettier --check .

      - name: Analyser le code
        run: npx ng lint
```

Ces deux contrôles durent quelques secondes. Placés avant les tests et la construction, ils font échouer le job au plus tôt, pour la raison la moins coûteuse à corriger.

Poussez, ouvrez une *pull request* et vérifiez que les deux jobs sont verts. Les vérifications obligatoires du module 05 portent sur les jobs : elles couvrent désormais ces nouvelles étapes sans autre réglage.

## À vous

1. **Les `using` inutiles.** La règle `IDE0005` les signale. Donnez-lui la sévérité `warning` dans `.editorconfig`, puis compilez. Un message inattendu apparaît : lisez-le en entier, suivez le lien qu'il donne, et appliquez ce qu'il demande dans `Directory.Build.props`. Un second problème surgit alors ; réglez-le avec la propriété `NoWarn`, pour un seul identifiant, et expliquez pourquoi cette suppression-là est acceptable.

2. **Un cran plus haut pour le domaine.** `Chronos.Domain` est le cœur de l'application. Dans son seul fichier `.csproj`, passez `AnalysisLevel` à `latest-all`. Combien d'avertissements nouveaux ? Appliquez la grille de l'étape 5 à chacun. Lors de l'essai, `RegleMetierException` déclenchait `CA1032` : lisez la page de cette règle et décidez.

3. **Voir la CI refuser.** Sur une branche, introduisez trois défauts dans trois commits successifs : un fichier C# mal indenté, une variable C# inutilisée, un `any` dans un composant. Ouvrez une *pull request*. Pour chaque défaut, quelle étape de quel job échoue ? Lequel aurait été arrêté le plus tôt ? Corrigez sans désactiver aucune règle.

4. **Avertissement ou erreur ?** Dans `eslint.config.js`, donnez à `@typescript-eslint/no-explicit-any` la sévérité `'warn'`, ajoutez un `any` quelque part et lancez `npx ng lint`, puis `npx ng lint --max-warnings 0`. Comparez les codes de sortie. Laquelle des deux commandes mettriez-vous dans le workflow, et quel rapport avec `TreatWarningsAsErrors` ?

5. **Des commandes qu'on retient.** Ajoutez à `src/web/package.json` deux scripts, `format` et `format:check`. Utilisez le second dans le workflow. Qu'est-ce qui est préférable dans un workflow : `npm run format:check` ou `npx prettier --check .` ? Donnez un argument dans chaque sens ; l'étape du module 13 sur `devEngines` en fournit un.

6. **Écrire la règle du jeu.** Recherchez dans tout le dépôt les `#pragma warning disable`, les `eslint-disable` et les sévérités `none` ou `'off'`. Pour chacun : la raison est-elle écrite, et est-elle encore vraie ? Rédigez ensuite, dans le `README.md`, une section « Qualité » d'une dizaine de lignes : ce que la CI vérifie, comment lancer les mêmes contrôles sur son poste, et dans quels cas l'équipe accepte une suppression.

## Critères de réussite

- Dans `src/api`, `dotnet format --verify-no-changes` se termine avec le code `0` et `dotnet build --no-incremental` annonce `0 Warning(s)` et `0 Error(s)`.
- Une variable inutilisée, ou un déréférencement possible de `null`, fait **échouer** `dotnet build`.
- Dans `src/web`, `npx prettier --check .` et `npx ng lint` se terminent avec le code `0`.
- Tous les tests passent encore : ce module n'a changé aucun comportement.
- Une *pull request* contenant un fichier mal formaté ou une règle enfreinte a ses jobs `API` ou `Web` en échec, à l'étape attendue.
- Chaque sévérité abaissée et chaque `#pragma` porte un commentaire qui en donne la raison.
- Le formatage initial du code est isolé dans ses propres commits.
- Comparez avec `git diff module-21 module-22` dans le dépôt du cours : vos conventions peuvent différer de celles de la solution de référence ; vous devez pouvoir les justifier.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `<TreatWarningsAsErrors>` recopié dans chaque `.csproj` | Le prochain projet créé l'oubliera. | Une fois, dans `Directory.Build.props`. |
| `<NoWarn>` sur une liste de règles, ou toute une catégorie mise à `none`, « pour que ça passe » | Le contrôle est éteint pour toujours, pour tout le monde, sans explication. | Une décision par règle, commentée. |
| Activer `latest-all` et `TreatWarningsAsErrors` d'un coup sur un code existant | Des centaines d'erreurs ; on renonce le jour même. | Monter par paliers : formatage, puis `recommended`, puis erreurs. |
| Le paquet `Microsoft.CodeAnalysis.FxCopAnalyzers`, des fichiers `.ruleset` | Paquet déprécié, format déprécié. | Les analyseurs du SDK et `.editorconfig`. |
| `dotnet format` sans `--verify-no-changes` dans le workflow | Les fichiers sont corrigés sur une machine qui sera détruite : le job est vert et le dépôt reste mal formaté. | `--verify-no-changes`. |
| Lancer `dotnet format` pour faire taire `CS8618` sur les `DbSet<T> { get; set; }` | Il les rend nullables : 95 avertissements de nullabilité sur les usages, des erreurs dès l'étape 6 (constaté en .NET 11). | `public DbSet<T> X => Set<T>();` : étape 4. |
| Formater avant d'avoir déclaré les migrations comme code généré | `dotnet format` réécrit les dix-sept migrations. | `Migrations/.editorconfig` d'abord : étape 1. |
| Formatage et modification de fond dans le même commit | La relecture devient impossible. | Un commit de formatage à part. |
| Déclarer `IDE0005` en `warning` sans autre réglage | La compilation réclame `GenerateDocumentationFile` (constaté) : voir l'exercice 1. | Suivre le message. |
| `TreatWarningsAsErrors` sans exception pour `NU1901` à `NU1904` | Une vulnérabilité publiée fait échouer la restauration sans qu'aucun commit n'ait eu lieu (constaté). | Décider explicitement : étape 6. |
| TSLint, `tslint.json` | TSLint est déprécié et son dépôt archivé depuis 2021. | ESLint avec angular-eslint. |
| `.eslintrc.json` | angular-eslint ne prend plus en charge que la configuration « à plat ». | `eslint.config.js`. |
| S'attendre à ce que `ng lint` fonctionne sur un projet neuf | La CLI ne configure aucun linter. | `ng add angular-eslint`. |
| Ajouter `"strict": false` pour faire taire le compilateur | On renonce au contrôle le plus rentable de TypeScript. | Corriger le type. |
| `// eslint-disable` en tête de fichier, `any` pour gagner du temps | Tout le fichier, ou toute la donnée, échappe au contrôle. | Désactiver une règle, sur une ligne, avec la raison. |
| Des règles de formatage dans ESLint à côté de Prettier | Les deux outils se contredisent. | Prettier formate, ESLint analyse. |

## Pour la séance

- Qu'est-ce qu'une relecture de code humaine devrait encore regarder, une fois ces outils en place ? Et que ne devrait-elle plus jamais commenter ?
- `TreatWarningsAsErrors` gêne aussi pendant qu'on écrit : une variable pas encore utilisée bloque la compilation. Faut-il ne l'activer que dans l'intégration continue ? Que perd-on alors ?
- Une vulnérabilité publiée dans un paquet doit-elle faire échouer la compilation ? Le cours répond non, et laisse un avertissement. Qui le lira ? Que mettriez-vous en place pour qu'il ne soit pas oublié ?
- Si vous aviez dû abaisser `CA1848` en attendant le module 25, comment vous assureriez-vous qu'un report ne devienne pas définitif ?
- Les règles d'accessibilité d'angular-eslint ont-elles trouvé quelque chose que vos vérifications du module 21 avaient manqué ? Que ne pourront-elles jamais vérifier ?
- Deux développeurs préfèrent deux styles différents. Comment une équipe choisit-elle, et quelle importance a le choix lui-même ?
- Sur une application de 200 000 lignes écrite sans aucun de ces outils, par où commenceriez-vous ?

## Pour aller plus loin

- [Liste des règles de qualité](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview) : la page d'ensemble donne les règles actives par défaut jusqu'en .NET 10 (elle n'a pas encore d'onglet .NET 11 au 13 novembre 2026) et renvoie au catalogue complet.
- [Fichiers de configuration globaux](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) : `.globalconfig`, pour régler les analyseurs par projet plutôt que par dossier.
- [`Directory.Build.props`](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory) : comment MSBuild trouve ce fichier, et comment en combiner plusieurs niveaux.
- [angular-eslint](https://github.com/angular-eslint/angular-eslint) : `ng add angular-eslint --tseslint-preset=strictTypeChecked` active des règles qui s'appuient sur les types.
- [Guide de style d'Angular](https://angular.dev/style-guide) : les conventions que plusieurs règles d'angular-eslint font respecter.
- Suite dans le cours : le module 23 conteneurise l'application ; le module 25 ajoute la journalisation, directement par `[LoggerMessage]`, sous le contrôle de `CA1848` et `CA1873` restées actives.
