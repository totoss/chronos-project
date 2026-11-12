# Module 18 — Écrans métier

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md).
> Le code de ce module a été **rejoué le 12 novembre 2026 sur la solution de référence** (Angular 22.2.6, CLI 22.2.7, Angular Material 22.2.7, TypeScript 6.0.3, Vitest 5.1.0, Node.js 24.22.3), à la suite de celui des modules 16 et 17, exercices 1 à 5 compris : `ng build` réussit (avec l'avertissement de budget) et les 194 tests passent ; les calculs de dates passent sous six fuseaux horaires, de UTC−11 à UTC+14. Les écrans ont été parcourus dans Chromium contre la **véritable API .NET** du module 12 (PostgreSQL 18), avec les comptes de Camille (collaboratrice) et de Sophie (sa manager) : saisie, refus `422`, soumission, rejet, validation, plan de charge par semaine et par mois, confirmation de surcharge. L'ajustement que ce module apporte à l'API (RG-05 ne compte plus une feuille vide) a été appliqué avec ses tests d'intégration, sans migration. Signalez au mentor tout écart constaté.

Les modules 16 et 17 ont produit des écrans de gestion : une liste, une fiche, un formulaire. Les deux écrans de ce module sont d'une autre nature. La **feuille de temps** est une grille que le collaborateur remplit chaque semaine ; le **plan de charge** est le tableau que le manager consulte pour savoir qui est surchargé et qui est sans mission. Tous deux manipulent beaucoup d'état dérivé — totaux, jours fermés, niveaux de charge — et partagent une même information : la semaine affichée.

## Objectifs

À la fin de ce module, vous saurez :

- découper un écran en un composant « écran », qui parle à l'API, et des composants de présentation, qui ne la connaissent pas ;
- dériver totaux et indicateurs avec `computed`, sans jamais les stocker ;
- manipuler des dates calendaires sans bibliothèque et sans erreur de fuseau ;
- partager un état entre deux écrans par un service à signals, et dire quand ce service est justifié ;
- signaler une information autrement que par la couleur ;
- ouvrir un dialogue de confirmation et en exploiter la réponse.

## Prérequis

- [Module 16](16-dialoguer-avec-l-api.md) et [module 17](17-formulaires.md) : `httpResource`, services d'accès à l'API, `erreursInterceptor`, `linkedSignal`.
- [Module 15](15-routage.md), exercice 1 : les routes `/feuille-de-temps` et `/plan-de-charge` existent, avec un composant provisoire et une entrée de menu.
- [Module 09](09-affectations-et-calcul-de-charge.md) et [module 10](10-temps-et-absences.md) : l'API expose le plan de charge, les affectations, les jours fériés et les feuilles de temps.
- Une session ouverte à la main (module 16, étape 2) : avec un compte dont la fiche a des affectations pour la feuille de temps, avec un compte de manager pour le plan de charge. La base de démonstration n'a aucune affectation : créez-en d'abord par `POST /api/affectations` (module 09), par exemple deux pour Camille, sur deux projets.

## Règles de gestion

| Règle | Ce que fait l'interface |
| --- | --- |
| **RG-30** — saisie par quarts de journée | Chaque case ne propose que 0,25 ; 0,5 ; 0,75 ; 1. |
| **RG-31** — pas plus d'un jour par journée | Total par jour calculé en direct ; dépassement signalé, enregistrement bloqué. |
| **RG-32** — pas de saisie un jour non travaillé | Week-ends et jours fériés grisés et fermés ; les absences sont l'exercice 1. |
| **RG-33** — saisie sur ses seules affectations | Les lignes de la grille sont les affectations de la semaine ; hors de la période d'affectation, la case est fermée. |
| **RG-34**, **RG-35** — cycle de la feuille | État affiché ; une feuille soumise ou validée est en lecture seule ; le motif d'un rejet est affiché. |
| **RG-24** — surcharge à confirmer | Taux supérieur à 100 % signalé ; le manager confirme dans un dialogue. |
| **RG-25** — intercontrat | Signalé dans le plan de charge. |

Dans tous les cas, **c'est l'API qui décide** : elle refuse par un `422` ce que l'interface aurait laissé passer. L'interface évite seulement de proposer ce qui serait refusé.

## Notions

### Composants « écran » et composants de présentation

Un composant **écran** est celui qu'une route affiche. Il charge les données, porte l'état de la page, appelle l'API. Un composant de **présentation** reçoit des données par ses entrées et prévient par ses sorties ; il ne sait pas d'où viennent les données ni ce qu'on en fera.

La grille de saisie est un composant de présentation : on lui donne des jours, des lignes et des durées, elle rend la main quand une case change. On peut la tester sans API, la réutiliser pour l'écran où le manager relit une feuille, et la lire sans se demander ce qu'elle déclenche.

La limite n'est pas une règle absolue. Un écran de dix lignes n'a pas besoin d'être découpé. Le signal du découpage, c'est un template qui mélange deux préoccupations : « quelles données ? » et « comment les montrer ? ».

Documentation : [guide de style — des composants centrés sur la présentation](https://angular.dev/style-guide), [entrées et `model()`](https://angular.dev/guide/components/inputs)

### L'état dérivé ne se stocke pas

Le total d'une journée, la liste des jours fermés, le droit d'enregistrer : tout cela se **déduit** des saisies, du calendrier et de l'état de la feuille. Ce sont des `computed`. Les stocker dans des signals qu'on mettrait à jour « au bon moment » revient à créer des copies qui finiront par diverger.

Une seule chose se stocke : la **copie de travail** des saisies, que l'utilisateur modifie avant d'enregistrer. Elle repart de la feuille chargée à chaque rechargement : c'est un `linkedSignal`, comme la page de la liste au module 16.

Les calculs eux-mêmes sont de simples fonctions, rangées dans un fichier sans Angular : faciles à lire, faciles à tester.

Documentation : [signals et `computed`](https://angular.dev/guide/signals), [`linkedSignal`](https://angular.dev/guide/signals/linked-signal)

### Un service d'état à signals

La semaine affichée intéresse deux écrans : en passant de la feuille de temps au plan de charge, on s'attend à rester sur la même semaine. Aucun des deux composants ne peut la posséder — ils ne sont jamais affichés ensemble. Elle va dans un **service** : une instance unique, que les deux écrans reçoivent par `inject()`.

Sa forme est toujours la même :

- le signal modifiable est **privé** ;
- le service en expose une vue en **lecture seule**, par `asReadonly()` ;
- les modifications passent par des **méthodes** qui disent ce qu'elles font : `decaler(1)`, pas `semaine.set(...)`.

Ainsi, on trouve en un seul endroit toutes les façons de changer la semaine.

Un tel service est justifié quand un état est **partagé** par des composants qui n'ont pas de parent commun commode, ou doit **survivre** à la destruction d'un écran. Il ne l'est pas pour l'état d'un seul écran (la grille en cours de saisie), ni pour des données du serveur : `httpResource` s'en charge. Et si l'état doit survivre au rechargement de la page ou se partager par un lien, sa place est dans l'URL (module 15).

Chronos n'utilise pas de bibliothèque de gestion d'état : des signals dans des services suffisent à cette taille, et la documentation d'Angular n'en recommande aucune.

Documentation : [signals en lecture seule](https://angular.dev/guide/signals), [services](https://angular.dev/guide/di/creating-and-using-services)

### Des dates sans heure

Une feuille de temps parle de **jours** : le lundi 4 mai 2026, sans heure ni fuseau. JavaScript n'a pas encore, partout, de type pour cela. L'API `Temporal` (`Temporal.PlainDate`) est faite pour ce besoin, mais elle n'est pas utilisable dans Chronos aujourd'hui : MDN la classe en disponibilité limitée, et l'essai montre qu'elle n'existe pas dans Node.js 24, où tournent les tests.

Reste `Date`, qui est un **instant**. Deux pièges classiques :

- `new Date(2026, 4, 4)` est à minuit, heure locale ; `toISOString()` le convertit en UTC et donne, à Paris, `2026-05-03T22:00:00Z` — la veille ;
- ajouter 24 heures à une date ne donne pas toujours le lendemain : un jour de changement d'heure dure 23 ou 25 heures.

La parade retenue : **la date circule sous forme de chaîne `aaaa-mm-jj`** — c'est déjà le format de l'API —, et les rares calculs se font en **UTC**, où un jour dure toujours 24 heures. Ces chaînes se comparent directement (`'2026-05-04' <= jour`), ce qui suffit à presque tout. Elles passent aussi telles quelles dans les URL : l'API ne comprend pas `04/05/2026`.

Documentation : [`Date`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date), [`Temporal`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Temporal), [`Intl.DateTimeFormat`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl/DateTimeFormat)

### L'interface ne refait pas le calcul

La capacité, la charge planifiée et le taux de charge sont calculés par l'API, avec des tests, au module 09. Le plan de charge **affiche** `tauxDeCharge`, `surcharge` et `intercontrat` ; il ne recalcule ni l'un ni l'autre. Écrire `taux > 1` dans un template, c'est déjà dupliquer RG-24.

### Jamais la couleur seule

Une case rouge ne dit rien à un utilisateur daltonien, à un lecteur d'écran, ni sur une impression en noir et blanc. Chaque niveau de charge porte donc un **libellé** (« Surcharge », « Intercontrat ») ; la couleur vient en renfort. C'est une exigence des règles d'accessibilité, que le module 21 reprend en entier.

Documentation : [WCAG 1.4.1 — usage de la couleur](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html)

### Dialogues

`MatDialog` ouvre un composant dans une fenêtre modale. On lui passe des données (`data`, relues par `inject(MAT_DIALOG_DATA)`) ; il renvoie un résultat quand il se ferme (`afterClosed()`). Material gère le reste : le focus est enfermé dans le dialogue, `Échap` le ferme, le focus revient ensuite à l'élément d'origine.

Documentation : [dialogue](https://material.angular.dev/components/dialog/overview)

## Pas à pas

### 1. Les dates

`src/app/commun/dates.ts` :

```ts
/**
 * Dates calendaires au format ISO « aaaa-mm-jj », comme les échange l'API.
 * Tous les calculs passent par l'UTC : le résultat ne dépend pas du fuseau du poste.
 */

function versDate(iso: string): Date {
  return new Date(`${iso}T00:00:00Z`);
}

function versIso(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function ajouterJours(iso: string, jours: number): string {
  const date = versDate(iso);
  date.setUTCDate(date.getUTCDate() + jours);
  return versIso(date);
}

/** Le lundi de la semaine qui contient la date. */
export function lundiDe(iso: string): string {
  const jour = versDate(iso).getUTCDay(); // 0 = dimanche, 1 = lundi … 6 = samedi
  return ajouterJours(iso, -((jour + 6) % 7));
}

export function estWeekEnd(iso: string): boolean {
  const jour = versDate(iso).getUTCDay();
  return jour === 0 || jour === 6;
}

/** La date du jour, telle que la lit l'utilisateur sur son calendrier. */
export function aujourdhui(): string {
  const maintenant = new Date();
  return versIso(
    new Date(Date.UTC(maintenant.getFullYear(), maintenant.getMonth(), maintenant.getDate())),
  );
}

const formatJour = new Intl.DateTimeFormat('fr-FR', {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  timeZone: 'UTC',
});

/** « lun. 4 mai » */
export function libelleJour(iso: string): string {
  return formatJour.format(versDate(iso));
}
```

- **`versDate` et `versIso` ne sont pas exportées** : hors de ce fichier, une date est une chaîne.
- **`(jour + 6) % 7`** : `getUTCDay()` compte à partir du dimanche ; ce décalage donne 0 pour lundi, 6 pour dimanche.
- **`aujourdhui()`** est le seul endroit qui lit l'heure locale, et c'est voulu : à 0 h 30 à Paris, on est déjà demain pour l'utilisateur, alors qu'un `toISOString()` naïf répondrait encore hier.

Écrivez `dates.spec.ts` avant d'aller plus loin. Quelques cas qui méritent un test : `lundiDe('2026-05-10')` (un dimanche) vaut `'2026-05-04'` ; `lundiDe('2026-01-01')` vaut `'2025-12-29'` ; `ajouterJours('2026-03-28', 2)` vaut `'2026-03-30'` malgré le passage à l'heure d'été.

### 2. La semaine sélectionnée

```powershell
npx ng generate service commun/selection-semaine
```

```ts
import { computed, Service, signal } from '@angular/core';
import { ajouterJours, aujourdhui, lundiDe } from './dates';

/** La semaine affichée, partagée par la feuille de temps et le plan de charge. */
@Service()
export class SelectionSemaine {
  private readonly _semaine = signal(lundiDe(aujourdhui()));

  /** Le lundi de la semaine, au format de l'API : '2026-05-04'. */
  readonly semaine = this._semaine.asReadonly();

  /** Les sept jours de la semaine, du lundi au dimanche. */
  readonly jours = computed(() =>
    [0, 1, 2, 3, 4, 5, 6].map((ecart) => ajouterJours(this._semaine(), ecart)),
  );

  decaler(semaines: number): void {
    this._semaine.update((lundi) => ajouterJours(lundi, 7 * semaines));
  }

  allerA(date: string): void {
    this._semaine.set(lundiDe(date));
  }
}
```

Le service garantit une chose que ses utilisateurs n'ont plus à vérifier : `semaine()` est toujours un lundi. L'API, de son côté, refuse par un `400` une semaine qui n'en est pas un.

### 3. Le collaborateur courant, provisoirement

L'API demande pour qui on lit la feuille de temps. L'interface ne sait pas encore qui est connecté : ce sera le travail du module 19. D'ici là, `src/app/commun/collaborateur-courant.ts` :

```ts
/**
 * PROVISOIRE — l'interface n'a pas encore d'authentification.
 * Le module 19 remplace cette constante par le collaborateur de la session.
 */
export const COLLABORATEUR_COURANT_ID = 2;
```

Donnez-lui le `collaborateurId` du compte avec lequel vous avez ouvert la session : 2 pour Camille Durand, dans les données de démonstration. Avec un autre identifiant, l'API répond `404` s'il est hors de votre périmètre ; s'il y est (un manager et son équipe), elle vous laisse **lire** la feuille, mais refuse par un `403` de l'enregistrer ou de la soumettre : chacun saisit ses propres temps. Une constante nommée, dans un seul fichier, avec un commentaire qui dit qu'elle est provisoire : le jour venu, il n'y aura qu'un endroit à changer.

### 4. Les types et les calculs de la feuille

`src/app/feuilles-de-temps/feuille-de-temps.ts` commence par les miroirs de vos DTO, écrits comme au module 16 :

- `SaisieDeTemps` : `projetId`, `jour`, `duree` ;
- `FeuilleDeTemps` : `id`, `collaborateurId`, `semaine` (le lundi), `statut` (`'Brouillon' | 'Soumise' | 'Validee'`), `motifRejet` (chaîne ou `null`), `saisies` ;
- `Affectation` : `id`, `collaborateurId`, `projetId`, `dateDebut`, `dateFin`, `tauxAllocation` ;
- `JourFerie` : `date`, `libelle`.

Puis ce dont la grille a besoin :

```ts
/** Une ligne de la grille : un projet sur lequel le collaborateur peut saisir cette semaine. */
export interface LigneDeSaisie {
  projetId: number;
  libelle: string;
  /** Périodes des affectations au projet : on ne saisit pas en dehors (RG-33). */
  periodes: { du: string; au: string }[];
}

/** Les durées saisies, rangées par case : la clé est « projetId|jour ». */
export type Grille = Record<string, number>;

export const DUREES = [0, 0.25, 0.5, 0.75, 1];

export function cle(projetId: number, jour: string): string {
  return `${projetId}|${jour}`;
}

export function versGrille(saisies: SaisieDeTemps[]): Grille {
  const grille: Grille = {};
  for (const saisie of saisies) {
    grille[cle(saisie.projetId, saisie.jour)] = saisie.duree;
  }
  return grille;
}

export function versSaisies(grille: Grille): SaisieDeTemps[] {
  return Object.entries(grille)
    .filter(([, duree]) => duree > 0)
    .map(([cleDeCase, duree]) => {
      const [projetId, jour] = cleDeCase.split('|');
      return { projetId: Number(projetId), jour, duree };
    });
}

export function totalDuJour(grille: Grille, lignes: LigneDeSaisie[], jour: string): number {
  return lignes.reduce((total, ligne) => total + (grille[cle(ligne.projetId, jour)] ?? 0), 0);
}
```

L'API échange une **liste** de saisies ; la grille a besoin d'un accès **par case**. `versGrille` et `versSaisies` font l'aller et le retour — encore une conversion explicite entre ce que l'écran manipule et ce que l'API attend. Une case à zéro n'est pas envoyée : pour l'API, c'est une saisie qui n'existe pas. Écrivez aussi `totalDeLaLigne`, sur le modèle de `totalDuJour`.

### 5. Le service d'accès

`src/app/feuilles-de-temps/feuilles-de-temps-api.ts`, sur le modèle de `CollaborateursApi` :

```ts
@Service()
export class FeuillesDeTempsApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/feuilles-de-temps';

  /** La feuille d'un collaborateur pour une semaine ; l'API la crée en brouillon si besoin. */
  deLaSemaine(collaborateurId: () => number, semaine: () => string) {
    return httpResource<FeuilleDeTemps>(() => ({
      url: this.url,
      params: { collaborateurId: collaborateurId(), semaine: semaine() },
    }));
  }

  affectations(collaborateurId: () => number) {
    return httpResource<Affectation[]>(
      () => ({ url: '/api/affectations', params: { collaborateurId: collaborateurId() } }),
      { defaultValue: [] },
    );
  }

  projets() {
    return httpResource<Page<Projet>>(() => ({ url: '/api/projets', params: { taille: 100 } }));
  }

  joursFeries() {
    return httpResource<JourFerie[]>(() => '/api/jours-feries', { defaultValue: [] });
  }

  enregistrer(id: number, saisies: SaisieDeTemps[]): Observable<FeuilleDeTemps> {
    return this.http.put<FeuilleDeTemps>(`${this.url}/${id}/saisies`, { saisies });
  }

  soumettre(id: number): Observable<FeuilleDeTemps> {
    return this.http.post<FeuilleDeTemps>(`${this.url}/${id}/soumission`, null);
  }
}
```

- **`defaultValue: []`** : pour ces listes d'appoint, « pas encore chargé » et « vide » se traitent de la même façon, et `value()` n'est jamais `undefined`. Mais **`value()` lève une erreur quand la ressource est en erreur**, valeur par défaut ou non (`ResourceValueError`) : dans un `computed`, lisez toujours `hasValue() ? value() : []`. Sinon, une seule requête en échec — une liste de projets refusée par un `403`, par exemple — fait tomber tout l'écran.
- **Les projets proposés viennent des affectations** (RG-33). Mais `AffectationDto` ne porte que `projetId` : pour afficher un libellé, on lit aussi les projets (le type `Projet` de l'exercice 2 du module 16), et l'écran fera le rapprochement. La limite de 100 projets est une facilité, à discuter en séance.
- **`enregistrer` envoie `{ saisies }`**, la forme de `RemplacerSaisiesRequete`.

### 6. La grille, composant de présentation

```powershell
npx ng generate component feuilles-de-temps/grille-de-saisie
```

```ts
/** Composant de présentation : il affiche et modifie une grille, il ne connaît pas l'API. */
@Component({
  imports: [DecimalPipe],
  selector: 'app-grille-de-saisie',
  styleUrl: './grille-de-saisie.scss',
  templateUrl: './grille-de-saisie.html',
})
export class GrilleDeSaisie {
  readonly jours = input.required<string[]>();
  readonly lignes = input.required<LigneDeSaisie[]>();
  /** Jours où rien ne se saisit : week-ends, jours fériés (RG-32). */
  readonly joursFermes = input(new Set<string>());
  readonly lectureSeule = input(false);
  readonly grille = model.required<Grille>();

  protected readonly durees = DUREES;
  protected readonly libelleJour = libelleJour;

  protected readonly totauxParJour = computed(() =>
    this.jours().map((jour) => totalDuJour(this.grille(), this.lignes(), jour)),
  );
  protected readonly totauxParLigne = computed(() =>
    this.lignes().map((ligne) => totalDeLaLigne(this.grille(), ligne, this.jours())),
  );

  protected duree(ligne: LigneDeSaisie, jour: string): number {
    return this.grille()[cle(ligne.projetId, jour)] ?? 0;
  }

  protected estSaisissable(ligne: LigneDeSaisie, jour: string): boolean {
    return (
      !this.lectureSeule() &&
      !this.joursFermes().has(jour) &&
      ligne.periodes.some((periode) => periode.du <= jour && jour <= periode.au)
    );
  }

  protected saisir(ligne: LigneDeSaisie, jour: string, valeur: string): void {
    this.grille.update((grille) => ({ ...grille, [cle(ligne.projetId, jour)]: Number(valeur) }));
  }
}
```

- **`grille` est un `model()`** : une entrée que le composant a le droit de modifier, et que le parent lie par `[(grille)]`.
- **`saisir` crée un nouvel objet** (`{ ...grille, … }`) au lieu de modifier l'ancien : c'est le changement de référence qui prévient les `computed`.
- **`periode.du <= jour`** : la comparaison directe des dates ISO. Une case n'est ouverte que si le jour est couvert par au moins une affectation du projet.

Le template est un vrai tableau : une `<caption>`, une ligne d'en-têtes `<th scope="col">` (« Projet », un jour par colonne avec `libelleJour(jour)`, « Total »), puis le corps et le pied :

```html
  <tbody>
    @for (ligne of lignes(); track ligne.projetId; let i = $index) {
      <tr>
        <th scope="row">{{ ligne.libelle }}</th>
        @for (jour of jours(); track jour) {
          <td [class.ferme]="joursFermes().has(jour)">
            <select
              #choix
              [attr.aria-label]="ligne.libelle + ', ' + libelleJour(jour)"
              [disabled]="!estSaisissable(ligne, jour)"
              (change)="saisir(ligne, jour, choix.value)"
            >
              @for (d of durees; track d) {
                <option [value]="d" [selected]="d === duree(ligne, jour)">
                  {{ d === 0 ? '—' : (d | number: '1.0-2') }}
                </option>
              }
            </select>
          </td>
        }
        <td>{{ totauxParLigne()[i] | number: '1.0-2' }}</td>
      </tr>
    } @empty {
      <tr>
        <td [attr.colspan]="jours().length + 2">Aucune affectation sur cette semaine.</td>
      </tr>
    }
  </tbody>
  <tfoot>
    <tr>
      <th scope="row">Total du jour</th>
      @for (jour of jours(); track jour; let j = $index) {
        <td [class.depassement]="totauxParJour()[j] > 1">
          {{ totauxParJour()[j] | number: '1.0-2' }}
          @if (totauxParJour()[j] > 1) {
            <span class="alerte">Plus d'un jour</span>
          }
        </td>
      }
      <td></td>
    </tr>
  </tfoot>
```

- **Une liste déroulante par case** (RG-30) : l'utilisateur ne peut pas saisir 0,3. C'est un `<select>` natif, plus léger qu'un `mat-select` quand il y en a trente-cinq à l'écran.
- **`aria-label`** : sans lui, un lecteur d'écran annoncerait trente-cinq « liste déroulante » indistinctes.
- **`#choix`** donne accès à la valeur choisie ; `$event.target.value` ne compile pas sur un `<select>`.
- **Le dépassement s'écrit** (« Plus d'un jour »), en plus de la couleur.

Dans `grille-de-saisie.scss`, grisez la classe `ferme` et colorez `depassement` avec les couleurs du thème : `var(--mat-sys-surface-container-highest)`, `var(--mat-sys-error)`. Les nombres s'affichent pour l'instant à l'anglaise (`0.5`) ; le module 21 règle la langue.

### 7. L'écran de la feuille de temps

```powershell
npx ng generate component feuilles-de-temps/feuille-de-temps-hebdo
```

Faites pointer la route `/feuille-de-temps` sur `FeuilleDeTempsHebdo`, à la place du composant provisoire, puis supprimez le dossier `feuilles-de-temps/feuille-de-temps-provisoire/`. Dans la classe :

```ts
private readonly api = inject(FeuillesDeTempsApi);
protected readonly selection = inject(SelectionSemaine);

// PROVISOIRE : le module 19 lira l'identifiant dans la session.
private readonly collaborateurId = signal(COLLABORATEUR_COURANT_ID);

protected readonly feuille = this.api.deLaSemaine(this.collaborateurId, this.selection.semaine);
private readonly affectations = this.api.affectations(this.collaborateurId);
private readonly projets = this.api.projets();
private readonly joursFeries = this.api.joursFeries();

/** Copie de travail des saisies : repart de la feuille à chaque (re)chargement. */
protected readonly grille = linkedSignal(() =>
  versGrille(this.feuille.hasValue() ? this.feuille.value().saisies : []),
);

/** RG-33 : une ligne par projet auquel le collaborateur est affecté pendant la semaine. */
protected readonly lignes = computed<LigneDeSaisie[]>(() => {
  const jours = this.selection.jours();
  const lundi = jours[0];
  const dimanche = jours[6];
  const projets = this.projets.hasValue() ? this.projets.value().elements : [];

  const affectations = this.affectations.hasValue() ? this.affectations.value() : [];

  return affectations
    .filter((a) => a.dateDebut <= dimanche && a.dateFin >= lundi)
    .map((a) => ({
      projetId: a.projetId,
      libelle: projets.find((p) => p.id === a.projetId)?.libelle ?? `Projet n° ${a.projetId}`,
      periodes: [{ du: a.dateDebut, au: a.dateFin }],
    }));
});

/** RG-32 : week-ends et jours fériés. */
protected readonly joursFermes = computed(() => {
  const feries = new Set(
    (this.joursFeries.hasValue() ? this.joursFeries.value() : []).map((jour) => jour.date),
  );
  return new Set(this.selection.jours().filter((jour) => estWeekEnd(jour) || feries.has(jour)));
});

/** RG-34 et RG-35 : seule une feuille en brouillon se modifie. */
protected readonly lectureSeule = computed(
  () => !this.feuille.hasValue() || this.feuille.value().statut !== 'Brouillon',
);

/** RG-31 : les jours dont le total dépasse une journée. */
protected readonly joursEnDepassement = computed(() =>
  this.selection.jours().filter((jour) => totalDuJour(this.grille(), this.lignes(), jour) > 1),
);

protected readonly envoiEnCours = signal(false);
protected readonly peutEnvoyer = computed(
  () => !this.lectureSeule() && this.joursEnDepassement().length === 0 && !this.envoiEnCours(),
);
```

Un projet peut avoir deux affectations dans la même semaine (un changement de taux, une surcharge ponctuelle) : `lignes` donnerait alors deux lignes au même `projetId`, et le `track ligne.projetId` de la grille échouerait. Fusionnez-les en une seule ligne, qui garde les périodes de chacune : ne les réunissez pas en une seule période « du plus tôt au plus tard », qui ouvrirait les jours entre deux affectations disjointes, que l'API refuse (RG-33). Une fonction `lignesDeLaSemaine(affectations, projets, jours)` dans `feuille-de-temps.ts` se teste sans Angular.

Lisez ce code comme une feuille de calcul. Trois sources : la semaine, les réponses de l'API, la grille en cours de saisie. Tout le reste en découle, et se recalcule seul. Quand `selection.semaine` change, la feuille est redemandée, la grille repart de la nouvelle feuille, les lignes et les jours fermés suivent — sans une ligne de code pour orchestrer. Les affectations, les projets et les jours fériés, eux, ne dépendent pas de la semaine : ils ne sont pas rechargés.

Le template compose le tout :

```html
<h2>Feuille de temps</h2>

<nav aria-label="Semaine affichée">
  <button matButton type="button" (click)="selection.decaler(-1)">Semaine précédente</button>
  <strong>Semaine du {{ libelleJour(selection.semaine()) }}</strong>
  <button matButton type="button" (click)="selection.decaler(1)">Semaine suivante</button>
</nav>

<!-- chargement et erreur : comme au module 16 -->

@if (feuille.hasValue()) {
  <p>État : {{ libelleStatut() }}</p>

  @if (feuille.value().motifRejet; as motif) {
    <p role="status">Feuille rejetée : {{ motif }}</p>
  }

  <app-grille-de-saisie
    [jours]="selection.jours()"
    [lignes]="lignes()"
    [joursFermes]="joursFermes()"
    [lectureSeule]="lectureSeule()"
    [(grille)]="grille"
  />

  @if (joursEnDepassement().length > 0) {
    <p role="alert">
      Le total dépasse une journée :
      @for (jour of joursEnDepassement(); track jour) {
        {{ libelleJour(jour) }}{{ $last ? '.' : ', ' }}
      }
    </p>
  }

  @if (!lectureSeule()) {
    <button matButton="outlined" type="button" [disabled]="!peutEnvoyer()" (click)="enregistrer()">
      Enregistrer le brouillon
    </button>
    <button matButton="filled" type="button" [disabled]="!peutEnvoyer()" (click)="soumettre()">
      Soumettre
    </button>
  }
}
```

L'état de la feuille s'affiche par un `computed` `libelleStatut` qui traduit `'Validee'` en « Validée » : l'identifiant de l'API n'est pas un texte pour l'utilisateur. Écrivez-le, à partir d'une table `LIBELLES_STATUT` rangée dans `feuille-de-temps.ts`.

Ouvrez `/feuille-de-temps` : la semaine en cours, vos projets en lignes, le week-end grisé. Choisissez 0,75 et 0,5 le même jour : le total passe à 1,25, l'alerte apparaît, les boutons se ferment.

### 8. Enregistrer, puis soumettre

```ts
protected enregistrer(): void {
  if (!this.feuille.hasValue()) return;
  const id = this.feuille.value().id;
  this.envoyer(this.api.enregistrer(id, versSaisies(this.grille())));
}

/** Enregistre d'abord, soumet ensuite : la feuille soumise est celle qu'on a sous les yeux. */
protected soumettre(): void {
  if (!this.feuille.hasValue()) return;
  const id = this.feuille.value().id;
  this.envoyer(
    this.api
      .enregistrer(id, versSaisies(this.grille()))
      .pipe(concatMap(() => this.api.soumettre(id))),
  );
}

private envoyer(requete: Observable<unknown>): void {
  this.envoiEnCours.set(true);
  requete.pipe(finalize(() => this.envoiEnCours.set(false))).subscribe({
    next: () => this.feuille.reload(),
    error: () => {}, // déjà notifiée par l'intercepteur ; la saisie reste à l'écran
  });
}
```

- **`concatMap`** enchaîne deux requêtes : la seconde ne part que si la première a réussi.
- **`finalize`** s'exécute dans les deux cas, succès ou échec : le bouton ne reste jamais bloqué.
- **En cas de refus** (un `422` pour RG-32, par exemple), l'intercepteur du module 16 affiche le message de l'API, et rien n'est rechargé : l'utilisateur garde sa saisie et peut la corriger.

Soumettez : après le rechargement, l'état devient « Soumise », toutes les cases sont fermées, les boutons ont disparu (RG-35). Changez de semaine et revenez : la feuille est toujours soumise — c'est l'API qui le dit, pas l'écran. Après un rejet, la feuille revient en brouillon avec son motif ; à la soumission suivante, l'API efface le motif, et l'écran ne l'affiche plus.

**Compléter le contrat de l'API.** Comptez, dans la base, les feuilles de temps du collaborateur courant : il y en a une par semaine que vous avez affichée, même sans rien saisir. C'est le contrat du module 10 : `GET /api/feuilles-de-temps` crée la feuille absente, en brouillon et vide. Ce premier client en montre le prix :

- **La navigation laisse des données.** Vingt clics sur « Semaine précédente », vingt feuilles vides. Un manager qui consulte la semaine d'un membre de son équipe crée la feuille de celui-ci, et la trace d'audit (`CreePar`) porte le nom du manager, pas celui du collaborateur.
- **RG-05 en est faussée.** `DELETE /api/collaborateurs/{id}` archive tout collaborateur qui a une feuille, même vide : il suffit d'avoir ouvert sa feuille de temps une fois pour ne plus pouvoir être supprimé.

La décision : on garde la lecture qui crée, et RG-05 ne compte plus une feuille vide. L'autre conception — un `GET` qui renvoie une feuille « virtuelle », sans identifiant, que le premier enregistrement crée — serait plus orthodoxe, mais elle change le contrat de bout en bout : un `id` qui peut manquer, une écriture adressée par collaborateur et semaine au lieu de l'identifiant, des tests d'intégration à reprendre, et un écran qui doit savoir si la feuille existe déjà. Pour un seul défaut, c'est trop cher.

Une feuille est **vide** quand elle est en brouillon, sans saisie et sans motif de rejet : elle n'a rien enregistré que la lecture n'ait créé. Dans `Chronos.Api`, `Collaborateurs/CollaborateursEndpoints.cs`, méthode `Supprimer`, la dernière condition de `aDesDonneesLiees` devient :

```csharp
|| await db.FeuillesDeTemps.AnyAsync(
    f => f.CollaborateurId == id
        && (f.Statut != StatutFeuilleDeTemps.Brouillon || f.MotifRejet != null || f.Saisies.Any()),
    cancellationToken);
```

et la branche de la suppression physique supprime d'abord ses feuilles, toutes vides à ce stade — la clé étrangère est en `Restrict` :

```csharp
else
{
    // Les feuilles vides, créées par la seule lecture d'une semaine, partent avec le collaborateur.
    var feuillesVides = await db.FeuillesDeTemps
        .Where(f => f.CollaborateurId == id)
        .ToListAsync(cancellationToken);
    db.FeuillesDeTemps.RemoveRange(feuillesVides);
    db.Collaborateurs.Remove(collaborateur);
}
```

Les deux suppressions partent dans le même `SaveChangesAsync`, donc dans une seule transaction ; EF Core les ordonne d'après la relation, les feuilles avant le collaborateur.

Ajoutez les tests d'intégration : un collaborateur dont les feuilles n'ont été que lues (`GET /api/feuilles-de-temps`) est réellement supprimé — `204`, et sa ligne n'existe plus, même en ignorant le filtre d'archivage ; un collaborateur qui a une saisie, une feuille soumise, ou une feuille rejetée revenue en brouillon avec son motif est archivé.

Aucune migration : le modèle de données ne change pas. Relancez l'API.

### 9. Le plan de charge

`src/app/plan-de-charge/charge.ts` commence par les miroirs des DTO du module 09 :

- `Charge` : `debut`, `fin`, `capacite`, `chargePlanifiee`, `tauxDeCharge` (une fraction, 1.33 pour 133 % — ou `null` quand la capacité est nulle), `surcharge`, `intercontrat` ;
- `LigneDePlanDeCharge` : `collaborateurId`, `nom`, `prenom`, `periodes` (des `Charge`) ;
- `PlanDeChargeDto` : `du`, `au`, `pas`, `lignes`.

Puis la traduction des indicateurs en un niveau à afficher :

```ts
export type NiveauDeCharge =
  'indisponible' | 'intercontrat' | 'sous-charge' | 'normale' | 'surcharge';

/** L'interface ne recalcule rien : elle traduit les indicateurs de l'API en un niveau à afficher. */
export function niveauDeCharge(charge: Charge): NiveauDeCharge {
  if (charge.tauxDeCharge === null) return 'indisponible';
  if (charge.surcharge) return 'surcharge'; // RG-24
  if (charge.intercontrat) return 'intercontrat'; // RG-25
  return charge.tauxDeCharge < 0.8 ? 'sous-charge' : 'normale';
}

export const LIBELLES_NIVEAU: Record<NiveauDeCharge, string> = {
  indisponible: 'Indisponible',
  intercontrat: 'Intercontrat',
  'sous-charge': 'Sous-charge',
  normale: 'Charge normale',
  surcharge: 'Surcharge',
};
```

Une capacité nulle ne veut pas toujours dire « absent » : elle l'est aussi avant la date d'entrée ou après la date de sortie, et le plan de charge ne dit pas laquelle des deux. D'où « Indisponible », qui ne prétend pas en savoir plus que l'API.

Le seuil de 80 % pour la sous-charge est un choix d'affichage : le cahier des charges demande de la signaler sans la définir. Posez la question à qui de droit ; le jour où elle aura une réponse, sa place sera dans l'API.

Le composant de présentation, `plan-de-charge/indicateur-charge/indicateur-charge.ts` :

```ts
/** Composant de présentation : le taux de charge d'une période, et ce qu'il signifie. */
@Component({
  host: { '[class]': 'niveau()' },
  imports: [MatButtonModule, PercentPipe],
  selector: 'app-indicateur-charge',
  styleUrl: './indicateur-charge.scss',
  template: `
    @if (charge().tauxDeCharge !== null) {
      <span class="taux">{{ charge().tauxDeCharge | percent }}</span>
    }
    <span class="niveau">{{ libelle() }}</span>
    @if (niveau() === 'surcharge') {
      @if (confirmee()) {
        <span class="niveau">confirmée</span>
      } @else {
        <button matButton type="button" (click)="confirmer.emit()">Confirmer</button>
      }
    }
  `,
})
export class IndicateurCharge {
  readonly charge = input.required<Charge>();
  readonly confirmee = input(false);
  readonly confirmer = output<void>();

  protected readonly niveau = computed(() => niveauDeCharge(this.charge()));
  protected readonly libelle = computed(() => LIBELLES_NIVEAU[this.niveau()]);
}
```

`host: { '[class]': 'niveau()' }` pose le niveau comme classe CSS sur l'élément `<app-indicateur-charge>` lui-même. La feuille de style s'y accroche par `:host(.surcharge)`, avec les couleurs du thème (`--mat-sys-error-container` et `--mat-sys-on-error-container`, par exemple). Le libellé, lui, est toujours là.

L'écran `PlanDeCharge` lit l'API directement — une seule requête, utilisée par ce seul écran, ne justifie pas un service :

```ts
const NOMBRE_DE_SEMAINES = 4;
```

```ts
private readonly dialog = inject(MatDialog);
protected readonly selection = inject(SelectionSemaine);

/** Les lundis des semaines affichées, à partir de la semaine sélectionnée. */
protected readonly semaines = computed(() =>
  Array.from({ length: NOMBRE_DE_SEMAINES }, (_, n) =>
    ajouterJours(this.selection.semaine(), 7 * n),
  ),
);

protected readonly plan = httpResource<PlanDeChargeDto>(() => ({
  url: '/api/plan-de-charge',
  params: {
    du: this.selection.semaine(),
    au: ajouterJours(this.selection.semaine(), 7 * NOMBRE_DE_SEMAINES - 1),
    pas: 'semaine',
  },
}));
```

Son template est un tableau : une ligne par élément de `plan.value().lignes`, avec `<th scope="row">` pour le nom ; une colonne par semaine, avec `<th scope="col">` ; dans chaque case, `<app-indicateur-charge [charge]="charge" />`. Ajoutez la même navigation de semaine que sur la feuille de temps.

Ouvrez la feuille de temps, avancez de deux semaines, passez au plan de charge par le menu : il commence à la même semaine. Les deux écrans ont reçu le **même** `SelectionSemaine`.

### 10. Confirmer une surcharge

Le dialogue, `plan-de-charge/confirmation-de-surcharge/confirmation-de-surcharge.ts` :

```ts
export interface DonneesDeConfirmation {
  collaborateur: string;
  /** Ce que la période désigne pour l'utilisateur : « semaine du lun. 4 mai ». */
  periode: string;
  tauxDeCharge: number;
}

/** RG-24 : une surcharge est autorisée, mais le manager doit la confirmer. */
@Component({
  imports: [MatButtonModule, MatDialogModule, PercentPipe],
  selector: 'app-confirmation-de-surcharge',
  template: `
    <h2 mat-dialog-title>Confirmer la surcharge</h2>
    <mat-dialog-content>
      {{ donnees.collaborateur }} est planifié à {{ donnees.tauxDeCharge | percent }} de sa capacité
      ({{ donnees.periode }}). Confirmez-vous cette surcharge ?
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button matButton type="button" [mat-dialog-close]="false">Annuler</button>
      <button matButton="filled" type="button" [mat-dialog-close]="true">Confirmer</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmationDeSurcharge {
  protected readonly donnees = inject<DonneesDeConfirmation>(MAT_DIALOG_DATA);
}
```

Et dans `PlanDeCharge`, branché sur la sortie `(confirmer)` de l'indicateur :

```ts
/** Surcharges confirmées pendant cette session, repérées par « collaborateurId|debut ». */
private readonly confirmees = signal(new Set<string>());

protected confirmer(ligne: LigneDePlanDeCharge, charge: Charge): void {
  const data: DonneesDeConfirmation = {
    collaborateur: `${ligne.prenom} ${ligne.nom}`,
    periode: `semaine du ${libelleJour(charge.debut)}`,
    tauxDeCharge: charge.tauxDeCharge ?? 0,
  };

  this.dialog
    .open<ConfirmationDeSurcharge, DonneesDeConfirmation, boolean>(ConfirmationDeSurcharge, {
      data,
    })
    .afterClosed()
    .subscribe((confirme) => {
      if (confirme) {
        this.confirmees.update(
          (cles) => new Set([...cles, `${ligne.collaborateurId}|${charge.debut}`]),
        );
      }
    });
}
```

Les trois paramètres de type de `open` — le composant, ses données, son résultat — font vérifier par le compilateur ce qu'on passe au dialogue et ce qu'on en reçoit. `afterClosed()` émet `true`, `false`, ou `undefined` si l'utilisateur a fermé par `Échap`.

**Ce que cette étape ne fait pas.** La confirmation ne vit que dans un signal de l'écran : rechargez la page, elle est oubliée. Une confirmation qui compte doit être enregistrée par l'API ; l'exercice 3 du module 09 en donne le moyen, et l'exercice 4 ci-dessous le branche.

```powershell
npm test
```

```powershell
npm run build
```

## À vous

1. **Les absences (RG-32).** Lisez `/api/absences?collaborateurId=` et fermez les jours d'absence **approuvée**. Une absence qui débute l'après-midi ou finit le matin ne ferme qu'une demi-journée : que devient la liste des durées proposées ce jour-là ? Et le total que l'écran admet ce jour-là, sur l'ensemble des projets (l'API refuse par un `422` `RG-32` 0,5 + 0,5 un jour d'absence l'après-midi) ? Où placez-vous ce calcul — dans l'écran, dans la grille, dans une fonction de `feuille-de-temps.ts` ? Testez-le. La solution de référence calcule `disponible(absences, jour)` (1 moins la part absente), compare le total du jour à ce disponible au lieu de 1 dans `joursEnDepassement`, et la grille écrit « Plus d'une demi-journée » au lieu de « Plus d'un jour » quand c'est ce seuil qui est franchi.

2. **Confirmer avant de supprimer.** Le module 16 a laissé la suppression d'un collaborateur sans garde-fou. Écrivez dans `commun/` un dialogue de confirmation générique (titre, message, libellé du bouton) et utilisez-le. `ConfirmationDeSurcharge` doit-il en devenir un cas particulier ?

3. **Le manager relit.** Sur un écran `/feuilles-a-valider`, affichez la feuille soumise d'un collaborateur avec `GrilleDeSaisie` en lecture seule, et deux actions : valider, rejeter. Le rejet demande un motif : un dialogue qui contient un Signal Form, avec les deux règles de `RejeterFeuilleRequete` (obligatoire, 500 caractères au plus) — sinon le `400` arrive après la fermeture du dialogue. Qu'avez-vous dû modifier dans la grille pour la réutiliser ? La liste vient de `GET /api/feuilles-de-temps/soumises` (exercice 1 du module 10) ; ouvrez l'écran avec le compte d'une manager : pour un simple collaborateur, l'API répond `403`, et elle ne propose jamais à quelqu'un sa propre feuille.

4. **Une confirmation qui compte.** Si votre API a fait l'exercice 3 du module 09, créez une affectation depuis le plan de charge : sur un `422` portant `"regle": "RG-24"`, ouvrez le dialogue, puis renvoyez la requête avec `"surchargeConfirmee": true`. Où attrapez-vous ce `422` ? Que devient la notification de l'intercepteur ? Ce `422` ne dit ni le taux atteint ni la semaine en cause : le dialogue ne peut pas reprendre le texte de `ConfirmationDeSurcharge`.

5. **Par mois.** Ajoutez au plan de charge un choix « semaine / mois » (`pas=mois`). Qu'est-ce qui change dans les en-têtes de colonnes ? Faites partir la vue du 1er du mois de la semaine sélectionnée : à partir du lundi, la première colonne « décembre 2026 » ne couvrirait que du 14 au 31. (Un `mat-button-toggle-group` à choix unique expose ses boutons avec le rôle `radio` : c'est ce rôle qu'un test recherche.) Le choix doit-il rejoindre `SelectionSemaine`, rester dans l'écran, ou aller dans l'URL ?

6. **La semaine dans l'URL.** Sur une branche, remplacez `SelectionSemaine` par un paramètre `?semaine=2026-05-04` sur les deux routes. Comparez : rechargement de la page, lien envoyé à un collègue, bouton « Précédent » du navigateur, quantité de code. Laquelle des deux solutions gardez-vous ?

## Critères de réussite

- `/feuille-de-temps` affiche la semaine en cours, une ligne par projet affecté pendant la semaine, les week-ends et les jours fériés grisés et fermés ; une case n'est ouverte que les jours couverts par une affectation du projet.
- Les totaux par jour et par ligne suivent chaque saisie ; un total supérieur à 1 — à 0,5 un jour de demi-journée d'absence approuvée (exercice 1) — est signalé par un texte et bloque l'enregistrement.
- « Enregistrer le brouillon » envoie un `PUT …/saisies` sans case vide ; « Soumettre » envoie le `PUT` puis le `POST …/soumission` ; la feuille soumise est en lecture seule.
- Un refus de l'API (essayez une saisie un jour d'absence, avant l'exercice 1) affiche son message et laisse la saisie à l'écran.
- L'API ajustée : un collaborateur dont les feuilles n'ont été que lues est réellement supprimé par `DELETE /api/collaborateurs/{id}` ; avec une saisie, une feuille soumise ou rejetée, il est archivé (RG-05). Les tests d'intégration de l'API passent.
- `/plan-de-charge` affiche quatre semaines ; chaque case porte un taux **et** un libellé ; une surcharge propose « Confirmer », qui ouvre un dialogue utilisable au clavier.
- Changer de semaine sur un écran la change sur l'autre.
- Aucun `Date` n'apparaît hors de `commun/dates.ts` ; `COLLABORATEUR_COURANT_ID` n'est défini qu'à un endroit.
- `npm test` passe, avec vos tests de `dates.ts` et de `feuille-de-temps.ts` ; `npm run build` réussit.
- Comparez avec la solution de référence : `git diff module-17 module-18`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| Une bibliothèque de dates ajoutée « par habitude » | Une dépendance de plus, à maintenir, pour dix lignes de calcul. | `Date` en UTC, isolé dans un fichier. |
| `Temporal.PlainDate` | Disponibilité limitée dans les navigateurs, absent de Node.js 24. | Attendre ; isoler les dates pour pouvoir migrer. |
| `date.toISOString().slice(0, 10)` sur une date locale | Donne la veille à l'est de Greenwich. | Chaînes ISO de bout en bout, calculs en UTC. |
| `date.getTime() + 7 * 24 * 3600 * 1000` en heure locale | Faux les semaines de changement d'heure. | `setUTCDate(getUTCDate() + 7)`. |
| `?du=04/05/2026` | L'API lit le 5 avril, ou refuse. | `2026-05-04`, toujours. |
| Un signal `total` mis à jour dans le gestionnaire de clic | Copie d'un état dérivé : elle divergera. | `computed`. |
| Un `effect` qui recopie la feuille chargée dans la grille | Usage à éviter de `effect`. | `linkedSignal`. |
| `grille()[cle] = valeur` | Même référence : aucun `computed` n'est prévenu. | `update` avec un nouvel objet. |
| Un signal public modifiable dans un service | N'importe qui peut écrire n'importe quoi, de n'importe où. | Privé, `asReadonly()`, méthodes. |
| Un service d'état pour chaque écran, ou NgRx « parce qu'il en faut un » | De la cérémonie sans état partagé à gérer. | L'état local dans le composant ; un service quand il y a partage. |
| `taux > 1` dans le template | Duplique RG-24, sans les tests de l'API. | Afficher `surcharge`. |
| La case rouge, sans texte | Invisible pour une partie des utilisateurs. | Libellé, et la couleur en renfort. |
| `affectations.value()` dans un `computed`, protégé par `defaultValue` | `value()` lève quand la ressource est en erreur : tout l'écran tombe. | `hasValue() ? value() : []`. |
| Une ligne de grille « du premier début à la dernière fin » de deux affectations | Ouvre les jours entre deux affectations disjointes, que l'API refuse (RG-33). | Garder les périodes de chaque affectation. |
| Compter toute feuille de temps comme donnée liée (RG-05) | La simple lecture d'une semaine empêche ensuite de supprimer le collaborateur. | Ne compter que les feuilles non vides ; supprimer les vides avec lui. |
| Un `<div>` positionné à la main en guise de fenêtre modale | Ni piège à focus, ni `Échap`, ni retour du focus. | `MatDialog`. |

La liste générale est dans le [référentiel Angular](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Où avez-vous tracé la limite entre l'écran et la grille ? Citez une chose que vous avez hésité à mettre d'un côté ou de l'autre, et ce qui a tranché.
- La semaine sélectionnée vit dans un service. Qu'est-ce qui aurait justifié de la mettre dans l'URL ? Dans le composant ? Quel critère appliquez-vous ?
- L'interface vérifie RG-31 avant d'envoyer, l'API la vérifie en recevant. Est-ce une duplication ? Qu'est-ce qui distingue ce cas du `taux > 1` que le module déconseille ?
- Pour afficher le libellé d'un projet, l'écran lit les affectations **et** les cent premiers projets. Quelles autres conceptions voyez-vous, côté API comme côté interface, et que coûtent-elles ?
- La lecture d'une semaine crée sa feuille, et l'on a corrigé RG-05 plutôt que le `GET`. Qu'aurait coûté la feuille « virtuelle » à l'API, aux tests et à l'écran ? Qu'est-ce qui vous ferait changer d'avis ?
- Entre le chargement de la feuille et le clic sur « Soumettre », le manager a pu approuver une absence. Que se passe-t-il ? L'écran se comporte-t-il correctement ?
- La confirmation de surcharge de l'étape 10 est oubliée au rechargement. Qu'est-ce qu'une démonstration à un utilisateur aurait révélé ? Qui, dans l'équipe, aurait dû poser la question, et quand ?

## Pour aller plus loin

- [`Temporal` (MDN)](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Temporal) : ce que sera la manipulation des dates calendaires, et le tableau de compatibilité à surveiller.
- [Ressources](https://angular.dev/guide/signals/resource) : composer plusieurs ressources, et les états détaillés.
- [Entrées `model()`](https://angular.dev/guide/components/inputs) : la liaison dans les deux sens entre un parent et son enfant.
- [Table Material](https://material.angular.dev/components/table/overview) : en-têtes fixes et tri, si le plan de charge grossit.
- Suite dans le cours : le module 19 remplace `COLLABORATEUR_COURANT_ID` par la session et réserve le plan de charge aux managers ; le module 20 teste `SelectionSemaine` et l'indicateur de charge ; le module 21 reprend l'accessibilité de ces deux écrans et l'affichage des nombres en français.
