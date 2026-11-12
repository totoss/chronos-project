# Module 04 — Modéliser le domaine en C#

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 10](../docs/references/dotnet-10.md).
> Le code et les commandes de ce module ont été **exécutés le 12 novembre 2026** dans un conteneur Linux `mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.404, C# 14, `xunit.v3.templates` et `xunit.v3.mtp-v2` 4.0.3) : les tests du pas-à-pas et ceux de l'exercice 2 passent avec `dotnet test` (17 tests). Les entités, celles des exercices comprises, ont été enregistrées puis relues par EF Core 10.0.15 sans retouche (essai avec le fournisseur SQLite). **Non exécutés** : les commandes sous Windows même, et le lancement des tests depuis un éditeur. Signalez au mentor tout écart constaté.

À la fin du module 03, l'API acceptait un projet facturable sans client, ou un projet qui se termine avant d'avoir commencé. Le cahier des charges interdit les deux. Où écrire ces interdictions ?

Pas dans l'API : demain, un import de fichier ou un traitement de nuit créera aussi des projets, et il faudrait tout réécrire. On les écrit dans les objets eux-mêmes. Un `Projet` qui **ne peut pas exister** dans un état interdit n'a pas besoin d'être surveillé par ceux qui l'utilisent. C'est le rôle du **domaine** : un projet C# qui ne contient que le métier, et qui ne dépend de rien.

Et puisque ces règles sont le cœur de l'application, on prouve qu'elles tiennent : ce module écrit aussi vos premiers tests automatisés.

## Objectifs

À la fin de ce module, vous saurez :

- écrire une entité qui protège ses invariants : constructeur qui valide, *setters* privés, méthodes qui portent le vocabulaire du métier ;
- choisir entre une classe, un `record` et une structure, et utiliser les types qui décrivent exactement une donnée (`DateOnly`, énumération, `decimal`, référence nulle ou non) ;
- signaler une règle de gestion violée par une exception qui porte son numéro ;
- créer un projet de tests xUnit v3, écrire des faits et des théories, lancer `dotnet test` et lire un échec ;
- dire quelles règles une entité peut vérifier seule, et lesquelles ont besoin de la base.

## Prérequis

- Module 03 : la solution existe dans `src/api`, avec `global.json`, `Directory.Build.props`, `Directory.Packages.props` et l'API des projets.
- Module 02 : vous avez en tête le modèle du domaine et les règles `RG-01` à `RG-13`.
- Vous savez ce que sont une classe, un constructeur et une exception dans un langage objet.

## Règles de gestion

| Règle | Dans ce module |
| --- | --- |
| **RG-11** — un projet facturable a un client, un projet interne n'en a pas | Vérifiée par `Projet` (pas à pas). |
| **RG-12** — la fin d'un projet ne précède pas son début | Vérifiée par `Projet` (pas à pas). |
| **RG-13** — ni affectation ni saisie sur un projet terminé ou annulé | `Projet` fournit la vérification (pas à pas) ; les affectations et les saisies qui l'appelleront arrivent aux modules 09 et 10. |
| **RG-02** — la date de sortie ne précède pas la date d'entrée | Vérifiée par `Collaborateur` (exercice 4). |
| **RG-03** — la quotité est comprise entre 10 % et 100 % | Vérifiée par `Contrat` (exercice 5). |
| **RG-01**, **RG-10** — unicité du matricule, de l'adresse électronique, du code de projet | **Pas ici.** Un projet ne connaît pas les autres projets : seule la base peut garantir l'unicité ([module 06](06-persister-avec-ef-core.md)). |
| **RG-04** — nul n'est son propre manager, même par une chaîne | **Pas ici.** Il faut remonter la hiérarchie, donc lire d'autres collaborateurs en base. |
| **RG-05** — un collaborateur qui a des données liées est archivé, pas supprimé | **Pas ici.** Savoir s'il « possède des saisies, des affectations ou des absences » demande d'interroger la base (module 06). |

Retenez la ligne de partage : une entité vérifie ce qu'elle peut décider **avec ses seules données**.

## Notions

### Invariant et encapsulation

Un **invariant** est une propriété vraie pendant toute la vie d'un objet. « La date de fin d'un projet ne précède pas sa date de début » en est un.

Pour le garantir, l'objet ne doit laisser aucune porte ouverte :

- le **constructeur** refuse de créer un objet invalide ;
- les **propriétés** se lisent de partout mais ne se modifient que de l'intérieur (`private set`) ;
- les changements passent par des **méthodes** nommées comme le métier les nomme — `Demarrer()`, pas `Statut = ...` — qui vérifient avant d'agir.

On parle d'**entité** pour un objet qui a une identité (son `Id`) et un cycle de vie : deux projets de même libellé restent deux projets.

Documentation : [Propriétés](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties), [Constructeurs](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors)

### Classe, `record` ou structure ?

C# offre trois façons de déclarer un type. Chronos utilise les trois, pour trois besoins différents.

| Type de Chronos | Déclaration | Pourquoi |
| --- | --- | --- |
| `Projet` | `class` | Une entité : elle a une identité, change d'état au fil du temps, et deux variables peuvent désigner le même projet. |
| `ProjetDto` (module 03) | `record` | Des données à transporter : jamais modifiées après création, et égales quand leurs valeurs sont égales. |
| `Periode` | `readonly record struct` | Une petite valeur sans identité, comme un nombre : « du 4 au 8 mai » n'existe pas en deux exemplaires. Copiée plutôt que partagée, jamais nulle par accident. |

Le mot `record` demande au compilateur d'écrire pour vous l'égalité par valeur et un affichage lisible. `struct` fait du type une valeur plutôt qu'une référence. `readonly` interdit toute modification après construction.

Documentation : [Classes, structures et records](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/), [Records](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records)

### Des types qui décrivent exactement la donnée

Un type bien choisi rend une erreur impossible, là où une vérification ne ferait que la détecter.

- **`DateOnly`** pour une date métier. Le cahier des charges le demande : un jour de calendrier, sans heure ni fuseau. Avec un `DateTime`, un décalage horaire peut faire glisser le 4 mai au 3.
- **Une énumération** pour une liste fermée de valeurs : `StatutProjet` ne peut pas valoir `"en cour"`.
- **`decimal`** pour une quotité ou un taux : `0.80m` vaut exactement 0,80. Avec `double`, `0.1 + 0.2` ne vaut pas `0.3`.
- **`string` ou `string?`**. La ligne `<Nullable>enable</Nullable>` de `Directory.Build.props` active les *références nullables* : `string` promet « jamais nul », `string?` annonce « peut manquer », et le compilateur signale chaque endroit où la promesse n'est pas tenue. `DateOnly? DateFin` dit de la même façon qu'un projet peut ne pas avoir de date de fin.

Documentation : [`DateOnly`](https://learn.microsoft.com/en-us/dotnet/standard/datetime/how-to-use-dateonly-timeonly), [Références nullables](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references), [Énumérations](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum)

### Trois façons de dire non

Une entité qui refuse lève une exception. Le type de l'exception dit **qui** s'est trompé.

| Exception | Signifie | Exemple |
| --- | --- | --- |
| `RegleMetierException` | Une règle numérotée du cahier des charges est violée. L'utilisateur peut corriger. | Projet facturable sans client (`RG-11`). |
| `ArgumentException` | L'appelant a transmis une valeur qui n'a aucun sens : c'est un défaut du programme. | Code de projet vide. |
| `InvalidOperationException` | L'opération n'est pas possible dans l'état actuel de l'objet. | Terminer un projet qui n'a pas démarré. |

La première est la nôtre. Elle porte le numéro de la règle, pour que l'API puisse plus tard le renvoyer au client ([module 08](08-une-api-robuste.md)) et pour que les tests vérifient **quelle** règle a parlé.

Documentation : [Bonnes pratiques des exceptions](https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)

### Test unitaire

Un **test unitaire** est une méthode qui construit une situation, exécute un comportement et vérifie le résultat. Il ne touche ni au réseau ni à la base : il s'exécute en quelques millisecondes, et on le relance à chaque modification.

Avec **xUnit**, la bibliothèque de tests retenue pour le cours :

- un **fait** (`[Fact]`) vérifie un cas ;
- une **théorie** (`[Theory]`) vérifie la même affirmation sur plusieurs jeux de données (`[InlineData]`) ;
- `Assert` porte les vérifications : `Assert.Equal`, `Assert.Throws`, `Assert.Null`…

Le cours utilise **xUnit v3**. Un projet de tests v3 est un exécutable, et `dotnet test` le pilote par **Microsoft.Testing.Platform**, le lanceur de tests actuel de .NET.

Documentation : [Débuter avec xUnit v3](https://xunit.net/docs/getting-started/v3/getting-started), [`dotnet test`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)

### Et C# 14 ?

.NET 10 apporte C# 14. Ce module n'en a pas besoin : une entité s'écrit avec des constructions stables depuis plusieurs versions du langage. Ne cherchez pas à placer une nouveauté pour elle-même. Une seule touche au sujet, le mot-clé `field` ; elle est signalée dans « Pour aller plus loin ».

Documentation : [Nouveautés de C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)

## Pas à pas

Toutes les commandes se lancent depuis `src/api`.

### 1. Créer le projet du domaine

```powershell
dotnet new classlib --name Chronos.Domain
```

```powershell
dotnet sln add Chronos.Domain
```

Supprimez le fichier `Class1.cs` généré. `Directory.Build.props` fournit déjà la version de .NET et les réglages : réduisez `Chronos.Domain.csproj` à

```xml
<Project Sdk="Microsoft.NET.Sdk">

</Project>
```

Ce fichier vide est une décision d'architecture : le domaine ne référence **aucun** paquet et **aucun** autre projet. Ni ASP.NET Core, ni base de données. Il doit le rester.

### 2. Les énumérations et l'exception métier

Un type par fichier, le fichier portant le nom du type. `Chronos.Domain/NatureProjet.cs` :

```csharp
namespace Chronos.Domain;

public enum NatureProjet { Facturable, Interne }
```

Écrivez de même `StatutProjet.cs`, avec les valeurs `EnPreparation`, `EnCours`, `Termine`, `Annule`. Ce sont les énumérations du module 03 : elles quitteront l'API à l'étape 10.

`Chronos.Domain/RegleMetierException.cs` :

```csharp
namespace Chronos.Domain;

public class RegleMetierException(string codeRegle, string message) : Exception(message)
{
    public string CodeRegle { get; } = codeRegle;
}
```

Les parenthèses après le nom de la classe déclarent un *constructeur principal* : ses paramètres sont utilisables dans tout le corps de la classe. `message` est transmis à la classe de base `Exception`, qui l'expose par sa propriété `Message`.

### 3. L'entité `Projet` : un constructeur et des *setters* privés

`Chronos.Domain/Projet.cs` :

```csharp
namespace Chronos.Domain;

public class Projet
{
    public Projet(
        string code,
        string libelle,
        NatureProjet nature,
        int? clientId,
        DateOnly dateDebut,
        DateOnly? dateFin,
        int budgetJours,
        int responsableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(libelle);
        ArgumentOutOfRangeException.ThrowIfNegative(budgetJours);

        Code = code.Trim();
        Libelle = libelle.Trim();
        Nature = nature;
        ClientId = clientId;
        DateDebut = dateDebut;
        DateFin = dateFin;
        BudgetJours = budgetJours;
        ResponsableId = responsableId;
        Statut = StatutProjet.EnPreparation;
    }

    public int Id { get; private set; }
    public string Code { get; private set; }
    public string Libelle { get; private set; }
    public NatureProjet Nature { get; private set; }
    public int? ClientId { get; private set; }
    public DateOnly DateDebut { get; private set; }
    public DateOnly? DateFin { get; private set; }
    public int BudgetJours { get; private set; }
    public int ResponsableId { get; private set; }
    public StatutProjet Statut { get; private set; }
}
```

Ce qu'il faut y lire :

- **Un seul constructeur, et il exige tout.** `new Projet()` ne compile pas : il est impossible de créer un projet sans code ni responsable.
- **`private set`** : `projet.Statut = StatutProjet.Termine` ne compile pas hors de la classe. Le statut ne changera que par les méthodes de l'étape 8.
- **Le statut n'est pas un paramètre** : un projet naît toujours *en préparation*.
- **`Id`** n'est fixé par personne. La base le générera au module 06 ; d'ici là il vaut 0.
- **`ClientId`, `ResponsableId`** : un projet désigne son client et son responsable par leur identifiant.
- **Les trois premières lignes** écartent les valeurs absurdes avec `ArgumentException`. Ce ne sont pas des règles du cahier des charges, seulement des garde-fous.
- **`string Code`** promet une valeur non nulle. Supprimez un instant la ligne `Code = code.Trim();` et compilez : l'avertissement `CS8618` vous rappelle la promesse.

> **Pensé pour le module 06.** Chaque paramètre du constructeur porte le nom d'une propriété (`code` pour `Code`), et les *setters* sont privés plutôt qu'absents. Ces deux choix suffisent à EF Core pour reconstruire un projet lu en base, en appelant ce même constructeur : la classe ne sera pas retouchée quand la base arrivera. Voir [Types d'entités avec constructeurs](https://learn.microsoft.com/en-us/ef/core/modeling/constructors).

### 4. Créer le projet de tests

Le modèle de projet xUnit v3 n'est pas fourni avec le SDK. Installez-le, une fois par poste :

```powershell
dotnet new install xunit.v3.templates
```

```powershell
dotnet new xunit3 -f net10.0 --name Chronos.Domain.Tests
```

`-f net10.0` est indispensable : sans lui, le modèle cible .NET 8, qui n'est plus pris en charge.

La commande se termine par une erreur, et c'est attendu :

```text
error NU1008: The following PackageReference items cannot define a value for Version: xunit.v3.mtp-v2.
```

Le modèle a écrit un numéro de version dans le `.csproj`, ce que la gestion centralisée du module 03 interdit. Corrigez en deux endroits. Dans `Directory.Packages.props`, ajoutez la version que le modèle avait choisie :

```xml
    <PackageVersion Include="xunit.v3.mtp-v2" Version="4.0.3" />
```

Dans `Chronos.Domain.Tests/Chronos.Domain.Tests.csproj`, retirez l'attribut `Version` de la ligne `PackageReference`, ainsi que les trois réglages déjà fournis par `Directory.Build.props` (`TargetFramework`, `Nullable`, `ImplicitUsings`). Laissez le reste :

- `<OutputType>Exe</OutputType>` : un projet de tests xUnit v3 est un programme exécutable ;
- `<Using Include="Xunit" />` : évite d'écrire `using Xunit;` dans chaque fichier ;
- la ligne `xunit.runner.json` : le fichier de réglages de xUnit, complété à l'étape 5.

```powershell
dotnet sln add Chronos.Domain.Tests
```

```powershell
dotnet reference add Chronos.Domain --project Chronos.Domain.Tests
```

La seconde commande donne aux tests l'accès au domaine. La dépendance va dans ce sens uniquement : le domaine ignore qu'il est testé.

Regardez enfin `global.json` : le modèle y a ajouté trois lignes.

```json
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
```

Elles indiquent à `dotnet test` quel lanceur utiliser. Versionnez ce changement : le [module 05](05-github-actions-integration-continue.md) en dépend.

Documentation : [xUnit v3 et Microsoft.Testing.Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)

### 5. Un premier test

Supprimez `UnitTest1.cs` et créez `Chronos.Domain.Tests/ProjetTests.cs` :

```csharp
namespace Chronos.Domain.Tests;

public class ProjetTests
{
    private static readonly DateOnly Debut = new(2026, 5, 4);

    private static Projet CreerProjetInterne() =>
        new("CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
            clientId: null, Debut, dateFin: null, budgetJours: 120, responsableId: 1);

    [Fact]
    public void Un_nouveau_projet_est_en_preparation()
    {
        var projet = CreerProjetInterne();

        Assert.Equal(StatutProjet.EnPreparation, projet.Statut);
    }
}
```

- **Le nom du test est une phrase du métier**, en français, sans accents. Quand il échouera dans six mois, son nom dira ce qui est cassé.
- **Trois temps**, séparés par une ligne vide : préparer, agir, vérifier. Ici, « agir » se confond avec la construction.
- **`Assert.Equal(attendu, obtenu)`** : la valeur attendue d'abord.
- **`CreerProjetInterne`** fabrique un projet valide. Chaque test n'écrira ainsi que ce qui le distingue.

Pour que les noms s'affichent comme des phrases, complétez `Chronos.Domain.Tests/xunit.runner.json` :

```json
{
    "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
    "methodDisplay": "method",
    "methodDisplayOptions": "replaceUnderscoreWithSpace"
}
```

```powershell
dotnet test
```

La commande compile la solution, exécute les tests et se termine par `Test run summary: Passed!` suivi du décompte : `total: 1`, `failed: 0`.

Documentation : [Configuration de xUnit](https://xunit.net/docs/config-xunit-runner-json)

### 6. RG-11 : le test d'abord

Écrivez le test **avant** le code. Dans `ProjetTests` :

```csharp
    [Fact]
    public void Un_projet_facturable_sans_client_est_refuse()
    {
        var exception = Assert.Throws<RegleMetierException>(() =>
            new Projet("ACME-MIGR", "Migration du SI commercial", NatureProjet.Facturable,
                clientId: null, Debut, dateFin: null, budgetJours: 300, responsableId: 1));

        Assert.Equal("RG-11", exception.CodeRegle);
    }
```

`Assert.Throws<T>` exécute le code qu'on lui confie, échoue si aucune exception de type `T` n'est levée, et sinon renvoie l'exception pour qu'on l'examine. Lancez `dotnet test` :

```text
failed Un projet facturable sans client est refuse (1ms)
  Assert.Throws() Failure: No exception was thrown
  Expected: typeof(Chronos.Domain.RegleMetierException)
```

Le test échoue, et c'est ce qu'on voulait voir : il est capable de détecter l'absence de la règle. Un test qu'on n'a jamais vu échouer ne prouve rien.

Écrivez maintenant la règle. Dans `Projet`, ajoutez la méthode :

```csharp
    private static void VerifierClient(NatureProjet nature, int? clientId)
    {
        if (nature == NatureProjet.Facturable && clientId is null)
        {
            throw new RegleMetierException("RG-11", "Un projet facturable a obligatoirement un client.");
        }

        if (nature == NatureProjet.Interne && clientId is not null)
        {
            throw new RegleMetierException("RG-11", "Un projet interne n'a pas de client.");
        }
    }
```

et appelez-la dans le constructeur, **avant** la première affectation :

```csharp
        VerifierClient(nature, clientId);
```

`dotnet test` : les deux tests passent. La règle a deux moitiés ; écrivez vous-même le test de la seconde, `Un_projet_interne_avec_client_est_refuse`.

### 7. RG-12 : une théorie

La règle dit « postérieure **ou égale** ». Ce sont les valeurs aux limites qui cachent les erreurs : on les teste toutes.

```csharp
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(31)]
    public void Une_date_de_fin_egale_ou_posterieure_au_debut_est_acceptee(int jourDeFin)
    {
        var dateFin = new DateOnly(2026, 5, jourDeFin);

        var projet = new Projet("CHRONOS", "Refonte du suivi des temps", NatureProjet.Interne,
            clientId: null, Debut, dateFin, budgetJours: 120, responsableId: 1);

        Assert.Equal(dateFin, projet.DateFin);
    }
```

La théorie s'exécute trois fois, une par `[InlineData]`, et chaque exécution compte pour un test. Un attribut n'accepte que des constantes : on ne peut pas y écrire un `DateOnly`. On passe donc le jour du mois, et le test construit la date.

Écrivez vous-même le fait `Une_date_de_fin_anterieure_au_debut_est_refusee`, sur le modèle de l'étape 6 : la veille du début s'obtient par `Debut.AddDays(-1)`, et l'exception attendue porte le code `RG-12`. Vérifiez qu'il échoue, puis ajoutez à `Projet` :

```csharp
    private static void VerifierDates(DateOnly dateDebut, DateOnly? dateFin)
    {
        if (dateFin is not null && dateFin < dateDebut)
        {
            throw new RegleMetierException("RG-12",
                "La date de fin d'un projet est postérieure ou égale à sa date de début.");
        }
    }
```

avec son appel dans le constructeur, sous celui de `VerifierClient`.

### 8. Le cycle de vie et RG-13

Le statut ne se modifie pas : le projet *démarre*, *se termine*, *est annulé*. Ajoutez à `Projet` :

```csharp
    public bool EstClos => Statut is StatutProjet.Termine or StatutProjet.Annule;

    public void Demarrer()
    {
        if (Statut != StatutProjet.EnPreparation)
        {
            throw new InvalidOperationException($"Un projet {Statut} ne peut pas être démarré.");
        }

        Statut = StatutProjet.EnCours;
    }

    public void VerifierOuvert()
    {
        if (EstClos)
        {
            throw new RegleMetierException("RG-13",
                $"Le projet {Code} est {Statut} : il n'accepte plus ni affectation ni saisie de temps.");
        }
    }
```

Écrivez sur le modèle de `Demarrer()` les deux méthodes qui manquent : `Terminer()`, possible seulement pour un projet en cours, et `Annuler()`, possible tant que le projet n'est pas clos.

- **`EstClos`** est une propriété calculée : elle n'a pas de *setter* et ne stocke rien. `is … or …` teste plusieurs valeurs d'un coup.
- **`VerifierOuvert()`** est ce que ce module peut faire de RG-13. Les affectations (module 09) et les saisies (module 10) l'appelleront avant de se rattacher à un projet.
- **Les transitions lèvent `InvalidOperationException`**, pas `RegleMetierException` : le cahier des charges énumère les statuts, mais aucune règle numérotée ne dit dans quel ordre ils s'enchaînent. Le code pose l'hypothèse la plus simple, et la question est à remonter au métier (voir « Pour la séance »).

Le test de RG-13 :

```csharp
    [Fact]
    public void Un_projet_termine_n_accepte_plus_ni_affectation_ni_saisie()
    {
        var projet = CreerProjetInterne();
        projet.Demarrer();
        projet.Terminer();

        var exception = Assert.Throws<RegleMetierException>(projet.VerifierOuvert);

        Assert.Equal("RG-13", exception.CodeRegle);
    }
```

Ici les trois temps sont bien distincts : on amène le projet à l'état voulu, on appelle la vérification, on contrôle l'exception. Une transition refusée se teste de la même façon : `Assert.Throws<InvalidOperationException>(projet.Terminer)` sur un projet encore en préparation.

### 9. Un objet valeur : `Periode`

Affectations, absences, plan de charge : Chronos manipule partout des intervalles de dates, et se demande sans cesse si deux d'entre eux se chevauchent. On écrit cette logique une fois. `Chronos.Domain/Periode.cs` :

```csharp
namespace Chronos.Domain;

public readonly record struct Periode
{
    public Periode(DateOnly debut, DateOnly fin)
    {
        if (fin < debut)
        {
            throw new ArgumentException("La fin d'une période ne précède pas son début.", nameof(fin));
        }

        Debut = debut;
        Fin = fin;
    }

    public DateOnly Debut { get; }
    public DateOnly Fin { get; }

    public bool Contient(DateOnly jour) => Debut <= jour && jour <= Fin;

    public bool Chevauche(Periode autre) => Debut <= autre.Fin && autre.Debut <= Fin;
}
```

Les deux bornes sont incluses. Une période n'a pas d'identité et ne change jamais : c'est un **objet valeur**. Il existe une écriture plus courte, `readonly record struct Periode(DateOnly Debut, DateOnly Fin);`, mais elle ne permet pas de refuser une période à l'envers ; on écrit donc le constructeur.

`Chronos.Domain.Tests/PeriodeTests.cs` :

```csharp
namespace Chronos.Domain.Tests;

public class PeriodeTests
{
    private static Periode EnMai(int debut, int fin) =>
        new(new DateOnly(2026, 5, debut), new DateOnly(2026, 5, fin));

    [Theory]
    [InlineData(4, 8, 11, 15, false)]
    [InlineData(4, 8, 8, 12, true)]
    [InlineData(4, 15, 6, 7, true)]
    [InlineData(11, 15, 4, 8, false)]
    public void Deux_periodes_se_chevauchent_si_elles_ont_un_jour_commun(
        int debutA, int finA, int debutB, int finB, bool attendu)
    {
        var a = EnMai(debutA, finA);
        var b = EnMai(debutB, finB);

        Assert.Equal(attendu, a.Chevauche(b));
        Assert.Equal(attendu, b.Chevauche(a));
    }

    [Fact]
    public void Deux_periodes_de_memes_bornes_sont_egales()
    {
        Assert.Equal(EnMai(4, 8), EnMai(4, 8));
    }
}
```

Le second test passe sans que vous ayez écrit de méthode d'égalité : c'est le mot `record` qui la fournit. Remplacez un instant `readonly record struct` par `class` et relancez : le test échoue, car deux objets distincts d'une classe ne sont égaux que s'ils sont le même objet.

### 10. Brancher l'API sur le domaine

Les énumérations existent maintenant en double. L'API doit utiliser celles du domaine.

```powershell
dotnet reference add Chronos.Domain --project Chronos.Api
```

Dans `Chronos.Api/Projets/ProjetDto.cs`, supprimez les deux énumérations. Ajoutez `using Chronos.Domain;` en tête de `ProjetDto.cs` et de `ProjetsEnMemoire.cs`.

```powershell
dotnet build
```

doit se terminer sans avertissement. Relancez l'API : `GET /api/projets/1` renvoie exactement le même JSON qu'au module 03.

L'API ne crée pas encore d'entité `Projet` : sa liste en mémoire contient des DTO, et les règles de ce module ne s'y appliquent donc pas. Le module 06 lui fera enregistrer de vraies entités ; à ce moment-là, la référence passera par le projet `Chronos.Infrastructure`.

## À vous

1. **Finir les tests de `Projet`.** Couvrez chaque transition : pour `Demarrer`, `Terminer` et `Annuler`, un test par statut de départ, accepté ou refusé. Une théorie peut-elle recevoir un `StatutProjet` dans `[InlineData]` ? Testez aussi les garde-fous : code vide, code fait d'espaces, budget négatif. Attention au type exact de l'exception attendue (voir « Pièges »).

2. **Compléter `Periode`.** Ajoutez `NombreDeJours`, puis `Intersection(Periode autre)`, qui renvoie la période commune à deux périodes, ou rien si elles sont disjointes. Quel type de retour dit « ou rien » ? Écrivez les tests d'abord.

3. **`Client`.** Écrivez l'entité (`Id`, `Nom`) et ses tests. Ajoutez ensuite à `Projet` une propriété `Client? Client`, à *setter* privé, que le constructeur ne renseigne pas : c'est EF Core qui la remplira au module 06, à partir de `ClientId`.

4. **`Collaborateur` et RG-02.** Propriétés : `Id`, `Matricule`, `Nom`, `Prenom`, `Email`, `Categorie` (énumération `CategorieCollaborateur` : `Consultant`, `Interne`), `Poste`, `DateEntree`, `DateSortie?`, `ManagerId?`, `EstArchive`. La date de sortie n'est pas connue à l'embauche : par quelle méthode arrive-t-elle, et comment s'appelle-t-elle dans la bouche d'un gestionnaire RH ? Testez RG-02, limite comprise. Ajoutez à `Projet` la propriété `Collaborateur? Responsable`, comme pour le client.

5. **`Contrat` et RG-03.** Propriétés : `Id`, `CollaborateurId`, `Type` (énumération `TypeContrat` : `Cdi`, `Cdd`, `Alternance`, `Stage`), `Quotite`, `DateDebut`, `DateFin?`. La quotité est une fraction décimale : `0.80m` pour 80 %. Testez RG-03 aux quatre limites avec une théorie ; `[InlineData(0.09m)]` ne compile pas, mais `[InlineData(0.09)]` avec un paramètre `decimal` fonctionne. Un collaborateur conserve l'historique de ses contrats : donnez-lui une méthode `AjouterContrat(...)`, une collection `Contrats` que l'extérieur peut lire mais pas modifier (un champ privé `_contrats` de type `List<Contrat>`, exposé en `IReadOnlyCollection<Contrat>`), et une méthode `QuotiteAu(DateOnly jour)`. Testez un passage de 100 % à 80 % : la quotité d'avant le changement ne doit pas bouger.

6. **Faire traverser une règle.** Dans le gestionnaire `Creer` de l'API, construisez un `Projet` à partir de la requête, avec `clientId: null` et un `responsableId` arbitraire, avant d'ajouter le DTO à la liste. Envoyez un projet facturable. Quel code HTTP recevez-vous ? Est-ce le bon ? Que voit l'appelant en `Development` ? Notez vos réponses pour la séance, puis retirez cet essai.

## Critères de réussite

- Depuis `src/api`, `dotnet build` se termine sans avertissement et `dotnet test` par `Test run summary: Passed!`.
- Chaque règle RG-02, RG-03, RG-11, RG-12 et RG-13 a au moins un test qui vérifie le `CodeRegle` de l'exception, valeurs limites comprises.
- Les noms de vos tests sont des phrases que le mentor comprend sans lire le code.
- `Chronos.Domain.csproj` ne contient ni `PackageReference` ni `ProjectReference`.
- Le domaine n'expose aucun *setter* public : depuis un test, `projet.Statut = …` ne compile pas.
- `global.json` contient la section `test` ; aucun `.csproj` ne contient de numéro de version.
- L'API répond comme à la fin du module 03, et `Chronos.Api` ne déclare plus d'énumération.
- Comparez votre travail à la solution de référence : `git diff module-03 module-04`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `dotnet new xunit` | Le modèle fourni avec le SDK génère encore du xUnit v2, en maintenance. | `dotnet new install xunit.v3.templates`, puis `dotnet new xunit3`. |
| `dotnet new xunit3` sans `-f` | Le projet cible `net8.0`, hors support. | `dotnet new xunit3 -f net10.0`. |
| Erreur `NU1008` à la création du projet de tests | Le modèle écrit une version dans le `.csproj`, ce que la gestion centralisée refuse. | Déplacer la version dans `Directory.Packages.props`. |
| Des propriétés `{ get; set; }` partout | N'importe quel code peut mettre l'entité dans un état interdit ; les règles se dispersent chez les appelants. | `private set`, un constructeur qui valide, des méthodes métier. |
| Un constructeur sans paramètre « pour faire plaisir à EF Core » | Il rouvre la porte aux objets incomplets. EF Core sait appeler un constructeur dont les paramètres portent le nom des propriétés. | Un seul constructeur, complet. |
| `DateTime` pour une date métier | L'heure et le fuseau s'en mêlent : le jour peut glisser. | `DateOnly`. |
| `double` pour une quotité ou un taux | Les fractions décimales n'y sont pas exactes. | `decimal` : `0.80m`. |
| `throw new Exception("…")` | L'appelant ne peut pas distinguer une règle violée d'une panne. | `RegleMetierException` avec le numéro de la règle. |
| `Assert.Throws<ArgumentException>` alors que le code lève `ArgumentNullException` | `Assert.Throws` exige le type **exact** : le test échoue (`Exception type was not an exact match`). | Attendre le type réellement levé, ou `Assert.ThrowsAny<ArgumentException>`. |
| Un test nommé `Test1`, ou qui vérifie cinq choses | Son échec ne dit pas ce qui est cassé. | Un comportement par test, un nom qui l'énonce. |
| Écrire le test après le code et ne jamais le voir échouer | Rien ne prouve qu'il détecte quoi que ce soit. | Le faire échouer une fois : avant d'écrire la règle, ou en la retirant un instant. |
| Faire taire un avertissement de nullabilité avec `!` ou `#nullable disable` | L'avertissement signalait un vrai risque de `NullReferenceException`. | Corriger le type : `string?` si la valeur peut manquer, sinon l'initialiser. |
| FluentAssertions pour « de plus jolies assertions » | Licence payante en usage commercial depuis la version 8. | `Assert` de xUnit. |
| Ajouter au domaine un paquet d'ASP.NET Core ou d'EF Core | Le métier devient impossible à tester sans infrastructure. | Le domaine ne dépend de rien. |

Les sources de ces constats sont dans le [référentiel .NET 10](../docs/references/dotnet-10.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Le constructeur de `Projet` refuse un projet interne qui a un client. Il aurait pu retirer le client en silence. Pourquoi refuser plutôt que corriger ?
- Le cahier des charges ne dit pas dans quel ordre les statuts d'un projet s'enchaînent. Qu'avez-vous supposé en écrivant `Demarrer`, `Terminer` et `Annuler` ? Qui doit trancher, et que change la réponse dans le code ?
- RG-01, RG-04, RG-05 et RG-10 ne sont pas dans le domaine. Pour chacune : quelle information manque à l'entité ? Si on chargeait en mémoire toute la chaîne des managers, RG-04 pourrait-elle s'écrire dans `Collaborateur` ? À quelles conditions serait-elle fiable ?
- Pourquoi avoir fait échouer le test de RG-11 avant d'écrire la règle ? Dans quels cas vous autoriseriez-vous à écrire le test après ?
- `Periode` est une structure et `Projet` une classe. Que se passerait-il si l'on inversait ?
- L'API a son `ProjetDto`, le domaine son `Projet`. Est-ce une duplication ? Qu'est-ce qui les fera diverger ?
- Vos tests créent des projets avec `responsableId: 1`, alors qu'aucun collaborateur n'existe. Est-ce un défaut du test, ou une information sur ce que `Projet` vérifie ?

## Pour aller plus loin

- [Le mot-clé `field`](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/field) (C# 14) : valider dans un *setter* sans déclarer soi-même le champ associé. Utile quand une propriété reste modifiable ; nos entités préfèrent une méthode métier.
- [Choisir entre classe et structure](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/choosing-between-class-and-struct) : les critères de la documentation.
- [Filtrage par motifs](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/pattern-matching) : tout ce que permet `is`, dont `is … or …` et `is not null`.
- [Bonnes pratiques des tests unitaires](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) : nommage, structure, ce qu'il ne faut pas tester.
- [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro) : le lanceur que `dotnet test` utilise, et ses options (`dotnet test --help` liste notamment les filtres `--filter-class` et `--filter-method`).
- Suite dans le cours : le module 05 fait tourner ces tests à chaque *push*, le module 06 enregistre ces entités en base, le module 08 traduit `RegleMetierException` en réponse HTTP.
