# Module 09 — Affectations et calcul de charge

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 10](../docs/references/dotnet-10.md).
> Le code du pas à pas a été **compilé et exécuté le 12 novembre 2026** dans un conteneur `mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.404, `xunit.v3.mtp-v2` 4.0.3, `Microsoft.Extensions.TimeProvider.Testing` 10.11.0, EF Core 10.0.15) : les tests cités passent avec `dotnet test`, les deux lectures de l'exemple chiffré ont été exécutées (167 %, puis 133 %), et les endpoints ont répondu comme indiqué sur PostgreSQL 18.6 et SQL Server 2025. **Rejoué le 12 novembre 2026 sur la solution de référence** (état du tag `module-08`, mêmes versions ; PostgreSQL 18.6 et SQL Server 2025 en conteneurs) : pas à pas, migration `Affectations` dans les deux jeux et appliquée, `has-pending-model-changes` pour les deux fournisseurs, exercices 1 à 5 (144 tests verts), appels HTTP des critères de réussite sur chaque base (réponses identiques, trois requêtes SQL pour le plan de charge) ; exercice 3 vérifié par HTTP sur PostgreSQL (422 `RG-24`, puis 201 avec `surchargeConfirmee`), exercice 5 de même (400 pour `du=04/05/2026`). Corrigés à cette occasion : l'avertissement du filtre d'archivage (étape 1), le cas « sans contrat » (étape 6), `Collaborateurs.Lister` (étape 7), le jour férié sur une base existante (étape 9). **Non exécutés** : l'envoi depuis un fichier `.http`, l'exercice 6 (exploratoire).

« Qui est disponible, surchargé ou sans mission ? » C'est la première question que le cahier des charges pose à Chronos, et celle pour laquelle un manager ouvrira l'application chaque lundi. La réponse est un calcul : des jours ouvrés, une quotité, des absences, des taux d'allocation.

Un calcul faux ici ne plante pas. Il affiche un nombre plausible, et quelqu'un prend une décision dessus. Le cahier des charges exige donc que ce calcul soit **couvert par des tests** — et il fournit un exemple chiffré. Ce module part de cet exemple, et découvre en l'écrivant que la règle n'était pas si claire.

## Objectifs

À la fin de ce module, vous saurez :

- écrire un calcul métier comme un service **pur**, qui reçoit ses données et ne connaît ni la base ni l'horloge ;
- transformer l'exemple chiffré d'un cahier des charges en test, et vous servir d'un test pour faire trancher une ambiguïté ;
- calculer en `decimal`, et arrondir au bon endroit ;
- rendre testable un code qui dépend de la date du jour, avec `TimeProvider` et `FakeTimeProvider` ;
- écrire des théories xUnit v3 alimentées par `[MemberData]` ;
- charger les données d'un calcul en un nombre fixe de requêtes.

## Prérequis

- Module 04 : `Periode` (avec `Intersection`, exercice 2), `Collaborateur`, `Contrat`, `Projet.VerifierOuvert()`, les tests xUnit v3.
- Modules 06 à 08 : le contexte EF Core, les migrations, les erreurs `400`/`422`.

## Règles de gestion

| Règle | Dans ce module |
| --- | --- |
| **RG-20** — une affectation tient dans la période du projet et dans la présence du collaborateur | Vérifiée par `Affectation` (pas à pas). |
| **RG-21** — taux d'allocation entre 5 % et 100 % | Vérifiée par `Affectation` (pas à pas). |
| **RG-13** — pas d'affectation sur un projet terminé ou annulé | `Affectation` appelle `Projet.VerifierOuvert()` du module 04. |
| **RG-22** — capacité | `CalculateurDeCharge` (pas à pas). Les absences n'existent qu'au module 10 : le calcul les reçoit déjà, l'API lui en passera alors. |
| **RG-23** — charge planifiée | `CalculateurDeCharge`. **La règle est précisée dans ce module** (étape 5). |
| **RG-24** — surcharge | Calculée ici ; la confirmation par le manager est l'exercice 3. |
| **RG-25** — intercontrat | Calculé ici ; son sens sur un mois est l'exercice 4. |

## Notions

### Un service métier pur

Le calcul de charge a besoin de données venues de quatre tables. La tentation est de lui donner le `DbContext` et de le laisser se servir. On fait l'inverse : `CalculateurDeCharge` **reçoit** tout ce dont il a besoin, sous forme de données simples, et rend un résultat. Mêmes entrées, même sortie, aucun effet de bord : c'est une fonction **pure**.

Le bénéfice est immédiat. Un test s'écrit en trois lignes, sans base ni conteneur, et s'exécute en une milliseconde. Chaque cas tordu — entrée en milieu de semaine, changement de quotité — devient une ligne de données.

### `decimal`, et l'arrondi à la fin

Le module 04 l'a dit : `0.1 + 0.2` ne vaut pas `0.3` en `double`. Un taux de charge de 100,00000000000001 % déclencherait une alerte de surcharge. Tout le calcul est donc en `decimal`, et les littéraux portent le suffixe `m`.

On n'arrondit **que ce qu'on affiche**, et à la fin : le taux de charge, une division, est arrondi à quatre décimales (`1.3333`, soit 133,33 %). La surcharge, elle, se décide en comparant la charge à la capacité, pas en comparant un taux arrondi à 1.

Documentation : [Types numériques à virgule flottante](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types), [`MidpointRounding`](https://learn.microsoft.com/en-us/dotnet/api/system.midpointrounding)

### Raisonner jour par jour

« Jours ouvrés × quotité » est simple tant que la quotité est constante. Mais un collaborateur peut passer de 100 % à 80 % un mercredi, arriver un jeudi, poser une demi-journée. Plutôt que de découper la période en morceaux homogènes, le calcul parcourt les jours ouvrés un par un : pour chaque jour, quelle quotité, quelle part d'absence, quelles affectations. C'est plus lent qu'une formule — quelques centaines d'additions — et beaucoup plus difficile à écrire faux.

### La date du jour est une donnée

« La semaine en cours » dépend de l'horloge. Un code qui appelle `DateTime.Now` ne peut être testé que le jour où on l'exécute, et un test qui passe le vendredi peut échouer le lundi.

`TimeProvider` est l'abstraction de l'horloge fournie par .NET. Le code reçoit un `TimeProvider` ; en production c'est `TimeProvider.System`, dans les tests un `FakeTimeProvider` réglé sur la date qu'on veut.

Documentation : [`TimeProvider`](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview), [`FakeTimeProvider`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.time.testing.faketimeprovider)

### Des théories alimentées par des données

`[InlineData]` n'accepte que des constantes : ni `DateOnly`, ni objet. Quand les jeux de données sont des dates ou des dossiers entiers, on les fournit par `[MemberData]`, qui désigne un membre statique de la classe de tests.

En xUnit v3, ce membre renvoie de préférence un `IEnumerable<TheoryDataRow<…>>` : chaque ligne est typée, vérifiée à la compilation, et peut porter un libellé (`Label`). Vous croiserez partout `TheoryData<…>` et `IEnumerable<object[]>` : la documentation de xUnit v3 les dit dépréciés, conservés pour la compatibilité.

Documentation : [Attributs de données des théories](https://xunit.net/docs/getting-started/v3/theory-data-attributes)

### Un nombre fixe de requêtes

Le plan de charge d'une équipe de trente personnes ne doit pas coûter trente fois plus de requêtes que celui d'une personne. Charger les collaborateurs, puis pour chacun ses affectations, est le problème dit *N+1*. On charge au contraire chaque **sorte** de données en une requête, pour toute l'équipe, puis on regroupe en mémoire.

Documentation : [Requêtes efficaces](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)

## Pas à pas

Les commandes se lancent depuis `src/api`.

### 1. `Affectation` et `JourFerie`

`Chronos.Domain/Affectation.cs` :

```csharp
namespace Chronos.Domain;

public class Affectation
{
    private Affectation(int collaborateurId, int projetId, DateOnly dateDebut, DateOnly dateFin, decimal tauxAllocation)
    {
        CollaborateurId = collaborateurId;
        ProjetId = projetId;
        DateDebut = dateDebut;
        DateFin = dateFin;
        TauxAllocation = tauxAllocation;
    }

    public int Id { get; private set; }
    public int CollaborateurId { get; private set; }
    public int ProjetId { get; private set; }
    public Projet? Projet { get; private set; }
    public DateOnly DateDebut { get; private set; }
    public DateOnly DateFin { get; private set; }
    public decimal TauxAllocation { get; private set; }

    public static Affectation Creer(
        Collaborateur collaborateur, Projet projet, DateOnly dateDebut, DateOnly dateFin, decimal tauxAllocation)
    {
        projet.VerifierOuvert();

        if (tauxAllocation is < 0.05m or > 1m)
        {
            throw new RegleMetierException("RG-21", "Le taux d'allocation est compris entre 5 % et 100 %.");
        }

        if (dateFin < dateDebut)
        {
            throw new RegleMetierException("RG-20", "La fin d'une affectation ne précède pas son début.");
        }

        if (dateDebut < projet.DateDebut || dateFin > (projet.DateFin ?? DateOnly.MaxValue))
        {
            throw new RegleMetierException("RG-20", "L'affectation doit être comprise dans la période du projet.");
        }

        if (dateDebut < collaborateur.DateEntree || dateFin > (collaborateur.DateSortie ?? DateOnly.MaxValue))
        {
            throw new RegleMetierException("RG-20",
                "L'affectation doit être comprise dans la période de présence du collaborateur.");
        }

        return new Affectation(collaborateur.Id, projet.Id, dateDebut, dateFin, tauxAllocation);
    }
}
```

- **Le constructeur est privé, la fabrique est publique.** RG-20 ne se vérifie qu'en ayant le collaborateur et le projet sous la main ; or EF Core ne sait pas passer des entités liées à un constructeur. `Creer` est donc la seule porte d'entrée pour votre code, et le constructeur privé — dont les paramètres portent le nom des propriétés, comme au module 04 — sert à EF Core pour relire une affectation.
- **`projet.VerifierOuvert()`** applique RG-13, écrite au module 04.
- **`Projet? Projet`** est une navigation que EF Core remplira, comme `Projet.Client`. Elle servira à lire la nature du projet dans une requête.

`JourFerie` n'a pas de règle : `public class JourFerie(DateOnly date, string libelle)`, avec les propriétés `Date` et `Libelle` à *setter* privé.

Côté infrastructure, ajoutez les deux `DbSet`, puis `AffectationConfiguration` (taux en `HasPrecision(3, 2)`, relations vers `Collaborateur` et `Projet` en `DeleteBehavior.Restrict`, index sur `CollaborateurId` et `DateDebut`) et `JourFerieConfiguration` (`builder.HasKey(j => j.Date)`). Créez la migration `Affectations` pour chacun des deux fournisseurs (la routine à deux jeux du [module 07](07-deux-sgbd-un-seul-code.md), étape 10) et appliquez-la. L'avertissement du module 06 sur le filtre d'archivage ne réapparaît pas pour `Affectation` : EF Core ne le signale que pour une navigation (`Projet.Responsable`), et `Affectation` n'en a pas vers `Collaborateur` (constaté). Le problème qu'il décrit existe pourtant : une affectation peut appartenir à un collaborateur archivé, que le filtre masque. Gardez-le en tête pour l'étape 9.

Écrivez les tests de RG-20, RG-21 et RG-13 sur le modèle du module 04. Pour les bornes du taux, une théorie suffit : `[InlineData(0.04, false)]`, `[InlineData(0.05, true)]`, `[InlineData(1, true)]`, `[InlineData(1.01, false)]`, avec un paramètre `decimal`.

### 2. Le calendrier de travail

`Chronos.Domain/CalendrierDeTravail.cs` :

```csharp
namespace Chronos.Domain;

public sealed class CalendrierDeTravail(IEnumerable<DateOnly> joursFeries)
{
    private readonly HashSet<DateOnly> _joursFeries = [.. joursFeries];

    public bool EstOuvre(DateOnly jour) =>
        jour.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !_joursFeries.Contains(jour);

    public IEnumerable<DateOnly> JoursOuvres(Periode periode)
    {
        for (var jour = periode.Debut; jour <= periode.Fin; jour = jour.AddDays(1))
        {
            if (EstOuvre(jour))
            {
                yield return jour;
            }
        }
    }

    public int NombreDeJoursOuvres(Periode periode) => JoursOuvres(periode).Count();
}
```

Le calendrier ne lit pas la table des jours fériés : on les lui donne. Son test, dans `Chronos.Domain.Tests/CalendrierDeTravailTests.cs`, est la première théorie à `[MemberData]` du cours :

```csharp
public class CalendrierDeTravailTests
{
    private static readonly CalendrierDeTravail Calendrier = new(
    [
        new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 8), new DateOnly(2026, 5, 14),
    ]);

    public static IEnumerable<TheoryDataRow<DateOnly, DateOnly, int>> Periodes =>
    [
        new(new DateOnly(2026, 5, 4), new DateOnly(2026, 5, 10), 4),   // semaine avec un férié
        new(new DateOnly(2026, 5, 18), new DateOnly(2026, 5, 24), 5),  // semaine complète
        new(new DateOnly(2026, 5, 9), new DateOnly(2026, 5, 10), 0),   // week-end
        new(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), 18),  // mai 2026
    ];

    [Theory]
    [MemberData(nameof(Periodes))]
    public void Nombre_de_jours_ouvres(DateOnly debut, DateOnly fin, int attendu)
    {
        Assert.Equal(attendu, Calendrier.NombreDeJoursOuvres(new Periode(debut, fin)));
    }
}
```

`dotnet test --filter-class "*CalendrierDeTravailTests"` exécute quatre tests, un par ligne.

### 3. Les données du calcul

`Chronos.Domain/DossierDeCharge.cs` décrit ce que le calcul reçoit pour **un** collaborateur :

```csharp
namespace Chronos.Domain;

public sealed record QuotiteContractuelle(Periode Periode, decimal Quotite);

public sealed record AffectationPlanifiee(int ProjetId, Periode Periode, decimal TauxAllocation, bool Facturable);

public sealed record DossierDeCharge(
    Periode Presence,
    bool EstConsultant,
    IReadOnlyList<QuotiteContractuelle> Quotites,
    IReadOnlyList<AffectationPlanifiee> Affectations,
    IReadOnlyDictionary<DateOnly, decimal> Absences)
{
    public decimal QuotiteAu(DateOnly jour) =>
        Quotites.FirstOrDefault(q => q.Periode.Contient(jour))?.Quotite ?? 0m;
}

public sealed record ChargeSurPeriode(
    Periode Periode,
    decimal Capacite,
    decimal ChargePlanifiee,
    decimal? TauxDeCharge,
    bool Surcharge,
    bool Intercontrat);
```

Ce ne sont pas les entités, et c'est voulu. Le calcul n'a besoin ni du matricule ni du libellé du projet, seulement de périodes et de taux. `Absences` associe à un jour la part de journée absente (`1` ou `0.5`) : le module 10 la remplira. Une date de fin absente (contrat en cours, collaborateur présent) se traduit par `DateOnly.MaxValue`.

### 4. L'exemple du cahier des charges devient un test

Relisez le § 7 du cahier des charges, puis écrivez-le. `Chronos.Domain.Tests/CalculateurDeChargeTests.cs` :

```csharp
public class CalculateurDeChargeTests
{
    // Semaine du 4 au 8 mai 2026 ; le vendredi 8 est férié.
    private static readonly DateOnly Lundi = new(2026, 5, 4);
    private static readonly Periode LaSemaine = new(Lundi, Lundi.AddDays(6));
    private static readonly Periode Toujours = new(DateOnly.MinValue, DateOnly.MaxValue);
    private static readonly CalendrierDeTravail Calendrier = new([new DateOnly(2026, 5, 8)]);

    private static DossierDeCharge Dossier(
        decimal quotite = 1m,
        IReadOnlyList<AffectationPlanifiee>? affectations = null,
        IReadOnlyDictionary<DateOnly, decimal>? absences = null,
        Periode? presence = null,
        IReadOnlyList<QuotiteContractuelle>? quotites = null,
        bool consultant = true) =>
        new(presence ?? Toujours, consultant, quotites ?? [new QuotiteContractuelle(Toujours, quotite)],
            affectations ?? [], absences ?? new Dictionary<DateOnly, decimal>());

    private static AffectationPlanifiee Affectation(
        decimal taux, int projetId = 1, Periode? periode = null, bool facturable = true) =>
        new(projetId, periode ?? LaSemaine, taux, facturable);

    private static DossierDeCharge Camille(bool congeLeLundi) => Dossier(
        quotite: 0.8m,
        affectations: [Affectation(0.5m, projetId: 1), Affectation(0.5m, projetId: 2)],
        absences: congeLeLundi ? new Dictionary<DateOnly, decimal> { [Lundi] = 1m } : null);

    [Fact]
    public void Exemple_du_cahier_des_charges()
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(Camille(congeLeLundi: true), LaSemaine);

        Assert.Equal(2.4m, charge.Capacite);
        Assert.Equal(4m, charge.ChargePlanifiee);
        Assert.Equal(1.6667m, charge.TauxDeCharge);
        Assert.True(charge.Surcharge);
    }
}
```

Les nombres attendus sont **ceux du document**, pas ceux que produirait votre code : capacité 2,4 jours, charge 4 jours, taux 167 %. Le test ne compile pas encore. Écrivez `Chronos.Domain/CalculateurDeCharge.cs`, en suivant RG-22 et RG-23 à la lettre :

```csharp
namespace Chronos.Domain;

public sealed class CalculateurDeCharge(CalendrierDeTravail calendrier)
{
    public ChargeSurPeriode Calculer(DossierDeCharge dossier, Periode periode)
    {
        var capacite = 0m;
        var charge = 0m;
        var chargeFacturable = 0m;

        foreach (var jour in calendrier.JoursOuvres(periode))
        {
            if (!dossier.Presence.Contient(jour))
            {
                continue;
            }

            var quotite = dossier.QuotiteAu(jour);
            var partAbsente = dossier.Absences.GetValueOrDefault(jour);
            capacite += (1m - partAbsente) * quotite;

            foreach (var affectation in dossier.Affectations)
            {
                if (!affectation.Periode.Contient(jour))
                {
                    continue;
                }

                var joursReserves = affectation.TauxAllocation;
                charge += joursReserves;
                if (affectation.Facturable)
                {
                    chargeFacturable += joursReserves;
                }
            }
        }

        decimal? taux = capacite == 0m
            ? null
            : Math.Round(charge / capacite, 4, MidpointRounding.AwayFromZero);

        return new ChargeSurPeriode(
            periode,
            capacite,
            charge,
            taux,
            Surcharge: charge > capacite,
            Intercontrat: dossier.EstConsultant && capacite > 0m && chargeFacturable == 0m);
    }
}
```

`dotnet test` : le test passe. Capacité (4 − 1) × 0,8 = 2,4 ; charge 4 × 0,5 + 4 × 0,5 = 4 ; taux 1,6667. Le code fait ce que dit le cahier des charges. On pourrait s'arrêter là.

### 5. Le test qui pose la question

Le cahier des charges lui-même prévient que l'exemple « mérite d'être discuté ». Pour discuter, on écrit un second exemple, plus simple : **Camille sans congé**. Elle est à 80 %, affectée à 50 % au projet A et à 50 % au projet B. Le vocabulaire (§ 3) définit le taux d'allocation comme une « part de la **capacité du collaborateur** réservée à un projet ». Deux moitiés de sa capacité : elle est exactement à plein. On l'écrit :

```csharp
    [Fact]
    public void Un_temps_partiel_alloue_a_100_pour_cent_est_charge_a_100_pour_cent()
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(Camille(congeLeLundi: false), LaSemaine);

        Assert.Equal(3.2m, charge.Capacite);
        Assert.Equal(1m, charge.TauxDeCharge);
        Assert.False(charge.Surcharge);
    }
```

```text
failed Un temps partiel alloue a 100 pour cent est charge a 100 pour cent
  Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   1.25
```

Le calcul annonce 125 % : charge 4 jours, capacité 3,2. Appliquée à la lettre, RG-23 compte les allocations en jours **de temps plein**, et tout collaborateur à temps partiel qu'on occupe entièrement apparaît en surcharge permanente. La formule de RG-23 et la définition du § 3 se contredisent.

Ce n'est pas au développeur de choisir en silence. Le test est l'outil pour poser la question au métier : « voici Camille, sans congé, à plein : affichez-vous 100 % ou 125 % ? »

**Décision retenue.** Le vocabulaire a raison : une allocation réserve une part de la capacité **du collaborateur**. La charge planifiée tient donc compte de la quotité. Elle ne tient **pas** compte des absences : une absence réduit la capacité, pas le travail promis aux projets, et c'est justement ce qui doit faire apparaître la surcharge. RG-23 devient :

> **RG-23** — Charge planifiée sur une période = somme, pour chaque affectation, des jours ouvrés couverts × **quotité** × taux d'allocation.

Dans `CalculateurDeCharge`, une ligne change :

```csharp
                var joursReserves = quotite * affectation.TauxAllocation;
```

Le second test passe. Le premier échoue, et c'est normal : l'exemple du § 7 illustrait l'ancienne lecture. Avec le congé du lundi, la capacité reste 2,4 jours ; la charge vaut 4 × 0,8 × 0,5 × 2 = 3,2 jours ; le taux 3,2 ÷ 2,4 ≈ 133 %. Mettez le test à jour :

```csharp
        Assert.Equal(2.4m, charge.Capacite);
        Assert.Equal(3.2m, charge.ChargePlanifiee);
        Assert.Equal(1.3333m, charge.TauxDeCharge);
        Assert.True(charge.Surcharge);
```

Camille est toujours en surcharge cette semaine-là, mais pour la bonne raison : son congé, pas son temps partiel. Signalez la modification de RG-23 et du § 7 à qui tient le cahier des charges : un test qui contredit le document de référence sans que personne le sache est une dette. Dans le dépôt du cours, la décision est consignée au [§ 8 du cahier des charges](../docs/cahier-des-charges.md#8-précisions-apportées-en-cours-de-projet), sans réécrire le texte d'origine.

### 6. Les cas limites

Chaque cas tordu devient une ligne de données, avec son libellé :

```csharp
    public static IEnumerable<TheoryDataRow<DossierDeCharge, decimal, decimal>> CasLimites =>
    [
        new(Dossier(presence: new Periode(Lundi.AddDays(2), DateOnly.MaxValue), affectations: [Affectation(0.5m)]),
            2m, 1m)
            { Label = "entrée le mercredi" },

        new(Dossier(
                quotites:
                [
                    new QuotiteContractuelle(new Periode(DateOnly.MinValue, Lundi.AddDays(1)), 1m),
                    new QuotiteContractuelle(new Periode(Lundi.AddDays(2), DateOnly.MaxValue), 0.8m),
                ],
                affectations: [Affectation(1m)]),
            3.6m, 3.6m)
            { Label = "quotité qui passe de 100 % à 80 % le mercredi" },

        new(Dossier(affectations: [Affectation(0.5m, periode: new Periode(Lundi.AddDays(2), Lundi.AddDays(3)))]),
            4m, 1m)
            { Label = "affectation limitée au mercredi et au jeudi" },

        new(Dossier(quotite: 0.8m, absences: new Dictionary<DateOnly, decimal> { [Lundi.AddDays(1)] = 0.5m }),
            2.8m, 0m)
            { Label = "demi-journée d'absence à 80 %" },
    ];

    [Theory]
    [MemberData(nameof(CasLimites))]
    public void Capacite_et_charge_dans_les_cas_limites(
        DossierDeCharge dossier, decimal capaciteAttendue, decimal chargeAttendue)
    {
        var charge = new CalculateurDeCharge(Calendrier).Calculer(dossier, LaSemaine);

        Assert.Equal(capaciteAttendue, charge.Capacite);
        Assert.Equal(chargeAttendue, charge.ChargePlanifiee);
    }
```

Vérifiez chaque nombre attendu à la main avant de lancer les tests : c'est tout l'intérêt. Dans le rapport de tests, chaque ligne porte son libellé : `Capacite et charge dans les cas limites [entrée le mercredi]`.

Deux cas restent à écrire en faits séparés, parce qu'ils vérifient autre chose :

- **Sans capacité** (absent toute la semaine, ou sans contrat), le taux de charge n'est pas défini : `TauxDeCharge` vaut `null`, pas zéro, et il n'y a pas de division par zéro. Absent toute la semaine avec des affectations, `Surcharge` est vrai. Sans contrat, la quotité est nulle, la charge aussi (RG-23 précisée) : pas de surcharge.
- **Intercontrat (RG-25)** : un consultant qui a de la capacité et aucune charge **facturable**. Un consultant affecté à une formation interne est en intercontrat ; un salarié interne ne l'est jamais.

### 7. « La semaine en cours »

`Chronos.Domain/Semaine.cs` :

```csharp
namespace Chronos.Domain;

public static class Semaine
{
    public static Periode Contenant(DateOnly jour)
    {
        var joursDepuisLundi = ((int)jour.DayOfWeek + 6) % 7;
        var lundi = jour.AddDays(-joursDepuisLundi);
        return new Periode(lundi, lundi.AddDays(6));
    }
}

public static class Horloge
{
    public static DateOnly Aujourdhui(this TimeProvider horloge) =>
        DateOnly.FromDateTime(horloge.GetLocalNow().DateTime);
}
```

`DayOfWeek` vaut 0 le dimanche : le calcul `(… + 6) % 7` ramène le lundi à 0. `Aujourdhui` est une méthode d'extension sur `TimeProvider` — qui fait partie de .NET, le domaine ne dépend toujours de rien.

Pour le tester, ajoutez le paquet de l'horloge factice au projet de tests, en version 10 :

```powershell
dotnet package add Microsoft.Extensions.TimeProvider.Testing --version 10.11.0 --project Chronos.Domain.Tests
```

```csharp
using Microsoft.Extensions.Time.Testing;

public class HorlogeTests
{
    [Fact]
    public void La_semaine_en_cours_depend_de_l_horloge()
    {
        var horloge = new FakeTimeProvider(new DateTimeOffset(2026, 5, 6, 10, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 5, 4), Semaine.Contenant(horloge.Aujourdhui()).Debut);

        horloge.Advance(TimeSpan.FromDays(7));

        Assert.Equal(new DateOnly(2026, 5, 11), Semaine.Contenant(horloge.Aujourdhui()).Debut);
    }
}
```

Le test choisit la date, puis fait avancer le temps d'une semaine en une ligne. Écrivez-en un second, `Aujourdhui_est_la_date_locale_et_non_la_date_UTC` : réglez l'horloge sur le dimanche 10 mai 2026 à 22 h 30 UTC, donnez-lui le fuseau de Paris par `horloge.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"))`, et vérifiez qu'« aujourd'hui » est déjà le lundi 11. Une date métier est une date **locale** ; avec `GetUtcNow()`, le plan de charge du dimanche soir serait encore celui de la semaine passée.

Dans l'API, l'horloge réelle est déjà enregistrée depuis le module 08 (`AddSingleton(TimeProvider.System)`), et un gestionnaire la reçoit en déclarant un paramètre `TimeProvider horloge`. Dans `Collaborateurs.Lister` (module 08), remplacez `DateOnly.FromDateTime(horloge.GetLocalNow().DateTime)` par `horloge.Aujourdhui()` : la règle « aujourd'hui » s'écrit à un seul endroit.

### 8. Découper une période

Le plan de charge se lit par semaine ou par mois. Ajoutez à `Semaine.cs` :

```csharp
public static class Decoupage
{
    public static IEnumerable<Periode> ParSemaine(Periode periode) =>
        Decouper(periode, debut => Semaine.Contenant(debut).Fin);

    public static IEnumerable<Periode> ParMois(Periode periode) =>
        Decouper(periode, debut => new DateOnly(debut.Year, debut.Month, DateTime.DaysInMonth(debut.Year, debut.Month)));

    private static IEnumerable<Periode> Decouper(Periode periode, Func<DateOnly, DateOnly> finDeTranche)
    {
        var debut = periode.Debut;
        while (debut <= periode.Fin)
        {
            var fin = finDeTranche(debut);
            if (fin > periode.Fin)
            {
                fin = periode.Fin;
            }

            yield return new Periode(debut, fin);
            debut = fin.AddDays(1);
        }
    }
}
```

Du 6 au 19 mai 2026, `ParSemaine` rend trois tranches : 6–10, 11–17, 18–19. Écrivez ce test, et celui d'un découpage par mois à cheval sur février.

### 9. `GET /api/plan-de-charge`

Créez `Chronos.Api/PlanDeCharge/`, avec les DTO :

```csharp
public record PlanDeChargeDto(DateOnly Du, DateOnly Au, string Pas, IReadOnlyList<LignePlanDeChargeDto> Lignes);

public record LignePlanDeChargeDto(int CollaborateurId, string Nom, string Prenom, IReadOnlyList<ChargeDto> Periodes);

public record ChargeDto(
    DateOnly Debut, DateOnly Fin, decimal Capacite, decimal ChargePlanifiee,
    decimal? TauxDeCharge, bool Surcharge, bool Intercontrat);
```

et le gestionnaire, branché par `app.MapGet("/api/plan-de-charge", Obtenir)` dans une méthode `MapPlanDeCharge` :

```csharp
private static async Task<Results<Ok<PlanDeChargeDto>, ValidationProblem>> Obtenir(
    DateOnly? du, DateOnly? au, string? pas, int? managerId,
    ChronosDbContext db, TimeProvider horloge, CancellationToken cancellationToken)
{
    var debut = du ?? Semaine.Contenant(horloge.Aujourdhui()).Debut;
    var fin = au ?? Semaine.Contenant(debut).Fin;

    if (fin < debut || fin.DayNumber - debut.DayNumber > 366)
    {
        return Invalide("Au", "La période demandée va de « du » à « au », sur un an au plus.");
    }
    if (pas is not (null or "semaine" or "mois"))
    {
        return Invalide("Pas", "Valeurs acceptées : semaine, mois.");
    }

    var periode = new Periode(debut, fin);
    var tranches = (pas == "mois" ? Decoupage.ParMois(periode) : Decoupage.ParSemaine(periode)).ToList();

    // Requête 1 : les collaborateurs présents sur la période, avec leurs contrats.
    var collaborateurs = await db.Collaborateurs
        .Where(c => managerId == null || c.ManagerId == managerId)
        .Where(c => c.DateEntree <= fin && (c.DateSortie == null || c.DateSortie >= debut))
        .OrderBy(c => c.Nom).ThenBy(c => c.Prenom).ThenBy(c => c.Id)
        .Select(c => new
        {
            c.Id, c.Nom, c.Prenom, c.Categorie, c.DateEntree, c.DateSortie,
            Contrats = c.Contrats.Select(k => new { k.DateDebut, k.DateFin, k.Quotite }).ToList(),
        })
        .ToListAsync(cancellationToken);

    var ids = collaborateurs.Select(c => c.Id).ToList();

    // Requête 2 : leurs affectations qui touchent la période, avec la nature du projet.
    var affectations = await db.Affectations
        .Where(a => ids.Contains(a.CollaborateurId) && a.DateDebut <= fin && a.DateFin >= debut)
        .Select(a => new { a.CollaborateurId, a.ProjetId, a.DateDebut, a.DateFin, a.TauxAllocation, a.Projet!.Nature })
        .ToListAsync(cancellationToken);

    // Requête 3 : les jours fériés de la période.
    var joursFeries = await db.JoursFeries
        .Where(j => j.Date >= debut && j.Date <= fin)
        .Select(j => j.Date)
        .ToListAsync(cancellationToken);

    var calculateur = new CalculateurDeCharge(new CalendrierDeTravail(joursFeries));
    var affectationsParCollaborateur = affectations.ToLookup(a => a.CollaborateurId);

    var lignes = collaborateurs.Select(c =>
    {
        var dossier = new DossierDeCharge(
            Presence: new Periode(c.DateEntree, c.DateSortie ?? DateOnly.MaxValue),
            EstConsultant: c.Categorie == CategorieCollaborateur.Consultant,
            Quotites: [.. c.Contrats.Select(k =>
                new QuotiteContractuelle(new Periode(k.DateDebut, k.DateFin ?? DateOnly.MaxValue), k.Quotite))],
            Affectations: [.. affectationsParCollaborateur[c.Id].Select(a =>
                new AffectationPlanifiee(a.ProjetId, new Periode(a.DateDebut, a.DateFin), a.TauxAllocation,
                    a.Nature == NatureProjet.Facturable))],
            Absences: new Dictionary<DateOnly, decimal>());

        return new LignePlanDeChargeDto(c.Id, c.Nom, c.Prenom,
            [.. tranches.Select(t => VersDto(calculateur.Calculer(dossier, t)))]);
    }).ToList();

    return TypedResults.Ok(new PlanDeChargeDto(debut, fin, pas ?? "semaine", lignes));
}
```

`VersDto` recopie un `ChargeSurPeriode` dans un `ChargeDto` ; `Invalide` est la méthode du module 08.

- **Trois requêtes**, pour une personne comme pour trente. Vérifiez-le dans le journal SQL.
- **Des projections, pas des entités** : on ne lit que les colonnes utiles au calcul, et rien n'est suivi par le contexte.
- **`ToLookup`** regroupe en mémoire les affectations par collaborateur ; `lookup[id]` rend une liste vide quand il n'y en a pas.
- **`CalculateurDeCharge` n'est pas enregistré dans l'injection de dépendances** : il se construit à partir de données de la requête. Un simple `new` suffit.
- **Les deux tests de chevauchement** (`DateDebut <= fin && DateFin >= debut`) sont ceux de `Periode.Chevauche`, écrits de façon qu'EF Core les traduise en SQL.
- **Un collaborateur archivé** n'apparaît pas : le filtre du module 06 s'applique à la requête 1. Ses affectations, elles, sont toujours en base.

Il reste à créer des affectations. `POST /api/affectations`, dans `Chronos.Api/Affectations/`, charge le collaborateur et le projet désignés par la requête (`400` avec `errors` si l'un des deux n'existe pas), appelle `Affectation.Creer(…)`, enregistre et répond `201`. Les règles violées deviennent des `422` sans une ligne de plus, grâce au gestionnaire du module 08.

Créez les deux affectations de Camille pour la semaine du 4 mai, ajoutez le 8 mai aux jours fériés (dans vos données initiales pour l'instant).

Les données initiales ne s'exécutent que sur une base vide (module 06) : sur votre base, qui contient déjà Camille, le jour férié n'apparaît pas. Repartez d'une base vide (`dotnet ef database drop --force` puis `database update`, pour chaque fournisseur, comme au module 07) — et recréez alors les deux affectations — ou, si vous avez fait l'exercice 2, envoyez `POST /api/jours-feries`. Ensuite :

```powershell
curl.exe "http://localhost:5080/api/plan-de-charge?du=2026-05-04&au=2026-05-10"
```

Pour Camille : `"capacite":3.20,"chargePlanifiee":3.2000,"tauxDeCharge":1.00,"surcharge":false`. C'est le second test, vu depuis l'API ; le module 10 ajoutera le congé du lundi. Ne vous étonnez pas des zéros : un `decimal` garde la trace de sa précision, et `3.2000` vaut `3.2`.

## À vous

1. **Les affectations au complet.** Écrivez `GET /api/affectations?collaborateurId=&projetId=`, `PUT /api/affectations/{id}` et `DELETE /api/affectations/{id}`. Pour la modification, l'entité a besoin d'une méthode qui revérifie RG-13, RG-20 et RG-21 sans dupliquer `Creer`. Complétez aussi la condition d'archivage de `DELETE /api/collaborateurs/{id}` (RG-05).

2. **Les jours fériés.** `GET`, `POST` et `DELETE /api/jours-feries`. Quelle est la clé d'un jour férié dans l'URL ? Que répond un second `POST` pour la même date, et grâce à quoi ?

3. **Confirmer une surcharge (RG-24).** Une affectation qui met le collaborateur en surcharge sur au moins une semaine doit être confirmée. Faites répondre `422` avec `"regle": "RG-24"` tant que la requête ne porte pas `"surchargeConfirmee": true`. Le calcul doit inclure l'affectation qu'on est en train de créer. Écrivez d'abord le test du calcul, puis l'endpoint.

4. **Intercontrat sur un mois.** RG-25 parle d'« une semaine complète ». Que signifie `intercontrat` dans une cellule mensuelle : aucune charge facturable du mois, ou au moins une semaine sans ? Écrivez les deux exemples chiffrés, faites trancher, puis codez.

5. **Une date mal écrite.** Appelez le plan de charge avec `du=04/05/2026`. Quelle période l'API a-t-elle comprise ? Proposez une parade et testez-la.

6. **Compter les requêtes.** Ajoutez vingt collaborateurs aux données initiales et vérifiez dans le journal que le plan de charge n'émet toujours que trois requêtes. Réécrivez ensuite — sans la garder — une version qui charge les affectations collaborateur par collaborateur, et comparez.

## Critères de réussite

- `dotnet test` se termine par `Test run summary: Passed!`, et les tests comprennent : l'exemple du cahier des charges à 133 %, le temps partiel à 100 %, les quatre cas limites, le taux indéfini, l'intercontrat, les bornes de RG-21, RG-20 et RG-13.
- Vous savez montrer, en changeant une ligne, le test qui échoue avec la lecture littérale de RG-23, et expliquer pourquoi.
- Aucun test du calcul n'utilise de base de données ni la date du jour ; `Chronos.Domain.csproj` ne référence toujours rien.
- `GET /api/plan-de-charge?du=2026-05-04&au=2026-05-10` répond pour Camille une capacité de 3,2 et un taux de 1, en trois requêtes SQL.
- Sans paramètre, le plan de charge porte sur la semaine en cours, et un test prouve que cette semaine suit l'horloge.
- `POST /api/affectations` répond `422` avec `RG-20`, `RG-21` ou `RG-13` selon le cas.
- Comparez votre travail à la solution de référence : `git diff module-08 module-09`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| Donner le `DbContext` au service de calcul | Chaque test exige une base ; le calcul et le chargement se mélangent. | Un service pur, qui reçoit ses données. |
| `double` ou `float` pour des jours et des taux | `0.1 + 0.2 != 0.3` : des surcharges fantômes. | `decimal`, littéraux en `m`. |
| Arrondir à chaque étape | Les erreurs d'arrondi s'additionnent. | Arrondir le résultat affiché, une fois. |
| `Math.Round(x, 4)` sans préciser le mode | Par défaut, l'arrondi se fait au pair le plus proche (`2.5` → `2`). | `MidpointRounding.AwayFromZero` si c'est l'arrondi attendu par le métier. |
| Diviser par la capacité sans tester zéro | `DivideByZeroException` pour un collaborateur absent toute la semaine. | Un taux `null` quand la capacité est nulle. |
| `DateTime.Now`, `DateTime.Today` dans le code métier | Impossible à tester ; dépend du fuseau du serveur sans le dire. | `TimeProvider` injecté. |
| `ISystemClock` | Obsolète depuis .NET 8. | `TimeProvider`. |
| `GetUtcNow()` pour obtenir « aujourd'hui » | Le jour change à minuit UTC, pas à minuit chez l'utilisateur. | `GetLocalNow()`, et un test avec un fuseau. |
| `dotnet package add Microsoft.Extensions.TimeProvider.Testing` sans version | La commande prend la plus récente, 11.x. | `--version 10.11.0`. |
| `TheoryData<…>` ou `IEnumerable<object[]>` pour `[MemberData]` | Dépréciés en xUnit v3. | `IEnumerable<TheoryDataRow<…>>`. |
| Écrire les valeurs attendues en exécutant le code | Le test ne vérifie plus que le code est égal à lui-même. | Calculer l'attendu à la main, ou le prendre dans le cahier des charges. |
| Corriger en silence une règle ambiguë | Le code et le document de référence divergent. | Un exemple chiffré, une question au métier, une règle mise à jour. |
| Une requête par collaborateur dans une boucle | Trente collaborateurs, trente allers-retours. | Une requête par sorte de données, puis `ToLookup`. |
| `du=04/05/2026` dans l'URL | Lu comme le 5 avril : les dates d'URL sont analysées en culture invariante. Constaté. | Le format ISO `2026-05-04`, et un refus du reste (exercice 5). |

## Pour la séance

- Avant la décision de l'étape 5, quel nombre auriez-vous affiché pour Camille sans congé ? Qui, dans une entreprise, a autorité pour trancher ? Que faites-vous en attendant sa réponse ?
- La charge ne tient pas compte des absences. Un manager affecte Camille à 100 % pendant ses trois semaines de congés d'été : que montre le plan de charge ? Est-ce utile ou trompeur ?
- Le calcul parcourt les jours un par un. À partir de quelle taille d'équipe et de période cela deviendrait-il un problème ? Comment le sauriez-vous avant vos utilisateurs ?
- `CalculateurDeCharge` reçoit des `DossierDeCharge`, pas des `Collaborateur`. Qu'aurait-on gagné, et perdu, à lui passer les entités ?
- « Intercontrat » exige une capacité non nulle. Un consultant en congé toute la semaine est-il en intercontrat ? Qui le décide ?
- Le serveur tourne en UTC dans un conteneur et les utilisateurs sont à Paris. Que renvoie `GetLocalNow()` ? Où ce réglage doit-il vivre ?
- Pourquoi les tests du calcul sont-ils dans `Chronos.Domain.Tests` et pas dans les tests d'intégration du module 11 ? Que resterait-il à vérifier là-bas ?

## Pour aller plus loin

- [Nouveautés de xUnit v3](https://xunit.net/docs/getting-started/v3/whats-new) : `TheoryDataRow`, `MatrixTheoryData` pour croiser des jeux de données, tests explicites.
- [xUnit v3 et Microsoft.Testing.Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform) : `--filter-class`, `--filter-method` et les autres options de `dotnet test`.
- [Requêtes uniques ou fractionnées](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries) : ce que coûte la jointure de la requête 1, et quand la fractionner.
- [`ExecuteUpdate` et `ExecuteDelete`](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete) : supprimer une affectation sans la charger.
- [Paquet `Microsoft.Extensions.TimeProvider.Testing`](https://www.nuget.org/packages/Microsoft.Extensions.TimeProvider.Testing) : les versions disponibles.
- Suite dans le cours : le module 10 fait entrer les absences dans la capacité ; le module 18 affiche le plan de charge.
