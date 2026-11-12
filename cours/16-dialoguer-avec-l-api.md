# Module 16 — Dialoguer avec l'API

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md).
> Le code de ce module a été **rejoué le 12 novembre 2026 sur la solution de référence** (Angular 22.2.6, CLI 22.2.7, Angular Material 22.2.7, TypeScript 6.0.3, Vitest 5.1.0, Node.js 24.22.3), à partir de l'état de la fin du module 15 : `ng build` réussit (avec l'avertissement de budget) et les 53 tests passent. Le proxy, l'ouverture de session de l'étape 2 et les écrans ont été exécutés dans Chromium contre la **véritable API .NET** du module 12, avec les quatre comptes de démonstration. L'ajout de l'indicateur `estSorti` à l'API (étape 3) a été rejoué de même : 340 tests côté API, sur PostgreSQL 18 et SQL Server 2025. Signalez au mentor tout écart constaté.

Depuis le module 13, la liste des collaborateurs sort d'un tableau écrit en dur. L'API des modules 03 à 12, elle, sait paginer, filtrer, valider et refuser. Ce module branche l'une sur l'autre : l'interface demande ses données à l'API, lui envoie ses modifications, et montre à l'utilisateur ce qui se passe pendant l'attente et quand l'API dit non.

## Objectifs

À la fin de ce module, vous saurez :

- expliquer pourquoi l'interface et l'API doivent avoir la même origine, et configurer le proxy de développement ;
- écrire les types TypeScript qui reflètent les DTO de l'API ;
- lire des données avec `httpResource`, dont l'URL dépend de signals ;
- afficher les quatre états d'une lecture : chargement, erreur, vide, données ;
- écrire avec `HttpClient` (`post`, `put`, `delete`), puis recharger ce qui a changé ;
- intercepter les réponses pour traduire les erreurs de l'API et en avertir l'utilisateur.

## Prérequis

- [Module 15](15-routage.md) : les routes existent, `withComponentInputBinding()` est activé, `ListeCollaborateurs` et `FicheCollaborateur` lisent encore `collaborateurs-en-dur.ts`.
- [Module 08](08-une-api-robuste.md) : l'API pagine (`PageDto<T>`), valide et répond par des `ProblemDetails`.
- [Module 12](12-securite-et-tracabilite.md) : l'API exige une session ; vous disposez des comptes de démonstration et de leur mot de passe.
- L'API tourne sur `http://localhost:5080` (profil `http`), avec sa base de données. Ouvrez `http://localhost:5080/api/collaborateurs` dans le navigateur : la réponse `401` prouve qu'elle écoute.

## Règles de gestion

Aucune règle nouvelle : elles sont appliquées par l'API. L'interface se contente ici de **montrer** ce que l'API décide — par exemple qu'une suppression archive le collaborateur au lieu de l'effacer (RG-05).

## Notions

### Même origine et proxy

L'**origine** d'une page, c'est son protocole, son hôte et son port. Le serveur de développement d'Angular sert l'interface sur `http://localhost:4200` ; l'API écoute sur `http://localhost:5080`. Deux ports, donc deux origines.

Or un navigateur n'autorise pas une page à lire librement les réponses d'une autre origine : il faudrait configurer CORS côté API, et les cookies de session du module 19 demanderaient des réglages supplémentaires.

Chronos évite le problème : l'interface appelle `/api/...`, **sur sa propre origine**, et le serveur de développement relaie ces requêtes vers l'API. C'est le rôle du **proxy**. En production (module 23), le serveur web fera le même relais. Le code de l'interface ne contient donc jamais l'adresse de l'API.

Documentation : [politique de même origine](https://developer.mozilla.org/fr/docs/Web/Security/Same-origin_policy), [serveur de développement et proxy](https://angular.dev/tools/cli/serve)

### Types miroirs

L'API renvoie du JSON ; TypeScript ne sait rien de sa forme. On écrit donc, pour chaque DTO, une interface qui a **exactement** les mêmes champs. Trois conventions de l'API se retrouvent dans ces types :

- une date `DateOnly` arrive sous forme de chaîne `"2026-05-04"` : le type est `string`, pas `Date` ;
- une énumération arrive sous forme de chaîne : le type est une union, `'Consultant' | 'Interne'` ;
- une valeur absente arrive `null` : le type est `string | null`.

Ces interfaces ne **vérifient** rien : si l'API change un nom de champ, TypeScript ne le voit pas, et l'écran affiche du vide. La référence, c'est le document OpenAPI de l'API (module 03).

### `httpResource` : lire

`httpResource` reçoit une fonction qui calcule une requête à partir de signals. Il envoie la requête, la **renvoie** dès qu'un de ces signals change (en annulant la précédente si elle est encore en cours), et expose le résultat sous forme de signals :

| Membre | Ce qu'il donne |
| --- | --- |
| `value()` | La réponse. **Lève une exception** si la ressource est en erreur. |
| `hasValue()` | `true` s'il y a une valeur à lire. À tester avant `value()`. |
| `error()` | L'erreur de la dernière requête, ou `undefined`. |
| `isLoading()` | `true` pendant un chargement ou un rechargement. |
| `status()` | `'idle'`, `'loading'`, `'reloading'`, `'resolved'`, `'error'` ou `'local'`. |
| `reload()` | Renvoie la même requête. |

Deux états se ressemblent et ne se comportent pas pareil, ce que l'essai confirme :

- quand **un signal change** (on demande la page 2), l'état passe à `'loading'` et `value()` redevient `undefined` ;
- quand on appelle **`reload()`**, l'état passe à `'reloading'` et `value()` **garde** l'ancienne valeur jusqu'à la réponse.

Si la fonction renvoie `undefined`, aucune requête ne part et l'état reste `'idle'`.

Documentation : [`httpResource`](https://angular.dev/guide/http/http-resource), [états d'une ressource](https://angular.dev/guide/signals/resource)

### `HttpClient` : écrire

`httpResource` sert à **lire**. Pour créer, modifier ou supprimer, la documentation demande d'utiliser `HttpClient` directement.

`http.post(...)`, `http.put(...)` et `http.delete(...)` renvoient un **Observable** : la description d'une requête, qui ne part que lorsqu'on s'y abonne. Deux façons de la déclencher :

- `.subscribe({ next, error })`, quand on réagit par une action simple ;
- `await firstValueFrom(...)`, dans une fonction `async` — vous en aurez besoin au module 17.

Une écriture réussie rend périmé ce qui est affiché : on termine par le `reload()` de la ressource concernée.

Documentation : [envoyer des requêtes](https://angular.dev/guide/http/making-requests)

### Configuration et intercepteurs

`HttpClient` est disponible sans rien configurer, et utilise l'API `fetch` du navigateur. On n'écrit `provideHttpClient(...)` que pour lui ajouter quelque chose — ici, des intercepteurs.

Un **intercepteur** est une fonction que traversent toutes les requêtes, à l'aller, et toutes les réponses, au retour. C'est l'endroit où traiter ce qui est commun à tous les appels. Les intercepteurs s'exécutent dans l'ordre où ils sont listés.

Documentation : [configuration](https://angular.dev/guide/http/setup), [intercepteurs](https://angular.dev/guide/http/interceptors)

### Les erreurs de l'API

L'API répond aux erreurs par un `ProblemDetails` ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html)) :

| Code | Sens | Ce que contient la réponse |
| --- | --- | --- |
| `400` | Entrées invalides | `errors` : les messages par champ, sous le nom C# du champ (`"Matricule"`) ; en français quand l'API les a rédigés (`Statut`, `Tri`, champs de création), en anglais sinon (`Page`, `Taille` : message par défaut d'ASP.NET Core) |
| `404` | Introuvable | — |
| `409` | Doublon (RG-01, RG-10) ou modification concurrente | `detail` |
| `422` | Règle de gestion violée | `detail`, et `regle` : `"RG-11"` |

S'y ajoute un cas sans réponse du tout : l'API est arrêtée ou le réseau coupé. `HttpClient` le signale par le code `0`.

L'intercepteur traduit tout cela en un seul type, `ErreurApi`. Pour l'affichage, le cours retient une règle simple :

- **une lecture qui échoue** s'affiche à la place des données, avec un bouton pour réessayer ;
- **une écriture qui échoue** est notifiée par un message temporaire (`MatSnackBar`), parce que l'écran d'origine reste affiché.

### Où écrire les appels HTTP ?

Le guide de style ne tranche pas ce point ; il demande seulement de garder les composants centrés sur la présentation. Le guide HTTP, lui, recommande de créer des services qui isolent l'accès aux données. Le cours retient donc **un service par fonctionnalité** : `collaborateurs-api.ts` contient toutes les URL de `/api/collaborateurs`, en lecture comme en écriture.

Une précision : `httpResource` doit être créé dans un **contexte d'injection**, et sa durée de vie est celle de qui l'a créé. Le service expose donc des méthodes qui **fabriquent** une ressource, et le composant les appelle dans l'initialiseur d'un champ. La ressource appartient au composant, l'URL appartient au service.

Documentation : [services](https://angular.dev/guide/di/creating-and-using-services), [guide de style](https://angular.dev/style-guide)

### Temporiser une recherche

Envoyer une requête à chaque frappe gaspille des appels : on attend que l'utilisateur fasse une pause. Trois moyens ont été essayés pour ce module.

| Moyen | Constat |
| --- | --- |
| `debounced(signal, 300)`, primitive de la v22 | Fonctionne bien, mais elle est **expérimentale** : sa forme peut changer. Le cours ne s'appuie pas dessus. |
| RxJS sur le signal : `toObservable` → `debounceTime` → `toSignal` | Stable. Mais en arrivant par un lien `?recherche=mar`, une première requête part avec une recherche vide, puis est annulée. |
| RxJS sur les frappes : un `Subject` → `debounceTime` | Stable, une seule requête dans tous les cas. **C'est la solution retenue.** |

Les signals décrivent un état ; une suite de frappes est un flux d'événements dans le temps, et c'est le terrain de RxJS.

Documentation : [`debounced`](https://angular.dev/guide/signals/debounced), [RxJS et signals](https://angular.dev/ecosystem/rxjs-interop)

## Pas à pas

Les commandes se lancent dans `src/web`.

### 1. Relayer `/api` vers l'API

Créez `src/proxy.conf.json` :

```json
{
  "/api/**": {
    "target": "http://localhost:5080"
  }
}
```

Déclarez-le dans `angular.json`, dans la cible `serve` du projet :

```json
"serve": {
  "builder": "@angular/build:dev-server",
  "options": {
    "proxyConfig": "src/proxy.conf.json"
  }
}
```

Laissez le reste de la cible tel quel. Le proxy n'est lu qu'au démarrage : arrêtez le serveur de développement, puis relancez-le.

```powershell
npm start
```

Ouvrez `http://localhost:4200/api/collaborateurs` : la même réponse `401` qu'en prérequis, servie cette fois par le port 4200. Si vous obtenez la page de l'application, le motif `/api/**` ou le chemin du fichier est faux.

### 2. Ouvrir une session, provisoirement

L'API refuse les inconnus, et l'écran de connexion n'arrive qu'au module 19. D'ici là, ouvrez la session à la main. Sur `http://localhost:4200`, dans la console des outils du navigateur :

```js
await fetch('/api/compte/login?useCookies=true', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'adresse-du-compte', password: 'mot-de-passe' }),
});
await (await fetch('/api/compte/moi')).json();
```

Prenez le compte de démonstration d'un **gestionnaire RH** : lui seul peut créer et modifier une fiche (RG-52). Le premier appel dépose le cookie de session. Le second affiche qui est connecté — notez son `collaborateurId`, il servira au module 18 — et dépose le cookie anti-CSRF, que `HttpClient` renverra de lui-même en en-tête dans chaque écriture. Les deux cookies sont attachés à `localhost` : l'application en profite sans rien faire.

Rechargez `http://localhost:4200/api/collaborateurs?page=1&taille=5` : du JSON. Si l'API répond de nouveau `401` un autre jour, recommencez. Le module 19 explique ce mécanisme et remplace cette manipulation par un écran.

### 3. Écrire les types miroirs

`src/app/commun/page.ts`, miroir de `PageDto<T>` :

```ts
export interface Page<T> {
  elements: T[];
  page: number;
  taille: number;
  total: number;
}
```

Reprenez ensuite `src/app/collaborateurs/collaborateur.ts`. Votre `CollaborateurDto` est né d'un exercice du module 06, a pris sa forme de référence au module 08 et a été scindé en deux au module 12 (`CollaborateurDto` réduit, `CollaborateurRhDto` complet) : ouvrez le document OpenAPI de **votre** API, et alignez le type sur lui, champ par champ. Un seul type TypeScript couvre les deux DTO, avec des champs facultatifs. Il devrait ressembler à ceci :

```ts
export type CategorieCollaborateur = 'Consultant' | 'Interne';

/** Miroir de CollaborateurDto et de CollaborateurRhDto (modules 08 et 12). */
export interface Collaborateur {
  id: number;
  matricule: string;
  nom: string;
  prenom: string;
  email: string;
  categorie: CategorieCollaborateur;
  poste: string;
  managerId: number | null;
  managerNom: string | null;
  /**
   * Informations contractuelles (RG-52), réservées au gestionnaire RH et à l'intéressé.
   * Dates au format ISO ('2026-05-04') ; quotité du contrat en cours, en fraction : 0.8 pour 80 %.
   */
  dateEntree?: string;
  dateSortie?: string | null;
  quotite?: number;
  /** Vrai si la date de sortie est passée. Toujours présent : c'est un indicateur, pas une date. */
  estSorti: boolean;
  /** Jeton de concurrence, à renvoyer tel quel dans une modification. */
  version: string;
}
```

Le DTO fait foi. Si le vôtre n'expose pas un champ, retirez-le du type : le compilateur vous montre les lignes à corriger. S'il en expose d'autres, ajoutez-les.

**Les dates et la quotité sont facultatives**, et c'est le changement qui demande de l'attention. Depuis le module 12, la liste ne les envoie **jamais**, même au gestionnaire RH, et la fiche ne les envoie qu'au gestionnaire RH et à l'intéressé. Ici le compilateur ne dit rien — les pipes `date` et `percent` acceptent `undefined` — et c'est l'écran qui se dégrade : la fiche afficherait « Date d'entrée » sans date. Dans la fiche, entourez ces affichages d'un `@if (c.dateEntree; as dateEntree) { … }` (de même pour la quotité), et ajoutez-y la date de sortie, dans un `@if (c.dateSortie; as dateSortie)`. La carte, elle, n'affiche que des éléments de la liste : retirez-en la date d'entrée et la mention « Sortie le », qu'elle n'a plus les moyens d'afficher.

Reste à distinguer les sortis quand on coche « Afficher les collaborateurs sortis ». La liste n'a plus de date, mais le DTO porte `estSorti` : un indicateur calculé par l'API avec la même règle que son filtre `statut=sorti`. Il ne révèle rien que ce filtre ne donne déjà au même utilisateur, et c'est ce qui permet de l'exposer sans rouvrir les dates. Dans la carte, affichez une mention **en texte** — « A quitté la société » — quand il est vrai ; une couleur peut la renforcer, jamais la remplacer (module 21). La fiche, pour le gestionnaire RH et l'intéressé, reste l'endroit où lire la date elle-même.

**Cet indicateur n'existe pas encore dans votre API** : le module 12 a retiré les dates de la liste sans rien mettre à la place. C'est le premier client d'une API qui révèle ce genre de manque, et c'est à vous de le combler — un aller-retour côté serveur, comme il s'en produit dans tout projet. Dans `Chronos.Api/Collaborateurs/` :

- ajoutez `bool EstSorti` à `CollaborateurDto` et à `CollaborateurRhDto`, juste avant `Version` ;
- les projections `VersDto` deviennent des méthodes qui reçoivent la date du jour — `VersDto(DateOnly aujourdhui)` — et renvoient l'expression, avec `c.DateSortie < aujourdhui` ; leurs appelants (`Lister`, `Obtenir`, `Creer`, `Modifier`) leur passent `horloge.Aujourdhui()`, jamais `DateTime.Now` ;
- ajoutez les tests d'intégration : sortie passée → `estSorti` vrai dans la liste et dans la fiche ; pas de sortie, sortie aujourd'hui ou future → faux ; la fiche réduite vue par un manager porte l'indicateur, et toujours ni dates ni quotité.

Aucune migration : le modèle de données ne change pas. Relancez l'API, puis vérifiez dans le document OpenAPI que `estSorti` apparaît dans les deux schémas.

### 4. Lire une fiche

Dans `fiche-collaborateur.ts`, remplacez la recherche dans le tableau en dur :

```ts
readonly id = input.required({ transform: numberAttribute });

protected readonly identifiantValide = computed(() => !Number.isNaN(this.id()));

protected readonly collaborateur = httpResource<Collaborateur>(() =>
  this.identifiantValide() ? `/api/collaborateurs/${this.id()}` : undefined,
);
```

`httpResource` s'importe de `@angular/common/http`. La fonction lit `this.id()` : si l'utilisateur passe d'une fiche à une autre, la requête repart d'elle-même. Pour `/collaborateurs/abc`, `numberAttribute` donne `NaN` : la fonction renvoie `undefined`, aucune requête ne part, et le message « l'identifiant doit être un nombre » du module 15 reste affiché, en tête du template.

Le template doit maintenant distinguer les états. Dans `fiche-collaborateur.html` :

```html
@if (!identifiantValide()) {
  <!-- le message du module 15 : « l'identifiant doit être un nombre » -->
} @else if (collaborateur.isLoading()) {
  <mat-progress-bar mode="indeterminate" aria-label="Chargement de la fiche" />
} @else if (collaborateur.error()) {
  <p role="alert">
    La fiche n'a pas pu être chargée.
    <button matButton type="button" (click)="collaborateur.reload()">Réessayer</button>
  </p>
} @else if (collaborateur.hasValue()) {
  @let c = collaborateur.value();
  <!-- la carte du module 14 : elle lit c -->
}
```

Ajoutez `MatProgressBarModule` aux `imports` du composant. L'ordre des branches compte : `hasValue()` vient en dernier, et c'est lui qui autorise la lecture de `value()`.

Ouvrez une fiche, puis l'onglet *Réseau* des outils du navigateur : une requête vers `/api/collaborateurs/1`. Arrêtez l'API et rechargez la page : le message d'erreur et son bouton apparaissent.

### 5. Regrouper les appels dans un service

```powershell
npx ng generate service collaborateurs/collaborateurs-api
```

La commande crée `collaborateurs-api.ts`, avec une classe décorée `@Service()`. Complétez-la :

```ts
import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { Page } from '../commun/page';
import { Collaborateur } from './collaborateur';

/** Les tris acceptés par l'API ; `-nom` est l'ordre décroissant. */
export type TriCollaborateurs = 'nom' | '-nom' | 'matricule';

export interface CriteresCollaborateurs {
  page: number;
  taille: number;
  recherche: string;
  avecSortis: boolean;
  tri: TriCollaborateurs;
}

@Service()
export class CollaborateursApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/collaborateurs';

  /** À appeler dans un contexte d'injection : l'initialiseur d'un champ de composant. */
  liste(criteres: () => CriteresCollaborateurs) {
    return httpResource<Page<Collaborateur>>(() => {
      const { page, taille, recherche, avecSortis, tri } = criteres();
      return {
        url: this.url,
        // Sans `statut`, l'API renvoie les actifs et les sortis.
        params: { page, taille, recherche, tri, ...(avecSortis ? {} : { statut: 'actif' }) },
      };
    });
  }

  /** Tant que `id` renvoie `undefined`, aucune requête ne part. */
  parId(id: () => number | undefined) {
    return httpResource<Collaborateur>(() => {
      const valeur = id();
      return valeur === undefined ? undefined : `${this.url}/${valeur}`;
    });
  }

  supprimer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
```

- **`liste` et `parId` reçoivent des fonctions**, pas des valeurs : c'est en les appelant *dans* la fonction de `httpResource` que la ressource devient dépendante des signals du composant.
- **`params`** : au lieu de construire l'URL à la main, on donne un objet ; Angular encode les valeurs. Le filtre `statut` n'est envoyé que s'il sert.
- **Le composant ne connaît plus l'API** : il parle de « collaborateurs sortis », le service traduit en `statut=actif`. L'API accepte `statut=actif` ou `statut=sorti` ; sans le paramètre, elle renvoie tout le monde.
- **`tri`** : l'API ne trie que par `nom`, `-nom` et `matricule` (sinon `400`). Le tri par date d'entrée du module 14 ne peut pas passer côté serveur — la date d'entrée est une information contractuelle que la liste ne porte pas : le bouton de tri bascule désormais entre nom et matricule.

La fiche devient :

```ts
private readonly api = inject(CollaborateursApi);

readonly id = input.required({ transform: numberAttribute });

protected readonly identifiantValide = computed(() => !Number.isNaN(this.id()));

protected readonly collaborateur = this.api.parId(() =>
  this.identifiantValide() ? this.id() : undefined,
);
```

`api` et `identifiantValide` doivent être déclarés **avant** `collaborateur` : les champs d'une classe s'initialisent dans l'ordre où ils sont écrits.

### 6. Brancher la liste, avec pagination

Dans `liste-collaborateurs.ts`, le signal en dur, le filtre calculé et la ligne de répartition consultants / internes disparaissent. C'est l'API qui filtre et qui trie ; et une répartition calculée sur les vingt cartes d'une page ne dirait rien de l'ensemble. `recherche` et `afficherSortis` restent ceux du module 15, lus dans l'adresse (`?recherche=…`, `?statut=tous`) :

```ts
private readonly api = inject(CollaborateursApi);

protected readonly tri = signal<TriCollaborateurs>('nom');
protected readonly taille = signal(20);

/** Revient à la première page dès que la recherche, le filtre ou le tri change. */
protected readonly page = linkedSignal({
  source: () => ({ recherche: this.recherche(), avecSortis: this.afficherSortis(), tri: this.tri() }),
  computation: () => 1,
});

protected readonly collaborateurs = this.api.liste(() => ({
  page: this.page(),
  taille: this.taille(),
  recherche: this.recherche(),
  avecSortis: this.afficherSortis(),
  tri: this.tri(),
}));

/** Garde le dernier total connu pendant qu'une autre page se charge. */
protected readonly total = linkedSignal<number | undefined, number>({
  source: () => (this.collaborateurs.hasValue() ? this.collaborateurs.value().total : undefined),
  computation: (total, precedent) => total ?? precedent?.value ?? 0,
});

protected changerDePage(evenement: PageEvent): void {
  this.taille.set(evenement.pageSize);
  this.page.set(evenement.pageIndex + 1);
}
```

- **`page` est un `linkedSignal`** : un signal modifiable (`page.set(3)`) qui se réinitialise quand sa source change. Rester en page 3 d'une recherche qui n'a plus qu'une page afficherait une liste vide.
- **`total` en est un aussi**, pour une autre raison : pendant le chargement de la page 2, `value()` est `undefined`. Sans ce signal, le paginateur afficherait « 0 » un instant, puis se recalerait.
- **`basculerTri`** alterne maintenant `'nom'` et `'matricule'`, et le libellé du bouton devient « Trier par matricule ».

Dans `liste-collaborateurs.html`, sous la barre d'outils du module 14 :

```html
@if (collaborateurs.isLoading()) {
  <mat-progress-bar mode="indeterminate" aria-label="Chargement des collaborateurs" />
}

@if (collaborateurs.error()) {
  <p role="alert">
    La liste n'a pas pu être chargée.
    <button matButton type="button" (click)="collaborateurs.reload()">Réessayer</button>
  </p>
} @else if (collaborateurs.hasValue()) {
  @let elements = collaborateurs.value().elements;

  @if (total() > 0) {
    <app-compteur-de-resultats [nombre]="elements.length" [total]="total()" />
  }

  <div class="cartes">
    @for (c of elements; track c.id) {
      <app-carte-collaborateur [collaborateur]="c" />
    } @empty {
      @if (recherche()) {
        <p>Aucun collaborateur ne correspond à « {{ recherche() }} ».</p>
      } @else {
        <p>Aucun collaborateur à afficher.</p>
      }
    }
  </div>
}

<mat-paginator
  aria-label="Pagination des collaborateurs"
  [length]="total()"
  [pageIndex]="page() - 1"
  [pageSize]="taille()"
  [pageSizeOptions]="[10, 20, 50]"
  (page)="changerDePage($event)"
/>
```

Ajoutez `MatPaginatorModule` et `MatProgressBarModule` aux `imports` ; `PageEvent` vient lui aussi de `@angular/material/paginator`. Le paginateur compte les pages à partir de 0, l'API à partir de 1 : d'où le `- 1` et le `+ 1`. Il reste en dehors du `@if` pour ne pas disparaître à chaque changement de page. Ses libellés sont en anglais ; le module 21 les traduit.

Le compteur du module 14 change de sens : « 20 collaborateurs sur 27 » compte les cartes de la page et le total de la sélection renvoyé par l'API. Il est masqué quand ce total est nul, plutôt que d'afficher « Aucun collaborateur sur 0 ». Un administrateur, qui n'a pas de fiche et ne voit aucun collaborateur (module 12), obtient ainsi « Aucun collaborateur à afficher ».

Si vous aviez aussi placé `page` dans l'adresse (exercice 5 du module 15), gardez-la : votre entrée remplace le signal local, et le retour à la première page se fait dans la navigation.

Documentation : [`linkedSignal`](https://angular.dev/guide/signals/linked-signal), [paginateur](https://material.angular.dev/components/paginator/overview)

### 7. Temporiser la recherche

Depuis le module 15, `rechercher(texte)` écrit la recherche dans l'URL, et l'URL alimente l'entrée `recherche`. Il suffit donc de retarder cette écriture :

```ts
/** Les frappes de l'utilisateur : un flux d'événements, temporisé avant d'agir. */
private readonly saisies = new Subject<string>();

constructor() {
  this.saisies
    .pipe(debounceTime(300), takeUntilDestroyed())
    .subscribe((texte) => this.afficherRecherche(texte));
}

protected rechercher(texte: string): void {
  this.saisies.next(texte);
}
```

`afficherRecherche` est votre ancienne méthode `rechercher`, renommée et passée en `private`. `Subject` et `debounceTime` viennent de `rxjs`, `takeUntilDestroyed` de `@angular/core/rxjs-interop` : il arrête l'abonnement quand le composant est détruit.

Tapez vite « mar » en regardant l'onglet *Réseau* : une seule requête, 300 ms après la dernière frappe.

### 8. Traduire et notifier les erreurs

`src/app/commun/erreur-api.ts` :

```ts
import { HttpErrorResponse } from '@angular/common/http';

/** Une erreur de l'API, traduite depuis sa réponse ProblemDetails. */
export class ErreurApi extends Error {
  constructor(
    /** Code HTTP ; 0 quand l'API n'a pas répondu. */
    readonly statut: number,
    message: string,
    /** Code de la règle de gestion violée (réponse 422), par exemple 'RG-11'. */
    readonly regle: string | null = null,
    /** Messages par champ (réponse 400). Les clés sont les noms de champs renvoyés par l'API. */
    readonly champs: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = 'ErreurApi';
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  regle?: string;
  errors?: Record<string, string[]>;
}

/** Quand l'API n'écrit pas de `detail`, son `title` est en anglais : on le remplace. */
const LIBELLES: Record<number, string> = {
  400: 'Les données envoyées sont invalides.',
  404: "La ressource demandée n'existe pas, ou vous n'y avez pas accès.",
  500: 'Une erreur est survenue sur le serveur.',
};

export function versErreurApi(erreur: unknown): ErreurApi {
  if (!(erreur instanceof HttpErrorResponse)) {
    return new ErreurApi(0, 'Une erreur inattendue est survenue.');
  }
  if (erreur.status === 0) {
    return new ErreurApi(0, "L'API ne répond pas. Vérifiez votre connexion, puis réessayez.");
  }
  const probleme = (erreur.error ?? {}) as ProblemDetails;
  const message =
    probleme.detail ??
    LIBELLES[erreur.status] ??
    `L'API a répondu par une erreur ${erreur.status}.`;
  return new ErreurApi(erreur.status, message, probleme.regle ?? null, probleme.errors ?? {});
}
```

- **`ErreurApi` étend `Error`** : `httpResource` expose alors l'objet tel quel dans `error()`, et un composant peut écrire `erreur instanceof ErreurApi && erreur.statut === 404`.
- **Le message vient de `detail`**, que Chronos rédige en français pour les `409` et les `422`. Le `title`, lui, est souvent écrit par ASP.NET Core, en anglais (« Not Found », « One or more validation errors occurred. ») : on ne le montre pas, on le remplace par un libellé de l'interface.
- **`401` et `403` n'ont pas encore de libellé** : une session expirée ou une suppression tentée sans le rôle de gestionnaire RH donne « L'API a répondu par une erreur 403. ». Le module 19 leur donne des mots justes.

```powershell
npx ng generate interceptor commun/erreurs
```

Dans `erreurs-interceptor.ts` :

```ts
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { versErreurApi } from './erreur-api';

export const erreursInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((reponse: unknown) => {
      const erreur = versErreurApi(reponse);
      if (req.method !== 'GET') {
        snackBar.open(erreur.message, 'Fermer', { duration: 8000 });
      }
      return throwError(() => erreur);
    }),
  );
};
```

`catchError` intercepte l'erreur, `throwError` la **relance** une fois traduite : l'appelant doit toujours savoir que sa requête a échoué. Enregistrez l'intercepteur dans `app.config.ts` :

```ts
provideHttpClient(withInterceptors([erreursInterceptor])),
```

Dans la fiche, distinguez maintenant le `404` des autres erreurs : « Aucun collaborateur ne porte cet identifiant » d'un côté, « La fiche n'a pas pu être chargée » et son bouton de l'autre.

Documentation : [notification temporaire](https://material.angular.dev/components/snack-bar/overview)

### 9. Supprimer, puis recharger

Ajoutez à `CarteCollaborateur` une sortie `readonly supprimer = output<void>();` et un bouton « Supprimer » qui l'émet. Dans la liste :

```html
<app-carte-collaborateur [collaborateur]="c" (supprimer)="supprimer(c)" />
```

```ts
protected supprimer(collaborateur: Collaborateur): void {
  this.api.supprimer(collaborateur.id).subscribe({
    next: () => this.collaborateurs.reload(),
    error: () => {}, // l'intercepteur a déjà notifié l'utilisateur
  });
}
```

Sans `.subscribe(...)`, rien ne part. Sans la fonction `error`, l'erreur relancée par l'intercepteur finirait, non traitée, dans la console. Pendant le rechargement, la liste reste affichée : l'état est `'reloading'`. L'en-tête `X-XSRF-TOKEN` exigé par l'API pour toute écriture (module 12) est posé par `HttpClient`, à partir du cookie déposé par `/api/compte/moi` : vérifiez-le dans l'onglet *Réseau*. Avec un compte qui n'est pas gestionnaire RH, la suppression est refusée (`403`) et notifiée.

Cette suppression ne demande pas de confirmation ; vous l'ajouterez au module 18, avec les dialogues.

### 10. Mettre à jour les tests, retirer les données en dur

Les tests des modules 14 et 15 échouent : les composants attendent maintenant une réponse HTTP. Fournissez un faux serveur, et répondez à sa place. Pour la fiche :

```ts
await TestBed.configureTestingModule({
  imports: [FicheCollaborateur],
  providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
}).compileComponents();

const http = TestBed.inject(HttpTestingController);
fixture = TestBed.createComponent(FicheCollaborateur);
fixture.componentRef.setInput('id', '1');
TestBed.tick();

http.expectOne('/api/collaborateurs/1').flush(camille);
await fixture.whenStable();
```

`provideHttpClientTesting` et `HttpTestingController` viennent de `@angular/common/http/testing`. L'ordre est celui de la documentation de `httpResource` : `TestBed.tick()` fait partir la requête, le test y répond, puis attend la stabilité. Ne commencez pas par `await fixture.whenStable()` : tant qu'une requête attend sa réponse, l'application n'est pas stable, et la promesse ne se résout jamais (constaté).

Il en va de même après une action. Un clic qui change un signal (« Réessayer », le tri, le paginateur) : `TestBed.tick()`, puis `expectOne`. Un clic qui passe par le routeur (le filtre des sortis, qui écrit dans l'adresse) ou une recherche temporisée : la requête part plus tard, `await vi.waitFor(() => http.expectOne(…))` la laisse apparaître. Le module 20 revient en détail sur ces tests. Supprimez enfin `collaborateurs-en-dur.ts`.

```powershell
npm test
```

```powershell
npm run build
```

Les tests doivent passer (`q` quitte le mode surveillance) et la compilation réussir, avec l'avertissement de budget du module 14, plus marqué : le chargement initial passe d'environ 513 kB à 617 kB (voir « Pour la séance »).

## À vous

1. **Provoquer chaque état.** Affichez provisoirement `{{ collaborateurs.status() }}` dans la liste. Faites apparaître, l'un après l'autre, `loading`, `reloading`, `resolved` et `error`. Dans quel état la liste reste-t-elle visible pendant l'attente ? Retirez l'affichage ensuite.

2. **Les projets.** Écrivez le type `Projet` d'après votre `ProjetDto`, le service `ProjetsApi` et l'écran `ListeProjets` sur la route `/projets` : pagination, recherche, filtre par statut. Qu'avez-vous pu reprendre tel quel du travail sur les collaborateurs ? Qu'est-ce que cela suggère ?

3. **Valeur par défaut.** `httpResource` accepte une option `defaultValue`. Réécrivez la liste des projets avec une page vide par défaut, sans `hasValue()`. Qu'y gagne le template ? Que ne sait-il plus distinguer ? Provoquez ensuite une erreur (arrêtez l'API) : `value()` lève une exception quand la ressource est en erreur, même avec une valeur par défaut — constaté dans le code d'Angular 22.2. Une lecture hors du template s'écrit donc `hasValue() ? value() : …` ; le module 18 s'en servira.

4. **Se méfier de la réponse.** L'option `parse` reçoit la réponse brute et renvoie la valeur typée. Écrivez une fonction qui vérifie que la réponse de `/api/collaborateurs` a bien un tableau `elements` et un nombre `total`, et lève une erreur sinon. Renommez un champ côté API : que voit l'utilisateur, avec et sans `parse` ?

5. **Mesurer.** Écrivez un second intercepteur qui écrit dans la console la durée de chaque requête. Placez-le avant, puis après `erreursInterceptor` dans la liste : qu'est-ce qui change pour une requête en erreur ?

6. **La primitive expérimentale.** Sur une branche, remplacez le `Subject` par `debounced()`. Comparez le nombre de lignes et le comportement dans l'onglet *Réseau*, y compris en arrivant par un lien `?recherche=…`. Fusionneriez-vous cette branche ?

## Critères de réussite

- Session ouverte, `http://localhost:4200/api/collaborateurs` renvoie le JSON de l'API ; l'adresse `localhost:5080` n'apparaît nulle part dans `src/app/`.
- La liste et la fiche affichent les données de la base ; `collaborateurs-en-dur.ts` n'existe plus.
- Changer de page, de taille de page ou de recherche envoie une requête, et une seule ; une recherche ramène à la première page.
- API arrêtée : la liste et la fiche affichent un message et un bouton « Réessayer », qui fonctionne une fois l'API relancée.
- Une suppression refusée par l'API affiche une notification ; une suppression réussie recharge la liste.
- `npm run build` réussit (l'avertissement de budget du module 14 est toujours attendu, avec un dépassement plus grand) et `npm test` passe.
- Avec un compte de manager, la liste ne montre que son équipe et la fiche d'un équipier n'a ni dates ni quotité ; avec un compte de collaborateur, la seule fiche visible est la sienne, les autres répondent « Aucun collaborateur ne porte l'identifiant » ; aucune erreur dans la console hormis les réponses `4xx` attendues.
- Comparez avec la solution de référence : `git diff module-15 module-16`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `http.get('http://localhost:5080/api/...')` | Autre origine : CORS, cookies refusés, et une adresse à changer en production. | URL relative `/api/...` et proxy. |
| Activer CORS « pour que ça marche » | Contourne le symptôme, complique l'authentification du module 19. | Même origine. |
| `provideHttpClient(withFetch())` | `withFetch()` est déprécié : `fetch` est le comportement par défaut. | `provideHttpClient(withInterceptors([...]))`, ou rien. |
| `HttpClientModule`, intercepteur écrit comme une classe | Déprécié pour le premier, déconseillé pour le second. | `provideHttpClient`, `HttpInterceptorFn`. |
| `subscribe` dans `ngOnInit` qui recopie la réponse dans un signal | Ni annulation, ni état de chargement, ni rechargement. | `httpResource`. |
| `httpResource` pour un `POST` ou un `DELETE` | Une ressource se relance quand ses signals changent : une écriture ne doit jamais partir « toute seule ». | `HttpClient`. |
| `http.delete(...)` sans `subscribe` | L'Observable est froid : la requête ne part pas. | `.subscribe(...)` ou `firstValueFrom`. |
| Lire `value()` sans tester `hasValue()` | Lève une exception quand la ressource est en erreur. | `@if (x.hasValue())`. |
| Créer une ressource dans une méthode ou dans `ngOnInit` | Hors contexte d'injection : erreur à l'exécution. | Dans l'initialiseur d'un champ. |
| Un `effect` qui remet la page à 1 | Recopier un signal dans un autre est l'usage à éviter de `effect`. | `linkedSignal`. |
| Type `Date` pour `dateEntree` | Le JSON contient une chaîne ; le type mentirait. | `string`, convertie à l'affichage. |
| Un intercepteur qui avale l'erreur | L'appelant croit que tout s'est bien passé. | `throwError` après traitement. |

La liste générale est dans le [référentiel Angular](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Le proxy n'existe qu'en développement. Qui joue son rôle en production ? Que se passerait-il si personne ne le jouait ?
- Vos types TypeScript et vos DTO C# décrivent la même chose à deux endroits. Comment un écart peut-il apparaître, et qui s'en aperçoit en premier ? Quelles parades connaissez-vous ?
- Pourquoi le cours notifie-t-il les erreurs d'écriture et pas celles de lecture ? Défendez la règle inverse.
- Comparez le compte rendu de `npm run build` à celui du module 15 : le chargement initial a grossi. Qu'est-ce qui, dans ce module, y est entré, et pourquoi ne peut-il pas être chargé à la demande ?
- Une API expérimentale fait mieux qu'une API stable, en moins de lignes. Selon quels critères décidez-vous de l'adopter ou non dans un projet d'entreprise ?
- Le service `CollaborateursApi` ne contient aucune logique. Est-ce une raison de s'en passer ?

## Pour aller plus loin

- [Tester les requêtes](https://angular.dev/guide/http/testing) : `provideHttpClientTesting` en détail, avant le module 20.
- [`httpResource`, référence de l'API](https://angular.dev/api/common/http/httpResource) : toutes les signatures ; le guide cité plus haut présente les variantes `text`, `blob` et `arrayBuffer`, pour lire autre chose que du JSON.
- [Ressources](https://angular.dev/guide/signals/resource) : `resource()`, la forme générale dont `httpResource` est un cas particulier.
- [Table Material](https://material.angular.dev/components/table/overview) : une autre présentation pour une liste paginée, avec tri par colonne.
- Suite dans le cours : le module 17 envoie des formulaires à l'API et rattache ses erreurs aux champs ; le module 19 ajoute l'intercepteur d'authentification.
