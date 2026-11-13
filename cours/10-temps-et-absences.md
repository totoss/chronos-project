# Module 10 — Temps et absences

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel .NET 10](../docs/references/dotnet-10.md).
> Le code du pas à pas a été **compilé et exécuté le 12 novembre 2026** dans un conteneur `mcr.microsoft.com/dotnet/sdk:10.0` (SDK 10.0.404, EF Core 10.0.15, Npgsql 10.0.3, Microsoft.Data.SqlClient 6.1.7) : tests unitaires des transitions, appels HTTP de chaque règle, et l'expérience des deux demandes simultanées dans ses quatre variantes, sur PostgreSQL 18.6 ; les mêmes appels et la variante finale de l'expérience ont été rejoués sur SQL Server 2025 (17.0.5045). **Rejoué le 12 novembre 2026 sur la solution de référence** (état du tag `module-09`, mêmes versions ; PostgreSQL 18.6 et SQL Server 2025 17.0.5045 en conteneurs) : pas à pas, migrations `FeuillesDeTemps` et `Absences` dans les deux jeux et appliquées (`EnableRetryOnFailure` sans effet sur `dotnet ef`), `has-pending-model-changes` pour les deux fournisseurs, exercices 1, 2, 3 et 5 (225 tests verts), parcours des étapes 5, 8 et 9 par HTTP sur chaque base (réponses identiques), courses avec délai provisoire sur les deux SGBD : RG-41 (`201` + `422`), course inverse de l'exercice 5 (`200` + `422` `RG-43`), deux transitions simultanées d'une même feuille ou d'une même absence (`200` + `409` ou `422`). Corrigés à cette occasion : la conversion du statut (étape 3), le projet inconnu (étape 4), le projet non affecté et la saisie corrigée (étape 5), le jeton `Version` de l'absence et les types sur une base existante (étape 6), `Demander` (étape 8), la dernière vérification (étape 9), le test de l'exercice 1. **Non exécutés** : l'envoi depuis un fichier `.http`, l'exercice 6 (exploratoire).

Chronos sait qui est affecté à quoi. Il lui reste à savoir ce qui s'est **réellement** passé : le temps saisi par chacun, validé par son manager, et les absences, demandées puis approuvées.

Ces deux objets ont un point commun que les collaborateurs et les projets n'avaient pas : ils **avancent**. Une feuille de temps est un brouillon, puis elle est soumise, puis validée ou renvoyée. Chaque étape autorise certaines actions et en interdit d'autres. Et ils se croisent : on ne saisit pas de temps un jour d'absence, on n'approuve pas une absence sur du temps déjà validé. Quand deux personnes agissent au même instant, il faut que la base reste cohérente quand même.

## Objectifs

À la fin de ce module, vous saurez :

- modéliser un cycle de validation par des états et des transitions, portés par l'entité ;
- regrouper des entités en un **agrégat** qui ne se modifie que par sa racine ;
- exposer une transition par un endpoint d'action plutôt que par la modification d'un champ ;
- dire ce que `SaveChangesAsync` garantit déjà, et reconnaître les cas où il faut une transaction explicite ;
- écrire cette transaction avec le bon niveau d'isolation et la stratégie d'exécution d'EF Core ;
- provoquer une condition de concurrence pour vérifier qu'elle est traitée.

## Prérequis

- Module 08 : `RegleMetierException` → `422`, le `ConflitExceptionHandler`, le jeton de concurrence.
- Module 09 : `CalendrierDeTravail`, `AffectationPlanifiee`, `DossierDeCharge`, le plan de charge, `TimeProvider`.
- Module 07 : le `switch` des fournisseurs dans `Program.cs`, la routine à deux jeux de migrations.

## Règles de gestion

| Règle | Dans ce module |
| --- | --- |
| **RG-30** à **RG-33** — quarts de journée, un jour au plus, jours non travaillés, projets affectés | Vérifiées par `FeuilleDeTemps` quand on remplace ses saisies (pas à pas). |
| **RG-13** — pas de saisie sur un projet terminé ou annulé | `Projet.VerifierOuvert()`, appelée avant d'enregistrer les saisies. |
| **RG-34**, **RG-35** — cycle de la feuille ; plus de modification après soumission | Les méthodes `Soumettre`, `Valider`, `Rejeter` (pas à pas). |
| **RG-36** — qui valide | Seule la seconde phrase ici : nul ne valide sa propre feuille. Le reste demande de savoir **qui** appelle : [module 12](12-securite-et-tracabilite.md). D'ici là, la requête porte l'identifiant du valideur. |
| **RG-40**, **RG-42** — cycle et durée d'une absence | `Absence` (pas à pas). |
| **RG-41** — pas de chevauchement | Croise plusieurs absences : transaction (pas à pas, étape 7). |
| **RG-43** — pas d'approbation sur du temps validé | Croise absences et feuilles : transaction (étape 8). |
| **RG-22** — les absences approuvées réduisent la capacité | Le plan de charge du module 09 reçoit enfin ses absences (étape 9). |
| **RG-05** — archivage | La condition du module 08 gagne les saisies et les absences (exercice 1). |

## Notions

### Une machine à états dans l'entité

Le cycle d'une feuille de temps tient en un schéma : *brouillon* → *soumise* → *validée*, avec un retour de *soumise* à *brouillon* en cas de rejet. C'est une **machine à états** : un ensemble fini d'états, et pour chacun les transitions permises.

On ne l'écrit pas dans l'API. `feuille.Statut = StatutFeuilleDeTemps.Validee` ne compile pas (*setter* privé) ; on appelle `feuille.Valider(valideurId)`, et c'est la feuille qui vérifie qu'elle est soumise. Une transition interdite lève `RegleMetierException("RG-34", …)` : ici, contrairement aux statuts d'un projet (module 04), une règle numérotée fixe l'ordre.

### Agrégat

Une saisie de temps n'a pas de sens seule : « 0,5 jour le mardi sur le projet A » ne peut être jugée qu'avec les autres saisies du même jour (RG-31) et avec le statut de la feuille (RG-35). La feuille et ses saisies forment un **agrégat** : un groupe d'objets qui doit rester cohérent **ensemble**, et dont un seul, la **racine**, est accessible de l'extérieur.

Conséquences pratiques : pas de `DbSet<SaisieDeTemps>`, pas d'endpoint `/api/saisies`, un constructeur de `SaisieDeTemps` que seul le domaine peut appeler. Toute modification passe par `FeuilleDeTemps`, qui vérifie tout. Entre agrégats, on se désigne par identifiant (`ProjetId`), pas par objet.

Documentation : [Concevoir un modèle de domaine — agrégats](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model)

### Un endpoint par action

Soumettre une feuille n'est pas « modifier le champ statut ». Un `PUT` qui accepterait `{ "statut": "Validee" }` laisserait le client choisir l'état d'arrivée, obligerait le serveur à deviner la transition voulue, et n'aurait nulle part où mettre le motif d'un rejet.

Chronos expose donc chaque transition comme une petite ressource que l'on crée : `POST /api/feuilles-de-temps/{id}/soumission`, `…/validation`, `…/rejet`. Le verbe est toujours `POST` (l'action n'est pas rejouable sans effet), le nom dit l'intention, le corps porte ce que l'action demande.

### Ce que `SaveChangesAsync` garantit déjà

Tous les changements d'un même appel à `SaveChangesAsync` sont appliqués dans **une transaction** : ils réussissent tous ou aucun. Remplacer les saisies d'une feuille — des `DELETE`, des `INSERT`, un `UPDATE` — est donc déjà atomique, sans rien écrire.

Ce que `SaveChangesAsync` ne garantit **pas** : que ce que vous avez **lu** avant soit encore vrai au moment où vous écrivez. « Je vérifie qu'aucune absence ne chevauche, puis j'insère » fait deux allers-retours ; entre les deux, une autre requête a le temps de faire la même chose.

Documentation : [Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)

### Transaction explicite et niveau d'isolation

Pour qu'une lecture et une écriture forment un tout, on les place dans une transaction ouverte par `BeginTransactionAsync`. Cela ne suffit pas : au niveau d'isolation par défaut des deux SGBD (*read committed*), deux transactions peuvent encore lire le même état puis écrire chacune de son côté.

Le niveau **sérialisable** promet davantage : le résultat est celui qu'on aurait obtenu en exécutant les transactions l'une après l'autre. Quand ce n'est pas possible, la base **fait échouer** l'une des deux — PostgreSQL par une erreur de sérialisation, SQL Server par un interblocage dont il désigne une victime. L'application doit alors **recommencer** la transaction perdante.

Documentation : [Isolation des transactions (PostgreSQL 18)](https://www.postgresql.org/docs/18/transaction-iso.html), [`SET TRANSACTION ISOLATION LEVEL` (SQL Server)](https://learn.microsoft.com/en-us/sql/t-sql/statements/set-transaction-isolation-level-transact-sql)

### Stratégie d'exécution

Recommencer, EF Core sait le faire : c'est le rôle de la **stratégie d'exécution**, activée par `EnableRetryOnFailure()`. Conçue pour les coupures de connexion, elle rejoue aussi les échecs que le fournisseur déclare passagers — dont l'erreur de sérialisation et l'interblocage.

Elle a une exigence : une transaction que vous ouvrez vous-même doit lui être confiée **en entier**, sous la forme d'une fonction qu'elle pourra rappeler depuis le début.

Documentation : [Résilience des connexions](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency), [Stratégie d'exécution de Npgsql](https://www.npgsql.org/efcore/misc/other.html)

## Pas à pas

Les commandes se lancent depuis `src/api`.

### 1. La feuille de temps et son cycle

`Chronos.Domain/FeuilleDeTemps.cs` (et `StatutFeuilleDeTemps.cs` : `Brouillon`, `Soumise`, `Validee`) :

```csharp
namespace Chronos.Domain;

public class FeuilleDeTemps
{
    private readonly List<SaisieDeTemps> _saisies = [];

    public FeuilleDeTemps(int collaborateurId, DateOnly semaine)
    {
        if (semaine.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentException("Une feuille de temps commence un lundi.", nameof(semaine));
        }

        CollaborateurId = collaborateurId;
        Semaine = semaine;
        Statut = StatutFeuilleDeTemps.Brouillon;
    }

    public int Id { get; private set; }
    public int CollaborateurId { get; private set; }
    public DateOnly Semaine { get; private set; }
    public StatutFeuilleDeTemps Statut { get; private set; }
    public string? MotifRejet { get; private set; }
    public Guid Version { get; private set; } = Guid.NewGuid();
    public IReadOnlyCollection<SaisieDeTemps> Saisies => _saisies;

    public Periode Periode => new(Semaine, Semaine.AddDays(6));

    public void Soumettre()
    {
        Exiger(StatutFeuilleDeTemps.Brouillon, "Seule une feuille à l'état de brouillon peut être soumise.");
        Statut = StatutFeuilleDeTemps.Soumise;
        MotifRejet = null;
        Version = Guid.NewGuid();
    }

    public void Valider(int valideurId)
    {
        Exiger(StatutFeuilleDeTemps.Soumise, "Seule une feuille soumise peut être validée.");
        InterdireAuCollaborateur(valideurId);
        Statut = StatutFeuilleDeTemps.Validee;
        Version = Guid.NewGuid();
    }

    public void Rejeter(int valideurId, string motif)
    {
        Exiger(StatutFeuilleDeTemps.Soumise, "Seule une feuille soumise peut être rejetée.");
        InterdireAuCollaborateur(valideurId);
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new RegleMetierException("RG-34", "Le rejet d'une feuille de temps exige un motif.");
        }

        Statut = StatutFeuilleDeTemps.Brouillon;
        MotifRejet = motif.Trim();
        Version = Guid.NewGuid();
    }

    private void Exiger(StatutFeuilleDeTemps attendu, string message)
    {
        if (Statut != attendu)
        {
            throw new RegleMetierException("RG-34", message);
        }
    }

    private void InterdireAuCollaborateur(int valideurId)
    {
        if (valideurId == CollaborateurId)
        {
            throw new RegleMetierException("RG-36", "Nul ne valide ni ne rejette sa propre feuille de temps.");
        }
    }
}
```

- **Chaque transition tient en trois temps** : exiger l'état de départ, vérifier ce que l'action demande, changer d'état.
- **Il n'y a pas d'état « rejetée »** : RG-34 dit qu'une feuille rejetée « revient à l'état de brouillon avec le motif du rejet ». Le motif reste visible jusqu'à la soumission suivante.
- **Le lundi** est un garde-fou (`ArgumentException`), pas une règle du cahier des charges : l'API le vérifiera avant.
- **`Version`** est le jeton du module 08. Le client ne le renvoie pas ici : il protège l'intervalle entre la lecture et l'écriture **au sein d'une requête**. Si deux managers valident et rejettent la même feuille au même instant, le second reçoit `409`.
- **`Periode`** n'a pas de *setter* : EF Core ne l'enregistre pas, elle est recalculée.

Écrivez les tests des transitions sur le modèle du module 04 : le cycle nominal ; le rejet qui rend la feuille à l'état de brouillon avec son motif ; puis **chaque transition interdite** — valider un brouillon, soumettre deux fois, rejeter une feuille validée, rejeter sans motif (`RG-34`), valider sa propre feuille (`RG-36`). Trois états et trois actions font neuf cas : aucun ne doit manquer.

### 2. Les saisies : quatre règles en un seul endroit

`Chronos.Domain/SaisieDeTemps.cs` :

```csharp
namespace Chronos.Domain;

public class SaisieDeTemps
{
    private static readonly decimal[] DureesPermises = [0.25m, 0.5m, 0.75m, 1m];

    internal SaisieDeTemps(int projetId, DateOnly jour, decimal duree)
    {
        if (!DureesPermises.Contains(duree))
        {
            throw new RegleMetierException("RG-30", "Le temps se saisit par quarts de journée : 0,25 ; 0,5 ; 0,75 ou 1.");
        }

        ProjetId = projetId;
        Jour = jour;
        Duree = duree;
    }

    public int Id { get; private set; }
    public int FeuilleDeTempsId { get; private set; }
    public int ProjetId { get; private set; }
    public DateOnly Jour { get; private set; }
    public decimal Duree { get; private set; }
}
```

Le constructeur est `internal` : seul le projet `Chronos.Domain` peut créer une saisie. Ni l'API ni les tests ne le peuvent — ils passent par la feuille.

Les règles RG-31 à RG-33 dépendent de données extérieures à la feuille : le calendrier, les absences, les affectations. Comme au module 09, on les lui **donne**. Ajoutez à `FeuilleDeTemps.cs` :

```csharp
public sealed record SaisieDemandee(int ProjetId, DateOnly Jour, decimal Duree);

public sealed record ContexteDeSaisie(
    CalendrierDeTravail Calendrier,
    IReadOnlyDictionary<DateOnly, decimal> Absences,
    IReadOnlyList<AffectationPlanifiee> Affectations);
```

et à la classe :

```csharp
    public void RemplacerSaisies(IReadOnlyCollection<SaisieDemandee> demandes, ContexteDeSaisie contexte)
    {
        if (Statut != StatutFeuilleDeTemps.Brouillon)
        {
            throw new RegleMetierException("RG-35", "Une feuille soumise ou validée n'est plus modifiable.");
        }

        var saisies = demandes.Select(d => new SaisieDeTemps(d.ProjetId, d.Jour, d.Duree)).ToList();

        foreach (var jour in saisies.GroupBy(s => s.Jour))
        {
            if (!Periode.Contient(jour.Key))
            {
                throw new ArgumentException($"Le {jour.Key:dd/MM/yyyy} n'appartient pas à la semaine de la feuille.", nameof(demandes));
            }

            var total = jour.Sum(s => s.Duree);
            if (total > 1m)
            {
                throw new RegleMetierException("RG-31", $"Le total des saisies du {jour.Key:dd/MM/yyyy} dépasse une journée.");
            }

            var disponible = contexte.Calendrier.EstOuvre(jour.Key)
                ? 1m - contexte.Absences.GetValueOrDefault(jour.Key)
                : 0m;
            if (total > disponible)
            {
                throw new RegleMetierException("RG-32",
                    $"Le {jour.Key:dd/MM/yyyy} n'est pas travaillé, ou pas entièrement : jour non ouvré ou absence approuvée.");
            }

            foreach (var saisie in jour)
            {
                var affecte = contexte.Affectations.Any(a => a.ProjetId == saisie.ProjetId && a.Periode.Contient(saisie.Jour));
                if (!affecte)
                {
                    throw new RegleMetierException("RG-33",
                        $"Le collaborateur n'est pas affecté au projet {saisie.ProjetId} le {saisie.Jour:dd/MM/yyyy}.");
                }
            }
        }

        _saisies.Clear();
        _saisies.AddRange(saisies);
        Version = Guid.NewGuid();
    }
```

- **On remplace tout, ou rien.** La liste n'est touchée qu'à la fin, quand toutes les vérifications sont passées : une feuille n'est jamais à moitié modifiée.
- **RG-32 et les demi-journées** : un jour ouvré offre `1` jour, moins la part d'absence approuvée. Avec une absence le matin, il reste `0.5`.
- **Les tests** s'écrivent sans base : un `ContexteDeSaisie` construit à la main, une ligne par règle. `Assert.Empty(feuille.Saisies)` après un refus vérifie le « ou rien ».

### 3. Enregistrer l'agrégat

Dans `ChronosDbContext`, un seul `DbSet` : `public DbSet<FeuilleDeTemps> FeuillesDeTemps { get; set; }`. Les configurations :

```csharp
public class FeuilleDeTempsConfiguration : IEntityTypeConfiguration<FeuilleDeTemps>
{
    public void Configure(EntityTypeBuilder<FeuilleDeTemps> builder)
    {
        builder.ToTable("FeuillesDeTemps");
        builder.HasIndex(f => new { f.CollaborateurId, f.Semaine }).IsUnique();
        builder.Property(f => f.MotifRejet).HasMaxLength(500);
        builder.Property(f => f.Version).IsConcurrencyToken();
        builder.HasOne<Collaborateur>().WithMany().HasForeignKey(f => f.CollaborateurId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(f => f.Saisies).WithOne().HasForeignKey(s => s.FeuilleDeTempsId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SaisieDeTempsConfiguration : IEntityTypeConfiguration<SaisieDeTemps>
{
    public void Configure(EntityTypeBuilder<SaisieDeTemps> builder)
    {
        builder.ToTable("SaisiesDeTemps");
        builder.Property(s => s.Duree).HasPrecision(3, 2);
        builder.HasIndex(s => new { s.FeuilleDeTempsId, s.ProjetId, s.Jour }).IsUnique();
        builder.HasOne<Projet>().WithMany().HasForeignKey(s => s.ProjetId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

- **L'index unique `(CollaborateurId, Semaine)`** : une feuille par collaborateur et par semaine. C'est lui qui rend RG-31 vérifiable dans l'agrégat — toutes les saisies d'un jour sont forcément dans la même feuille.
- **Le statut** est enregistré en chaîne de 20 caractères au plus par la convention de `ConfigureConventions` (module 06, exercice 2) : rien à écrire pour lui.
- **`Saisies` est lue et écrite par son champ `_saisies`** : EF Core reconnaît ce nom par convention.
- **La cascade est voulue ici**, pour une fois : une saisie n'existe pas sans sa feuille. Et une saisie retirée de la liste est **supprimée** de la base à l'enregistrement.

Créez la migration `FeuillesDeTemps` pour les deux fournisseurs (module 07, étape 10) et appliquez-la.

Documentation : [Champs de stockage](https://learn.microsoft.com/en-us/ef/core/modeling/backing-field), [Suppression en cascade et orphelins](https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete)

### 4. Les endpoints de la feuille

Dans `Chronos.Api/FeuillesDeTemps/`, les DTO `SaisieDto(int ProjetId, DateOnly Jour, decimal Duree)` et `FeuilleDeTempsDto(int Id, int CollaborateurId, DateOnly Semaine, StatutFeuilleDeTemps Statut, string? MotifRejet, IReadOnlyList<SaisieDto> Saisies)`, puis les requêtes :

```csharp
public record SaisieRequete(
    [Required] int? ProjetId,
    [Required] DateOnly? Jour,
    [Required] decimal? Duree);

public record RemplacerSaisiesRequete([Required] IReadOnlyList<SaisieRequete> Saisies);

public record ValiderFeuilleRequete([Required] int? ValideurId);

public record RejeterFeuilleRequete(
    [Required] int? ValideurId,
    [Required, StringLength(500)] string Motif);
```

`MapFeuillesDeTemps` déclare cinq routes sur le groupe `/api/feuilles-de-temps` :

```csharp
groupe.MapGet("/", ObtenirOuCreer);
groupe.MapPut("/{id:int}/saisies", RemplacerSaisies);
groupe.MapPost("/{id:int}/soumission", Soumettre);
groupe.MapPost("/{id:int}/validation", Valider);
groupe.MapPost("/{id:int}/rejet", Rejeter);
```

**`ObtenirOuCreer(int collaborateurId, DateOnly semaine, …)`** refuse par un `400` une `semaine` qui n'est pas un lundi ou un collaborateur inconnu, cherche la feuille (avec `.Include(f => f.Saisies)`), et la crée à l'état de brouillon si elle n'existe pas. Un `GET` qui écrit n'est pas orthodoxe ; la question est posée dans « Pour la séance ».

**Une action** tient en quelques lignes, toujours les mêmes :

```csharp
private static async Task<Results<Ok<FeuilleDeTempsDto>, NotFound>> Valider(
    int id, ValiderFeuilleRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
{
    var feuille = await db.FeuillesDeTemps
        .Include(f => f.Saisies)
        .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    if (feuille is null)
    {
        return TypedResults.NotFound();
    }

    feuille.Valider(requete.ValideurId!.Value);
    await db.SaveChangesAsync(cancellationToken);

    return TypedResults.Ok(FeuilleDeTempsDto.Depuis(feuille));
}
```

Charger, demander à l'entité, enregistrer. L'endpoint ne connaît aucune règle : `RG-34` et `RG-36` remontent en `422` par le gestionnaire du module 08. `Depuis` est une méthode statique du DTO qui recopie la feuille chargée.

**`RemplacerSaisies`** a plus à faire, parce qu'il doit réunir le contexte :

```csharp
var demandes = requete.Saisies
    .Select(s => new SaisieDemandee(s.ProjetId!.Value, s.Jour!.Value, s.Duree!.Value))
    .ToList();

if (demandes.Any(d => !feuille.Periode.Contient(d.Jour)))
{
    return Invalide("Saisies", "Toutes les saisies doivent porter sur la semaine de la feuille.");
}

var idsProjets = demandes.Select(d => d.ProjetId).Distinct().ToList();
var projets = await db.Projets.Where(p => idsProjets.Contains(p.Id)).ToListAsync(cancellationToken);
if (projets.Count != idsProjets.Count)
{
    return Invalide("Saisies", "Au moins une saisie porte sur un projet inconnu.");
}

foreach (var projet in projets)
{
    projet.VerifierOuvert();
}

var contexte = await ChargerContexte(db, feuille.CollaborateurId, feuille.Periode, cancellationToken);

feuille.RemplacerSaisies(demandes, contexte);
await db.SaveChangesAsync(cancellationToken);
```

`ChargerContexte` fait trois requêtes, bornées à la semaine : les jours fériés, les affectations du collaborateur (projetées en `AffectationPlanifiee`, comme au module 09), ses absences approuvées — une liste vide pour l'instant, que l'étape 9 remplira.

Vérifiez les règles une à une, puis **l'atomicité**. Enregistrez quatre saisies valides. Envoyez ensuite deux saisies pour le même projet et le même jour (`0.25` et `0.25`) : le domaine accepte, l'index unique refuse, la réponse est `409`. Relisez la feuille : les quatre saisies d'origine sont toujours là. Les `DELETE` qui devaient les remplacer faisaient partie du même `SaveChangesAsync` ; ils ont été annulés avec le reste. Vous n'avez rien écrit pour cela.

### 5. Le cycle complet

Rejouez le parcours, en notant les réponses. Pour Camille (collaborateur 2, manager 1), semaine du 4 mai 2026 :

| Appel | Réponse |
| --- | --- |
| `GET /api/feuilles-de-temps?collaborateurId=2&semaine=2026-05-04` | `200`, statut `Brouillon`, aucune saisie |
| `PUT …/1/saisies` avec `0.3` jour | `422`, `RG-30` |
| `PUT …/1/saisies` avec `0.75` + `0.5` le mardi | `422`, `RG-31` |
| `PUT …/1/saisies` le vendredi 8 mai (férié) | `422`, `RG-32` |
| `PUT …/1/saisies` sur un projet non affecté (créez-en un troisième par `POST /api/projets` : Camille est affectée aux deux autres toute la semaine) | `422`, `RG-33` |
| `PUT …/1/saisies` sur un projet qui n'existe pas | `400`, erreur sur `Saisies` |
| `PUT …/1/saisies` avec `{ "saisies": [ { "projetId": 1, "jour": "2026-05-05" } ] }` | `400`, erreur sur `Saisies[0].Duree` |
| `POST …/1/validation` sur le brouillon | `422`, `RG-34` |
| `POST …/1/soumission` | `200`, statut `Soumise` |
| `PUT …/1/saisies` | `422`, `RG-35` |
| `POST …/1/validation` avec `{ "valideurId": 2 }` | `422`, `RG-36` |
| `POST …/1/rejet` avec `{ "valideurId": 1, "motif": "Il manque le jeudi." }` | `200`, statut `Brouillon`, `motifRejet` renseigné |
| `PUT` corrigé, du mardi au jeudi (Camille sera en congé le lundi, étape 9), `POST …/soumission`, `POST …/validation` avec `{ "valideurId": 1 }` | `200`, statut `Validee` |

### 6. L'absence

`Chronos.Domain/Absence.cs`, avec `StatutAbsence` (`Demandee`, `Approuvee`, `Refusee`, `Annulee`) et `TypeAbsence` (`Id`, `Code`, `Libelle`) :

```csharp
public class Absence
{
    public Absence(int collaborateurId, int typeAbsenceId, DateOnly dateDebut, DateOnly dateFin,
        bool debuteApresMidi, bool finitLeMatin)
    {
        if (dateFin < dateDebut)
        {
            throw new RegleMetierException("RG-42", "La fin d'une absence est postérieure ou égale à son début.");
        }
        if (dateDebut == dateFin && debuteApresMidi && finitLeMatin)
        {
            throw new RegleMetierException("RG-42", "Une absence dure au moins une demi-journée.");
        }

        CollaborateurId = collaborateurId;
        TypeAbsenceId = typeAbsenceId;
        DateDebut = dateDebut;
        DateFin = dateFin;
        DebuteApresMidi = debuteApresMidi;
        FinitLeMatin = finitLeMatin;
        Statut = StatutAbsence.Demandee;
    }

    // Les propriétés : Id, puis une par paramètre du constructeur, Statut, et Version comme dans la feuille
    // (Guid.NewGuid() à la création, puis dans chaque transition). Setters privés.

    public Periode Periode => new(DateDebut, DateFin);

    public decimal PartAbsente(DateOnly jour)
    {
        if (!Periode.Contient(jour))
        {
            return 0m;
        }

        var part = 1m;
        if (jour == DateDebut && DebuteApresMidi)
        {
            part -= 0.5m;
        }
        if (jour == DateFin && FinitLeMatin)
        {
            part -= 0.5m;
        }
        return part;
    }

    public decimal DureeEnJours(CalendrierDeTravail calendrier) =>
        calendrier.JoursOuvres(Periode).Sum(PartAbsente);

    public void Approuver()
    {
        Exiger(StatutAbsence.Demandee, "Seule une absence demandée peut être approuvée.");
        Statut = StatutAbsence.Approuvee;
    }

    public void Annuler(DateOnly aujourdhui)
    {
        if (Statut is not (StatutAbsence.Demandee or StatutAbsence.Approuvee))
        {
            throw new RegleMetierException("RG-40", "Seule une absence demandée ou approuvée peut être annulée.");
        }
        if (DateDebut <= aujourdhui)
        {
            throw new RegleMetierException("RG-40", "Une absence ne peut plus être annulée une fois commencée.");
        }

        Statut = StatutAbsence.Annulee;
    }
}
```

`Refuser()` s'écrit comme `Approuver()`, et `Exiger` comme dans la feuille, avec le code `RG-40`.

- **Les demi-journées** tiennent en deux booléens ; `PartAbsente` les traduit en `0`, `0.5` ou `1` pour un jour donné. Du jeudi après-midi au lundi matin, avec un vendredi férié : `DureeEnJours` rend `1` (RG-42).
- **`Annuler` reçoit la date du jour** : c'est l'endpoint qui la demande à `TimeProvider` (`horloge.Aujourdhui()`, module 09). Le domaine reste pur et le test choisit sa date.
- **Pas de motif médical, pas de commentaire libre** : le cahier des charges (§ 6) demande de ne collecter que le nécessaire.
- **`Version`** joue le même rôle que dans la feuille : un manager qui refuse une absence pendant qu'un autre l'approuve reçoit `409` au lieu d'écraser la décision de l'autre. Seule l'approbation passera par une transaction (étape 8) ; le refus et l'annulation n'ont que ce jeton pour se protéger.

Ajoutez enfin la méthode statique `PartsAbsentes(IEnumerable<Absence> absences, Periode periode)`, qui rend un `IReadOnlyDictionary<DateOnly, decimal>` : pour chaque jour commun à une absence et à la période (`Periode.Intersection`), la somme des `PartAbsente`, plafonnée à `1`. C'est la forme sous laquelle le calcul de charge et la feuille de temps attendent les absences. Testez-la sur l'absence du jeudi après-midi au lundi matin : `0.5` le jeudi, `1` le vendredi, rien pour un lundi hors période.

Configurez `Absence` et `TypeAbsence` (`Version` en jeton de concurrence, relations en `Restrict`, code unique ; le statut suit la convention), ajoutez trois types aux données initiales (`CP`, `RTT`, `MAL`), créez les migrations. Attention : `DonneesInitiales.Alimenter` s'arrête dès qu'un collaborateur existe ; sur votre base, déjà alimentée, les types n'apparaîtraient jamais. Donnez-leur leur propre condition (« si la table des types est vide »), indépendante de celle des collaborateurs, dans les deux méthodes, synchrone et asynchrone.

### 7. RG-41 : deux demandes au même instant

La requête `CreerAbsenceRequete` porte `CollaborateurId`, `TypeAbsenceId`, `DateDebut` et `DateFin` (nullables et `[Required]`, comme au module 08), puis `bool DebuteApresMidi = false` et `bool FinitLeMatin = false`. Écrivez d'abord `POST /api/absences` de la façon la plus directe : construire l'`Absence` (nommée `demande`), vérifier qu'aucune autre ne la chevauche, l'ajouter, enregistrer. La vérification est une requête `AnyAsync` sur les absences demandées ou approuvées du même collaborateur dont les dates croisent celles de la demande ; elle lève `RG-41`. Elle figure telle quelle dans le gestionnaire final, plus bas.

Deux demandes envoyées l'une après l'autre : `201`, puis `422`. La règle semble tenue.

**Provoquez maintenant la course.** Entre la vérification et `db.Absences.Add(…)`, ajoutez provisoirement `await Task.Delay(5000, cancellationToken);`. Préparez deux fichiers, `absence-1.json` (du 4 au 8 janvier 2027) et `absence-2.json` (du 6 au 12), pour le même collaborateur, et lancez dans deux terminaux, à moins de cinq secondes d'intervalle :

```powershell
curl.exe -i -X POST http://localhost:5080/api/absences -H "Content-Type: application/json" -d "@absence-1.json"
```

Les deux répondent `201`. Le collaborateur a deux absences qui se chevauchent : chacune des deux requêtes a vérifié **avant** que l'autre n'écrive. Le délai ne crée pas le défaut, il le rend visible ; sans lui, il se produirait un jour sur mille. Et aucun index unique ne peut l'empêcher : un index compare des valeurs égales, pas des intervalles.

**Première correction : une transaction sérialisable.** Entourez le tout :

```csharp
await using var transaction =
    await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
// vérifier, ajouter, enregistrer
await transaction.CommitAsync(cancellationToken);
```

(`using System.Data;` pour `IsolationLevel`.) Si une exception survient avant `CommitAsync`, la transaction est annulée quand la variable sort de portée. Recommencez l'expérience : `201` pour l'une, **`500`** pour l'autre, et dans le journal de PostgreSQL :

```text
40001: could not serialize access due to read/write dependencies among transactions
```

La base a fait son travail : une seule absence est enregistrée. Mais elle a refusé la seconde transaction en disant « recommencez », et personne n'a recommencé.

**Seconde correction : laisser EF Core recommencer.** Dans `Program.cs`, activez les nouvelles tentatives sur les deux branches du `switch` du module 07 :

```csharp
        "PostgreSql" => options.UseNpgsql(chaineDeConnexion, npgsql => npgsql
            .MigrationsAssembly("Chronos.Migrations.PostgreSql")
            .EnableRetryOnFailure()),

        "SqlServer" => options.UseSqlServer(chaineDeConnexion, sqlServer => sqlServer
            .MigrationsAssembly("Chronos.Migrations.SqlServer")
            .EnableRetryOnFailure()),
```

Relancez et envoyez une seule demande : `500`.

```text
InvalidOperationException: The configured execution strategy 'NpgsqlRetryingExecutionStrategy' does not support user-initiated transactions. Use the execution strategy returned by 'DbContext.Database.CreateExecutionStrategy()' to execute all the operations in the transaction as a retriable unit.
```

Le message dit quoi faire. Voici le gestionnaire final :

```csharp
private static async Task<Created<AbsenceDto>> Demander(
    CreerAbsenceRequete requete, ChronosDbContext db, CancellationToken cancellationToken)
{
    var strategie = db.Database.CreateExecutionStrategy();

    var absence = await strategie.ExecuteAsync(async () =>
    {
        db.ChangeTracker.Clear();
        await using var transaction =
            await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var demande = new Absence(requete.CollaborateurId!.Value, requete.TypeAbsenceId!.Value,
            requete.DateDebut!.Value, requete.DateFin!.Value, requete.DebuteApresMidi, requete.FinitLeMatin);

        var chevauche = await db.Absences.AnyAsync(a =>
            a.CollaborateurId == demande.CollaborateurId
            && (a.Statut == StatutAbsence.Demandee || a.Statut == StatutAbsence.Approuvee)
            && a.DateDebut <= demande.DateFin && a.DateFin >= demande.DateDebut, cancellationToken);
        if (chevauche)
        {
            throw new RegleMetierException("RG-41", "Cette absence en chevauche une autre du même collaborateur.");
        }

        db.Absences.Add(demande);
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return demande;
    });

    return TypedResults.Created($"/api/absences/{absence.Id}", AbsenceDto.Depuis(absence));
}
```

- **Tout ce qui est entre accolades peut être exécuté deux fois.** Il ne doit donc rien supposer d'un passage précédent : `ChangeTracker.Clear()` oublie les entités de la tentative ratée, et l'`Absence` est créée **dans** la fonction.
- **La transaction perdante est rejouée**, et cette fois sa vérification voit l'absence de l'autre : elle lève `RG-41`. Une `RegleMetierException` n'est pas un échec passager ; elle n'est pas rejouée et remonte normalement.

Dernière expérience, délai toujours en place : `201` pour l'une, **`422` avec `"regle": "RG-41"`** pour l'autre. C'est exactement la réponse qu'aurait reçue la seconde si elle était arrivée une minute plus tard. Sur SQL Server, le journal montre un interblocage (erreur 1205) au lieu de l'erreur 40001 ; la réponse au client est la même. Retirez le `Task.Delay`.

### 8. RG-43 : approuver une absence

Trois actions, `POST /api/absences/{id}/approbation`, `…/refus`, `…/annulation`. Les deux dernières s'écrivent comme les actions de la feuille. La première lit des feuilles de temps pour décider du sort d'une absence : même schéma qu'à l'étape 7. Plutôt que de le recopier, rangez-le dans `ChronosDbContext` :

```csharp
public Task<T> EnTransactionSerialisableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
{
    var strategie = Database.CreateExecutionStrategy();

    return strategie.ExecuteAsync(async () =>
    {
        ChangeTracker.Clear();
        await using var transaction =
            await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var resultat = await operation();

        await transaction.CommitAsync(cancellationToken);
        return resultat;
    });
}
```

Faites-y passer aussi `Demander` (étape 7) : son corps devient la fonction confiée à `EnTransactionSerialisableAsync`. L'approbation se lit alors d'un trait :

```csharp
var absence = await db.EnTransactionSerialisableAsync(async () =>
{
    var absence = await db.Absences.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    if (absence is null)
    {
        return null;
    }

    var tempsDejaValide = await db.FeuillesDeTemps.AnyAsync(f =>
        f.CollaborateurId == absence.CollaborateurId
        && f.Statut == StatutFeuilleDeTemps.Validee
        && f.Saisies.Any(s => s.Jour >= absence.DateDebut && s.Jour <= absence.DateFin), cancellationToken);
    if (tempsDejaValide)
    {
        throw new RegleMetierException("RG-43", "Du temps a déjà été validé sur la période de cette absence.");
    }

    absence.Approuver();
    await db.SaveChangesAsync(cancellationToken);
    return absence;
}, cancellationToken);

return absence is null ? TypedResults.NotFound() : TypedResults.Ok(AbsenceDto.Depuis(absence));
```

Vérifiez : la feuille de Camille de l'étape 5 est validée, avec du temps le mardi 5 mai. Demandez une absence ce jour-là, puis approuvez-la : `422`, `RG-43`. Refusez-la : `200`.

### 9. Les absences entrent dans la capacité

Deux endroits attendaient une liste d'absences. Dans `ChargerContexte` (étape 4) et dans le plan de charge (module 09), ajoutez la requête :

```csharp
var absences = await db.Absences
    .AsNoTracking()
    .Where(a => ids.Contains(a.CollaborateurId) && a.Statut == StatutAbsence.Approuvee
        && a.DateDebut <= fin && a.DateFin >= debut)
    .ToListAsync(cancellationToken);
```

(dans `ChargerContexte`, le filtre porte sur le seul collaborateur de la feuille et sur sa semaine, et le résultat passe par `Absence.PartsAbsentes`). Dans le plan de charge, remplacez le dictionnaire vide du `DossierDeCharge` par `Absence.PartsAbsentes(absencesParCollaborateur[c.Id], periode)`, après un `ToLookup` sur `CollaborateurId`. Le plan de charge passe à quatre requêtes, toujours indépendantes de la taille de l'équipe.

Donnez à Camille son congé du lundi 4 mai, approuvez-le, et rappelez `GET /api/plan-de-charge?du=2026-05-04&au=2026-05-10` :

```json
{ "debut": "2026-05-04", "fin": "2026-05-10", "capacite": 2.40, "chargePlanifiee": 3.2000, "tauxDeCharge": 1.3333, "surcharge": true, "intercontrat": false }
```

2,4 jours de capacité, 3,2 jours de charge, 133 % : l'exemple du cahier des charges, tel que le module 09 l'a tranché, sort maintenant de l'API. Essayez enfin de saisir du temps le lundi 4 : `422`, mais `RG-35`, car la feuille de Camille est validée depuis l'étape 5 et ce contrôle passe le premier. Sur un brouillon, c'est `RG-32` qui répondrait, comme le montre le test unitaire de l'étape 2 avec une absence dans le contexte.

## À vous

1. **Compléter l'API.** `GET /api/absences?collaborateurId=`, `GET /api/types-absence`, le refus et l'annulation, et `GET /api/feuilles-de-temps/soumises`, la liste des feuilles à valider — attention à ce que cette route ne crée rien, contrairement à la lecture par semaine. Ajoutez les saisies et les absences (les affectations y sont depuis l'exercice 1 du module 09) à la condition d'archivage de `DELETE /api/collaborateurs/{id}` (RG-05) ; une feuille, même vide, compte, car sa clé étrangère interdirait de toute façon la suppression. Vérifiez-le par HTTP ; le test automatisé viendra avec les tests d'intégration du [module 11](11-tests-d-integration.md) (exercice 2).

2. **Des absences qui se touchent sans se chevaucher.** Une absence qui finit le mercredi matin et une autre qui débute le mercredi après-midi sont aujourd'hui refusées. Écrivez `Absence.Chevauche(Absence autre)` dans le domaine, avec ses tests, et utilisez-la après avoir chargé les absences candidates. Faites de même pour RG-43 : une demi-journée d'absence est-elle compatible avec une demi-journée de temps validé le même jour ? N'oubliez pas l'autre demi-journée : une absence approuvée le matin, du temps validé, puis une absence l'après-midi ne doivent pas faire plus d'un jour (`Absence.PartsAbsentes` additionne déjà les absences d'un même jour).

3. **Une absence sans jour ouvré.** Une demande du samedi au dimanche a une durée de zéro. Refusez-la (`RG-42`). Où placez-vous la vérification, sachant qu'elle a besoin du calendrier ?

4. **Le temps consommé.** Le § 4.4 demande de comparer le temps consommé au budget du projet. Ajoutez à `ProjetDto` le total des jours **validés**, calculé par la base, sans exposer de `DbSet<SaisieDeTemps>`. Combien de requêtes pour une page de vingt projets ?

5. **La course inverse.** Un manager approuve l'absence de Camille pendant qu'un autre valide sa feuille de la même semaine. Écrivez le déroulement, instant par instant, qui viole RG-43 malgré l'étape 8. Que doit **lire** la validation pour que la base détecte le conflit ? Corrigez, puis provoquez la course avec un délai, comme à l'étape 7.

6. **Rejouer sans dégât.** Dans la fonction confiée à la stratégie d'exécution, déplacez la création de l'`Absence` **avant** `ExecuteAsync` et retirez `ChangeTracker.Clear()`. Rejouez l'expérience de l'étape 7 et expliquez ce que vous observez. Que se passerait-il si la fonction envoyait un courriel ?

## Critères de réussite

- Les tests unitaires couvrent les neuf combinaisons état × action de la feuille, RG-30 à RG-33, RG-35, RG-36, le cycle d'une absence et sa durée en demi-journées — sans base de données.
- Le parcours de l'étape 5 donne les codes indiqués, et un `PUT` refusé laisse les saisies intactes.
- Aucun endpoint ne modifie un statut par affectation, et il n'existe ni `DbSet<SaisieDeTemps>` ni route `/api/saisies`.
- Avec le délai de l'étape 7, deux demandes d'absence simultanées qui se chevauchent donnent `201` et `422` (`RG-41`), et une seule ligne en base — sur PostgreSQL et sur SQL Server.
- Approuver une absence sur une période où du temps est validé répond `422` (`RG-43`).
- Le plan de charge de la semaine du 4 mai 2026 affiche pour Camille 2,4 jours de capacité et un taux de 1,3333.
- `dotnet ef migrations has-pending-model-changes` ne signale rien, pour chacun des deux jeux.
- Comparez votre travail à la solution de référence : `git diff module-09 module-10`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `PUT /api/feuilles-de-temps/{id}` avec `{ "statut": "Validee" }` | Le client choisit l'état d'arrivée ; les règles de transition se retrouvent dans l'API. | Un endpoint par action, une méthode par transition. |
| Les transitions écrites dans l'endpoint (`if (feuille.Statut == …)`) | La règle sera oubliée par le prochain endpoint, l'import, le traitement de nuit. | Dans l'entité. |
| Un état `Rejetee` | Le cahier des charges n'en veut pas : une feuille rejetée est un brouillon qui porte un motif. | Lire la règle avant de dessiner les états. |
| Modifier une saisie directement (`DbSet<SaisieDeTemps>`, `/api/saisies/{id}`) | RG-31 et RG-35 ne sont plus vérifiées. | Passer par la racine de l'agrégat. |
| Ouvrir une transaction autour d'un seul `SaveChangesAsync` | Inutile : il est déjà atomique. | Une transaction explicite seulement quand une **lecture** conditionne l'écriture, ou pour plusieurs enregistrements. |
| « J'ai mis une transaction, donc c'est sûr » | Au niveau par défaut, deux transactions lisent le même état et écrivent toutes les deux. | Le niveau d'isolation qui convient, et l'expérience qui le prouve. |
| Un niveau sérialisable sans nouvelle tentative | L'une des deux requêtes reçoit une erreur 500. Constaté. | Stratégie d'exécution, ou rattraper et recommencer soi-même. |
| `BeginTransactionAsync` hors de la stratégie quand `EnableRetryOnFailure` est actif | `InvalidOperationException` à la première transaction. Constaté. | `CreateExecutionStrategy().ExecuteAsync(…)`. |
| Une fonction rejouable qui garde un état d'une tentative à l'autre | Entités ajoutées deux fois, effets extérieurs répétés. | Tout créer dans la fonction ; `ChangeTracker.Clear()` au début. |
| Tout passer en sérialisable « par précaution » | Plus de conflits, plus de tentatives, des verrous plus larges sur SQL Server. | Le réserver aux opérations qui lisent pour décider. |
| `DateTime.Today` dans `Annuler()` | Le domaine dépend de l'horloge ; le test ne peut pas choisir sa date. | Recevoir la date en paramètre ; l'endpoint la demande à `TimeProvider`. |
| `TransactionScope` autour d'appels asynchrones | Sans option particulière, la transaction ne suit pas les `await`. | `BeginTransactionAsync`. |
| Tester la concurrence en envoyant deux requêtes « très vite » | La course ne se produit presque jamais sur un poste de développement : le test passe pour de mauvaises raisons. | Un délai provisoire qui élargit la fenêtre. |

## Pour la séance

- Dessinez de mémoire les deux machines à états. Où RG-34 et RG-40 se ressemblent-elles, où diffèrent-elles ? Une absence approuvée peut être annulée : une feuille validée peut-elle être rouverte ? Qui le décide ?
- `GET /api/feuilles-de-temps` crée la feuille si elle n'existe pas. Quelles propriétés d'un `GET` cela met-il à mal ? Quelles autres conceptions voyez-vous, et que coûtent-elles à l'interface ?
- Le valideur est passé dans le corps de la requête. Qu'est-ce qui empêche aujourd'hui Camille de valider sa feuille en écrivant `"valideurId": 1` ? Que faudra-t-il au module 12 ?
- Une absence est approuvée sur un jour où une feuille **en brouillon** contient déjà une saisie. RG-43 ne l'interdit pas. Que devient la feuille ? À quel moment faudrait-il revérifier RG-32 ?
- Pourquoi l'index unique suffisait-il pour RG-01, et pas pour RG-41 ? Connaissez-vous, dans PostgreSQL, un type de contrainte qui saurait exprimer RG-41 ? Pourquoi le cours ne l'utilise-t-il pas ?
- La seconde requête de l'étape 7 a été exécutée deux fois sans que le client le sache. Est-ce acceptable ici ? Donnez un exemple où ce ne le serait pas.
- La feuille et ses saisies forment un agrégat. Pourquoi le collaborateur et ses absences n'en forment-ils pas un ? Qu'y gagnerait-on pour RG-41, qu'y perdrait-on ?

## Pour aller plus loin

- [Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions) : points de sauvegarde, transactions partagées entre contextes, et les limites de `TransactionScope`.
- [Résilience des connexions](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency) : l'échec **pendant** la validation de la transaction, et `ExecuteInTransactionAsync` qui vérifie si elle a abouti.
- [Gérer les conflits de concurrence](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) : la section sur les niveaux d'isolation compare les deux façons de protéger une lecture.
- [Chargement des données liées](https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager) : `Include`, `ThenInclude`, et leurs filtres.
- [`BeginTransactionAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.relationaldatabasefacadeextensions.begintransactionasync) : la surcharge qui reçoit le niveau d'isolation.
- Suite dans le cours : le module 11 rejoue ces parcours en tests d'intégration, sur les deux SGBD ; le module 12 remplace `valideurId` par l'utilisateur connecté et achève RG-36 ; le module 18 construit l'écran de la feuille de temps.
