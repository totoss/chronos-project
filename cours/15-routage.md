# Module 15 — Routage

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md).
> Les commandes et le code de ce module ont été **exécutés le 12 novembre 2026** dans un conteneur Linux `node:24` : Node.js 24.22.3, Angular CLI 22.2.7, Angular 22.2.6, Angular Material 22.2.7, TypeScript 6.0.3, Vitest 5.1.0. `ng build` et `ng test` y réussissent ; la redirection, les liens, les titres de page, le paramètre de requête et la page introuvable ont été contrôlés dans un navigateur et par un test de navigation. **Non exécuté** : les commandes sous Windows / PowerShell, et les exercices. Signalez au mentor tout écart constaté.

À la fin du module 14, Chronos affiche tout sur une seule page, à une seule adresse. Un gestionnaire RH ne peut ni envoyer à un collègue le lien vers la fiche de Camille Martin, ni revenir en arrière avec le bouton du navigateur, ni garder sa recherche après un rechargement. Et l'application aura bientôt une demi-douzaine d'écrans. Ce module donne **une adresse à chaque écran**, ne charge le code d'un écran que lorsqu'on le visite, et installe la barre de navigation.

## Objectifs

À la fin de ce module, vous saurez :

- déclarer les routes de l'application et celles d'une fonctionnalité ;
- charger un écran à la demande avec `loadComponent` et `loadChildren` ;
- construire la navigation : `<router-outlet />`, `routerLink`, `routerLinkActive`, et l'attribut `aria-current` ;
- recevoir un paramètre d'URL (`:id`) et un paramètre de requête (`?recherche=`) par une entrée `input()` ;
- rediriger, afficher une page « introuvable », donner un titre à chaque page ;
- naviguer depuis le code avec `Router.navigate` ;
- expliquer pourquoi l'ordre des routes compte.

## Prérequis

- [Module 14](14-composer-des-composants.md) : les composants `ListeCollaborateurs`, `CarteCollaborateur`, `FicheCollaborateur` et `BarreDeRecherche` existent, Angular Material est installé.
- Savoir lire une URL : chemin, paramètres de requête après `?`.

## Règles de gestion

Aucune. Les routes de ce module ne sont pas encore protégées : les droits d'accès (RG-50 à RG-52) s'appliqueront au [module 19](19-authentification-cote-interface.md), avec des gardes.

## Notions

### Une adresse par écran

Chronos reste une **page unique** : le serveur n'envoie qu'un `index.html`. C'est le **routeur** d'Angular qui lit l'adresse, choisit le composant à afficher, et met l'adresse à jour quand l'utilisateur navigue — sans recharger la page.

L'adresse devient ainsi un **état de l'application**, visible et partageable. La question à se poser pour chaque état : *doit-il survivre à un rechargement, à un favori, à un lien envoyé par courriel ?* Si oui, sa place est dans l'URL. Le collaborateur affiché et le texte recherché sont dans ce cas ; l'ouverture d'un menu ne l'est pas.

Documentation : [vue d'ensemble du routage](https://angular.dev/guide/routing)

### Définir des routes

Une route associe un **chemin** à un **composant** :

| Forme | Exemple | Correspond à |
| --- | --- | --- |
| Chemin fixe | `path: 'collaborateurs'` | `/collaborateurs` |
| Paramètre | `path: ':id'` | n'importe quel segment, dont la valeur s'appelle `id` |
| Joker | `path: '**'` | toute adresse qu'aucune route précédente n'a reconnue |
| Redirection | `redirectTo: 'collaborateurs'` | renvoie vers une autre route |

Le routeur parcourt les routes **dans l'ordre et s'arrête à la première qui correspond**. Une route précise se place donc avant une route générale : `nouveau` avant `:id`, et `**` en dernier.

Une route peut aussi porter un `title`, que le routeur donne à l'onglet du navigateur.

Documentation : [définir des routes](https://angular.dev/guide/routing/define-routes), [redirections](https://angular.dev/guide/routing/redirecting-routes)

### Chargement à la demande

Avec `component: FicheCollaborateur`, le code de la fiche est livré dès l'ouverture de l'application. Avec `loadComponent`, il part dans un fichier séparé, téléchargé à la première visite de la route :

```ts
loadComponent: () =>
  import('./fiche-collaborateur/fiche-collaborateur').then((m) => m.FicheCollaborateur),
```

`loadChildren` fait de même pour **un fichier de routes entier** : toute une fonctionnalité, écrans compris, quitte le chargement initial. Chronos donne à chaque fonctionnalité son fichier de routes.

> `import('…')` renvoie le module ; `.then((m) => m.X)` y prend la classe exportée. Si le fichier utilise un export par défaut (`export default class …`), le `.then` devient inutile. Le cours garde les exports nommés que génère la CLI.

Documentation : [stratégies de chargement](https://angular.dev/guide/routing/loading-strategies)

### L'emplacement et les liens

- `<router-outlet />` marque l'endroit du template où le routeur affiche le composant de la route active. Tout ce qui l'entoure — barre de titre, menu — reste en place d'un écran à l'autre.
- `routerLink` s'emploie sur un `<a>` à la place de `href`. Le lien reste un vrai lien (clic droit, ouverture dans un nouvel onglet), mais un clic simple change d'écran sans recharger la page. `routerLink="/collaborateurs"` pour un chemin fixe, `[routerLink]="['/collaborateurs', c.id]"` quand un segment est calculé.
- `routerLinkActive` signale que la route d'un lien est active ; `ariaCurrentWhenActive="page"` pose alors l'attribut `aria-current="page"`, qui l'annonce aux lecteurs d'écran.

Un chemin qui commence par `/` part de la racine ; sans `/`, il est relatif à la route courante.

Documentation : [afficher les routes](https://angular.dev/guide/routing/show-routes-with-outlets), [naviguer vers une route](https://angular.dev/guide/routing/navigate-to-routes), [`RouterLinkActive`](https://angular.dev/api/router/RouterLinkActive)

### Les paramètres deviennent des entrées

Avec l'option `withComponentInputBinding()`, le routeur renseigne les **entrées** du composant affiché à partir de l'URL : une entrée `id` reçoit le paramètre `:id`, une entrée `recherche` reçoit `?recherche=…`. Le composant n'a rien à apprendre du routeur : ce sont les `input()` du module 14.

Deux précautions :

- **une URL ne contient que du texte.** Le routeur fournit `'3'`, pas `3`. Déclarer `input.required<number>()` serait mentir au compilateur : à l'exécution, `'3' === 3` est faux et rien ne serait trouvé. On convertit avec `transform: numberAttribute` ;
- **un paramètre absent vaut `undefined`**, même si l'entrée a une valeur par défaut : le routeur l'écrase pour ne pas laisser traîner l'ancienne valeur. On rétablit la valeur par défaut dans `transform`.

Documentation : [lier les informations de route aux entrées](https://angular.dev/guide/routing/common-router-tasks#getting-route-information), [`withComponentInputBinding`](https://angular.dev/api/router/withComponentInputBinding)

### Naviguer depuis le code

Un lien suffit quand l'utilisateur clique pour aller quelque part. Quand la navigation est la **conséquence d'autre chose** — une saisie, un enregistrement réussi — on appelle le routeur : `router.navigate(['/collaborateurs', 3])`. Le premier argument est le même tableau que celui de `routerLink` ; le second règle les paramètres de requête et l'historique.

Documentation : [`Router.navigate`](https://angular.dev/api/router/Router)

### Gardes

Une **garde** est une fonction que le routeur interroge avant d'activer une route, et qui peut refuser ou rediriger : c'est elle qui renverra vers `/connexion` un utilisateur non authentifié. Ce module n'en écrit pas ; le module 19 leur est consacré. Retenez seulement qu'une garde améliore le parcours de l'utilisateur et ne protège rien : la protection réelle est dans l'API.

Documentation : [gardes de route](https://angular.dev/guide/routing/route-guards)

## Pas à pas

### 1. Activer la liaison des paramètres

`src/app/app.config.ts` fournit déjà le routeur. Ajoutez-lui l'option :

```ts
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding())
  ]
};
```

`provideRouter` reçoit le tableau de routes, puis des options sous forme de fonctions `with…()`.

### 2. Partager les données en dur

La fiche va devenir un écran à part : elle ne recevra plus son collaborateur de la liste, elle devra le retrouver par son identifiant. En attendant l'API, déplacez le tableau en dur dans un fichier que les deux écrans liront, `src/app/collaborateurs/collaborateurs-en-dur.ts` :

```ts
import { Collaborateur } from './collaborateur';

export const COLLABORATEURS_EN_DUR: Collaborateur[] = [
  { id: 1, matricule: 'C0001', nom: 'Martin', prenom: 'Camille', /* … */ quotite: 0.8 },
  // … les quatre autres, repris de liste-collaborateurs.ts
];
```

Dans `ListeCollaborateurs`, le signal devient :

```ts
  protected readonly collaborateurs = signal(COLLABORATEURS_EN_DUR);
```

Ce fichier disparaîtra au [module 16](16-dialoguer-avec-l-api.md).

### 3. La fiche devient une page

Au module 14, `FicheCollaborateur` recevait un collaborateur de son parent et laissait celui-ci projeter un bouton « Fermer ». Affichée par le routeur, elle **n'a plus de parent dans un template** : personne pour lui passer un objet, lui projeter du contenu ou écouter ses sorties. Tout ce qu'elle sait vient de l'URL.

`fiche-collaborateur.ts` :

```ts
import { DatePipe, PercentPipe } from '@angular/common';
import { Component, computed, input, numberAttribute } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { RouterLink } from '@angular/router';
import { COLLABORATEURS_EN_DUR } from '../collaborateurs-en-dur';

@Component({
  imports: [
    DatePipe,
    PercentPipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatListModule,
    RouterLink,
  ],
  selector: 'app-fiche-collaborateur',
  styleUrl: './fiche-collaborateur.scss',
  templateUrl: './fiche-collaborateur.html',
})
export class FicheCollaborateur {
  readonly id = input.required({ transform: numberAttribute });

  protected readonly collaborateur = computed(() =>
    COLLABORATEURS_EN_DUR.find((c) => c.id === this.id()),
  );
}
```

L'entrée s'appelle `id`, comme le paramètre `:id` de la route à venir : c'est ce nom qui fait le lien. `numberAttribute` convertit le texte de l'URL en nombre.

Dans `fiche-collaborateur.html`, remplacez la première ligne (`@let c = …`) par un `@if`, et l'emplacement `<ng-content />` par un lien de retour :

```html
@if (collaborateur(); as c) {
  <mat-card>
    <!-- en-tête et liste inchangés -->

    <mat-card-actions align="end">
      <a matButton routerLink="/collaborateurs">Retour à la liste</a>
    </mat-card-actions>
  </mat-card>
} @else {
  <p>Aucun collaborateur ne porte l'identifiant « {{ id() }} ».</p>
  <a matButton routerLink="/collaborateurs">Retour à la liste</a>
}
```

`matButton` s'applique aussi bien à un `<a>` qu'à un `<button>` : un bouton **fait** quelque chose, un lien **mène** quelque part. « Retour à la liste » mène quelque part.

Dans le test `fiche-collaborateur.spec.ts`, l'entrée a changé et le composant utilise maintenant `RouterLink`, qui a besoin du routeur :

```ts
import { provideRouter } from '@angular/router';
```

```ts
    await TestBed.configureTestingModule({
      imports: [FicheCollaborateur],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(FicheCollaborateur);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('id', '1');
    await fixture.whenStable();
```

Notez `'1'`, une chaîne : c'est ce que fournira le routeur. Sans `provideRouter`, le test échoue sur `NG0201: No provider found for ActivatedRoute`. Vous ajouterez la même ligne `providers: [provideRouter([])]` aux tests de tous les composants qui utilisent `RouterLink` ou `Router`.

### 4. Deux écrans provisoires

```powershell
npx ng generate component collaborateurs/formulaire-collaborateur
```

```powershell
npx ng generate component commun/page-introuvable
```

Le formulaire sera écrit au [module 17](17-formulaires.md) ; pour l'instant, son template annonce simplement :

```html
<h2>Nouveau collaborateur</h2>
<p>Le formulaire sera construit au module 17.</p>
```

`page-introuvable.html` :

```html
<h2>Page introuvable</h2>
<p>L'adresse demandée ne correspond à aucun écran de Chronos.</p>
<a matButton="filled" routerLink="/">Retour à l'accueil</a>
```

Ajoutez `MatButtonModule` et `RouterLink` aux `imports` de `PageIntrouvable`.

### 5. Les routes des collaborateurs

Créez `src/app/collaborateurs/collaborateurs.routes.ts` :

```ts
import { Routes } from '@angular/router';
import { ListeCollaborateurs } from './liste-collaborateurs/liste-collaborateurs';

export const collaborateursRoutes: Routes = [
  {
    path: '',
    component: ListeCollaborateurs,
    title: 'Collaborateurs — Chronos',
  },
  {
    path: 'nouveau',
    loadComponent: () =>
      import('./formulaire-collaborateur/formulaire-collaborateur').then(
        (m) => m.FormulaireCollaborateur,
      ),
    title: 'Nouveau collaborateur — Chronos',
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./fiche-collaborateur/fiche-collaborateur').then((m) => m.FicheCollaborateur),
    title: 'Fiche collaborateur — Chronos',
  },
];
```

Les chemins sont relatifs au point où ce fichier sera branché : `''` deviendra `/collaborateurs`, `:id` deviendra `/collaborateurs/:id`.

**`nouveau` est placé avant `:id`, et ce n'est pas un détail.** Dans l'ordre inverse, `/collaborateurs/nouveau` serait reconnu par `:id`, avec `id` valant `'nouveau'` : on verrait la fiche d'un collaborateur introuvable au lieu du formulaire.

La liste, écran d'arrivée de la fonctionnalité, est référencée directement (`component`) ; le formulaire et la fiche, visités moins souvent, sont chargés à la demande.

### 6. Les routes de l'application

`src/app/app.routes.ts` :

```ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'collaborateurs' },
  {
    path: 'collaborateurs',
    loadChildren: () =>
      import('./collaborateurs/collaborateurs.routes').then((m) => m.collaborateursRoutes),
  },
  {
    path: '**',
    loadComponent: () =>
      import('./commun/page-introuvable/page-introuvable').then((m) => m.PageIntrouvable),
    title: 'Page introuvable — Chronos',
  },
];
```

Trois routes, trois mécanismes :

- **la redirection** : l'adresse vide renvoie vers `/collaborateurs`. `pathMatch: 'full'` est indispensable : par défaut, un chemin est comparé au *début* de l'adresse, et le chemin vide est le début de toutes les adresses — sans cette précision, tout serait redirigé ;
- **`loadChildren`** : tout ce qui commence par `/collaborateurs` est confié au fichier de routes de la fonctionnalité, chargé à la demande ;
- **le joker `**`**, en dernier : toute autre adresse affiche la page introuvable.

Ce fichier n'importe plus aucun composant : il ne fait que des `import()` dynamiques.

### 7. La coquille : barre de titre, menu, emplacement

`App` cesse d'afficher la liste : il devient le cadre commun à tous les écrans. `src/app/app.ts` :

```ts
import { Component, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

interface LienDeNavigation {
  chemin: string;
  libelle: string;
  icone: string;
}

@Component({
  imports: [
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatToolbarModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly titre = signal('Chronos');

  protected readonly liens: LienDeNavigation[] = [
    { chemin: '/collaborateurs', libelle: 'Collaborateurs', icone: 'group' },
  ];
}
```

`src/app/app.html` :

```html
<mat-toolbar>
  <button
    matIconButton
    type="button"
    aria-label="Afficher ou masquer le menu"
    (click)="menu.toggle()"
  >
    <mat-icon>menu</mat-icon>
  </button>
  <h1>{{ titre() }}</h1>
</mat-toolbar>

<mat-sidenav-container>
  <mat-sidenav #menu mode="side" opened>
    <mat-nav-list aria-label="Menu principal">
      @for (lien of liens; track lien.chemin) {
        <a
          mat-list-item
          [routerLink]="lien.chemin"
          routerLinkActive
          #etat="routerLinkActive"
          [activated]="etat.isActive"
          ariaCurrentWhenActive="page"
        >
          <mat-icon matListItemIcon>{{ lien.icone }}</mat-icon>
          <span matListItemTitle>{{ lien.libelle }}</span>
        </a>
      }
    </mat-nav-list>
  </mat-sidenav>

  <mat-sidenav-content role="main">
    <router-outlet />
  </mat-sidenav-content>
</mat-sidenav-container>
```

`src/app/app.scss` :

```scss
:host {
  display: flex;
  flex-direction: column;
  height: 100%;
}

h1 {
  font: inherit;
  margin: 0 0 0 8px;
}

mat-sidenav-container {
  flex: 1;
}

mat-sidenav {
  width: 240px;
}

mat-sidenav-content {
  padding: 24px;
}
```

Ce qu'il faut y lire :

- `<mat-sidenav-container>` contient le menu latéral (`<mat-sidenav>`) et le contenu principal (`<mat-sidenav-content>`). `mode="side"` place le menu à côté du contenu ; `opened` l'ouvre au démarrage ; `#menu` permet au bouton de la barre d'appeler `menu.toggle()` ;
- `<mat-nav-list>` est la liste faite pour des liens de navigation. Elle porte elle-même le rôle `navigation` ; on lui donne un nom avec `aria-label` ;
- sur chaque lien, `routerLinkActive` surveille si sa route est active. `#etat="routerLinkActive"` donne accès à ce résultat, et `[activated]="etat.isActive"` le transmet à Material, qui met le lien en évidence. `ariaCurrentWhenActive="page"` pose `aria-current="page"` ;
- `role="main"` désigne le contenu principal de la page, comme le recommande la documentation du composant ;
- `<router-outlet />` est l'emplacement où s'affichera l'écran de la route active.

Le test `app.spec.ts` a besoin du routeur, lui aussi : ajoutez `providers: [provideRouter([])]` à son `configureTestingModule`.

Documentation : [barre d'outils](https://material.angular.dev/components/toolbar/overview), [panneau latéral](https://material.angular.dev/components/sidenav/overview), [listes de navigation](https://material.angular.dev/components/list/overview)

### 8. Des liens à la place de la sélection

Au module 14, la carte émettait une sortie et la liste retenait le collaborateur sélectionné. Cet état vit désormais dans l'URL : `/collaborateurs/3` *est* la sélection. Le routage remplace trois mécanismes :

| Module 14 | Module 15 |
| --- | --- |
| Sortie `selection` de la carte | Lien `routerLink` vers `/collaborateurs/:id` |
| `linkedSignal` `selection` dans la liste | L'adresse de la page |
| Entrée `active` et bouton « Fermer » projeté | Bouton « Précédent » du navigateur, lien « Retour à la liste » |

Dans `CarteCollaborateur`, supprimez l'entrée `active` et la sortie `selection` (et leurs imports), la classe `[class.active]` et sa règle SCSS, ajoutez `RouterLink` aux `imports`, puis remplacez le bouton :

```html
  <mat-card-actions align="end">
    <a matButton [routerLink]="['/collaborateurs', collaborateur().id]">
      <mat-icon>visibility</mat-icon>
      Voir la fiche
    </a>
  </mat-card-actions>
```

Dans `ListeCollaborateurs`, supprimez le `linkedSignal`, `FicheCollaborateur` des `imports`, et dans le template le bloc `@if (selection(); …)` ainsi que la `<div class="contenu">` qui plaçait la fiche à côté des cartes. La boucle redevient :

```html
<div class="cartes">
  @for (c of resultats; track c.id) {
    <app-carte-collaborateur [collaborateur]="c" />
  } @empty {
    <p>Aucun collaborateur ne correspond à « {{ recherche() }} ».</p>
  }
</div>
```

Ajoutez dans la `<div class="outils">` le lien vers le futur formulaire (`MatIconModule` et `RouterLink` rejoignent les `imports`) :

```html
  <a matButton="filled" routerLink="nouveau">
    <mat-icon>add</mat-icon>
    Nouveau collaborateur
  </a>
```

`routerLink="nouveau"`, sans `/` initial, est relatif à la route de la liste : il mène à `/collaborateurs/nouveau`.

Relancez `npm start` et ouvrez `http://localhost:4200/`. L'adresse devient `/collaborateurs`, l'onglet s'intitule « Collaborateurs — Chronos », le lien du menu est en évidence. « Voir la fiche » mène à `/collaborateurs/1` ; le bouton « Précédent » du navigateur ramène à la liste. Essayez aussi `/collaborateurs/999`, `/collaborateurs/abc` et `/nimporte/quoi`.

### 9. La recherche dans l'adresse

Tapez `mar` dans la recherche, ouvrez une fiche, revenez : la recherche est perdue. Elle doit vivre dans l'adresse, sous la forme `/collaborateurs?recherche=mar`.

Dans `ListeCollaborateurs`, le signal `recherche` devient une **entrée**, que le routeur alimentera :

```ts
import { Component, computed, inject, input, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
```

```ts
export class ListeCollaborateurs {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly recherche = input('', { transform: (valeur: string | undefined) => valeur ?? '' });

  // collaborateurs, afficherSortis et collaborateursFiltres : inchangés

  protected rechercher(texte: string): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { recherche: texte === '' ? null : texte },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
```

Une entrée est en lecture seule : `[(recherche)]` n'est plus possible. On écrit les deux moitiés de la liaison séparément :

```html
  <app-barre-de-recherche
    libelle="Filtrer par nom"
    [recherche]="recherche()"
    (rechercheChange)="rechercher($event)"
  />
```

Le circuit complet : l'utilisateur tape → `BarreDeRecherche` émet `rechercheChange` → `rechercher()` demande au routeur de changer l'adresse → le routeur renseigne l'entrée `recherche` → `collaborateursFiltres` se recalcule et la barre reçoit la nouvelle valeur. **L'adresse est la seule source de vérité** ; le composant ne garde aucune copie du texte.

Les options de `navigate` :

| Option | Rôle |
| --- | --- |
| `[]` et `relativeTo: this.route` | Rester sur la route courante : seuls les paramètres changent. `inject()` fournit le routeur et la route active, sans constructeur. |
| `queryParams` | Les paramètres à écrire. `null` retire le paramètre : une recherche vide ne laisse pas de `?recherche=` dans l'adresse. |
| `queryParamsHandling: 'merge'` | Conserver les autres paramètres de requête déjà présents. |
| `replaceUrl: true` | Remplacer l'entrée courante de l'historique au lieu d'en ajouter une à chaque frappe ; sans cela, « Précédent » défilerait lettre par lettre. |

Vérifiez : tapez `mar`, l'adresse devient `/collaborateurs?recherche=mar` ; rechargez la page, le filtre est toujours là ; ouvrez une fiche puis revenez en arrière, il l'est encore.

### 10. Observer le chargement à la demande

```powershell
npm run build
```

Le compte rendu distingue maintenant deux groupes de fichiers :

```
Initial chunk files | Names                    |  Raw size
main-XVU3CUEK.js    | main                     | 341.08 kB
chunk-4Rt4q9eI.js   | -                        | 163.42 kB
styles-OPUTW5UJ.css | styles                   |   8.04 kB
                    | Initial total            | 512.55 kB

Lazy chunk files    | Names                    |  Raw size
chunk-DWkpqsH-.js   | collaborateurs-routes    |  86.69 kB
chunk-BdllczjF.js   | fiche-collaborateur      |   2.25 kB
chunk-BtY9A79-.js   | page-introuvable         | 580 bytes
chunk-BB-EPSBe.js   | formulaire-collaborateur | 408 bytes
```

Chaque `loadComponent` et chaque `loadChildren` a produit un fichier, que le navigateur ne demandera qu'en visitant la route. Vous pouvez le voir dans l'onglet « Réseau » des outils de développement : le fichier de la fiche n'arrive qu'au premier clic sur « Voir la fiche ».

L'avertissement de budget du module 14 est toujours là : la coquille utilise elle-même la barre d'outils, le panneau latéral et la liste de Material, qui restent dans le chargement initial. Deux lectures sont possibles — le seuil par défaut de 500 kB est trop bas pour une application Material, ou la coquille est trop lourde — et la colonne « Estimated transfer size » (environ 117 kB après compression) aide à trancher. Ne changez pas `angular.json` avant d'en avoir parlé en séance.

## À vous

1. **Les autres écrans.** Ajoutez les routes `/projets`, `/plan-de-charge`, `/feuille-de-temps`, `/absences` et `/connexion`, chacune vers un composant provisoire rangé dans le dossier de sa fonctionnalité (`projets/`, `plan-de-charge/`, `feuilles-de-temps/`, `absences/`, `compte/`), chargé à la demande et doté d'un titre. Complétez le menu, avec une icône par entrée. Vérifiez dans le compte rendu de `npm run build` que chaque écran a son propre fichier. La page de connexion doit-elle figurer dans le menu ?

2. **Prédire l'ordre.** Dans `collaborateurs.routes.ts`, placez `:id` avant `nouveau`. Prédisez, *avant d'essayer*, ce qu'affichera `/collaborateurs/nouveau`, puis vérifiez. Faites de même en déplaçant la route `**` en tête de `app.routes.ts`. Remettez l'ordre correct.

3. **Un identifiant qui n'en est pas un.** `/collaborateurs/abc` affiche « Aucun collaborateur ne porte l'identifiant « NaN » ». Rendez le message correct pour l'utilisateur. Puis discutez : cette adresse devrait-elle plutôt afficher la page introuvable ? Cherchez dans la documentation ce qui permettrait à une route de ne pas reconnaître `abc`.

4. **Le statut dans l'adresse.** Le choix « afficher les sortis » se perd, lui aussi, au rechargement. Faites-en un paramètre de requête `statut`, sur le modèle de `recherche`. Vérifiez que modifier la recherche ne fait pas disparaître `statut`, et inversement : quelle option de `navigate` le garantit ?

5. **Paginer.** Affichez les collaborateurs deux par deux, avec un paramètre `page` et des liens « Précédent » et « Suivant » — des liens `routerLink` avec `[queryParams]`, pas des boutons. Que doit devenir `page` quand la recherche change ? Que faire de `?page=99` ? Rappelez-vous que `page` arrive sous forme de texte, ou n'arrive pas.

6. **Un titre qui nomme.** L'onglet de la fiche affiche « Fiche collaborateur — Chronos » pour tout le monde. Faites-lui afficher « Camille Martin — Chronos ». La propriété `title` d'une route accepte une fonction : voyez la section [« Page titles »](https://angular.dev/guide/routing/define-routes#page-titles). Pourquoi un titre précis compte-t-il pour un utilisateur de lecteur d'écran, ou pour quelqu'un qui a dix onglets ouverts ?

## Critères de réussite

- `npm run build` réussit et liste des « Lazy chunk files » ; `npm test` passe.
- `/` redirige vers `/collaborateurs` ; `/collaborateurs/1` affiche la fiche de Camille Martin ; `/collaborateurs/nouveau` affiche l'écran provisoire du formulaire, pas une fiche ; une adresse inconnue affiche la page introuvable.
- Chaque écran a un titre d'onglet distinct.
- Une recherche apparaît dans l'adresse, survit au rechargement et au retour arrière, et n'ajoute pas une entrée d'historique par lettre.
- Le lien de l'écran courant porte `aria-current="page"` (vérifiable dans l'inspecteur du navigateur).
- Dans vos templates, on ne change d'écran que par des `<a routerLink>` ; aucun `<a href>` interne, aucun `(click)` sur un `<div>`.
- Aucune entrée liée à l'URL n'est typée `number` sans `transform`.
- Comparez avec la solution de référence : `git diff module-14 module-15`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `readonly id = input.required<number>()` pour un paramètre de route | Le routeur fournit toujours une chaîne : `'3' === 3` est faux, la recherche échoue sans erreur de compilation. | `input.required({ transform: numberAttribute })`. |
| Compter sur la valeur par défaut d'une entrée liée à un paramètre de requête | Quand le paramètre est absent, le routeur écrit `undefined` dans l'entrée. | Rétablir la valeur par défaut dans `transform`. |
| `:id` déclaré avant `nouveau` ; `**` ailleurs qu'en dernier | La première route qui correspond gagne. | Du plus précis au plus général. |
| `{ path: '', redirectTo: 'collaborateurs' }` sans `pathMatch: 'full'` | Le chemin vide est le préfixe de toutes les adresses. | `pathMatch: 'full'`. |
| `<a href="/collaborateurs/3">` | Recharge toute l'application à chaque clic. | `<a [routerLink]="['/collaborateurs', 3]">`. |
| `<button (click)="router.navigate(…)">` pour un simple changement d'écran | Ce n'est plus un lien : pas d'ouverture dans un nouvel onglet, pas d'adresse au survol, rôle erroné pour un lecteur d'écran. | Un `<a routerLink>` ; `navigate` quand la navigation découle d'une action. |
| `inject(ActivatedRoute)` puis `route.snapshot.paramMap.get('id')` | Lecture figée : le composant n'est pas recréé quand seul le paramètre change, et l'ancienne valeur reste affichée. | Une entrée `input()` liée par `withComponentInputBinding()`. |
| `route.params.subscribe(…)` dans `ngOnInit` | Fonctionne, mais oblige à gérer un abonnement pour une valeur qu'une entrée fournit déjà. | Une entrée `input()`. |
| `RouterModule.forRoot(routes)`, `RouterModule.forChild(routes)` | Configuration par `NgModule`. | `provideRouter(routes)` et `loadChildren` vers un fichier de routes. |
| `loadChildren: () => import('./x.module').then(m => m.XModule)` | Charge un `NgModule` ; la forme actuelle charge un tableau de routes. | `.then((m) => m.xRoutes)`. |
| `provideRoutes()` | Supprimé en version 22. | `provideRouter(routes)`. |
| Garde écrite comme une classe (`implements CanActivate`) | Déconseillé. | Une fonction `CanActivateFn` (module 19). |
| `RouterTestingModule` dans un test | Déprécié. | `providers: [provideRouter([])]`. |
| Dupliquer le texte recherché dans un signal local « pour aller plus vite » | Deux sources de vérité : l'adresse et le signal finissent par diverger. | L'adresse seule, lue par l'entrée. |
| Tout charger à la demande, y compris l'écran d'arrivée | Un aller-retour réseau de plus avant le premier affichage. | `component` pour l'écran d'arrivée, `loadComponent` pour les autres. |

La liste complète des pratiques obsolètes, avec les sources, est dans le [référentiel Angular 22](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Dressez la liste des états de votre écran des collaborateurs. Lesquels avez-vous mis dans l'URL, lesquels dans un signal, et sur quel critère ?
- Le module 14 vous a fait écrire une sortie et un `linkedSignal` que ce module vous a fait supprimer. Était-ce du travail perdu ? Dans quel cas la fiche à côté de la liste aurait-elle été le bon choix ?
- En développement, `http://localhost:4200/collaborateurs/3` fonctionne même après un rechargement. Que doit faire le serveur de production pour qu'il en soit de même ? (Le [module 23](23-conteneuriser-l-application.md) y reviendra.)
- Le chargement initial dépasse le budget de 500 kB. Relevez-vous le seuil, allégez-vous la coquille, ou laissez-vous l'avertissement ? Sur quels chiffres appuyez-vous votre décision ?
- Un utilisateur saisit à la main `/collaborateurs/42`, la fiche d'un collègue qu'il n'a pas le droit de voir (RG-50). À ce stade, que se passe-t-il ? Qu'est-ce qui devra l'en empêcher : le routeur, l'API, les deux ?
- Chaque fonctionnalité a son fichier de routes. Qu'y gagne une équipe de cinq développeurs ?

## Pour aller plus loin

- [Options de `withComponentInputBinding`](https://angular.dev/guide/routing/common-router-tasks#getting-route-information) : priorité entre les sources, désactivation des paramètres de requête et, depuis la version 22, comportement des entrées absentes de l'URL.
- [Stratégie de titre personnalisée](https://angular.dev/guide/routing/define-routes#using-titlestrategy-for-page-titles) (`TitleStrategy`) : ajouter « — Chronos » à tous les titres en un seul endroit.
- [Résolveurs de données](https://angular.dev/guide/routing/data-resolvers) et [chargement de données par les routes](https://angular.dev/guide/routing/data-fetching-with-resources) : obtenir les données avant d'afficher l'écran.
- [Tester le routage](https://angular.dev/guide/routing/testing) avec `RouterTestingHarness` ; le [module 20](20-tester-l-interface.md) s'en servira.
- Accessibilité du menu : la [documentation des listes](https://material.angular.dev/components/list/overview) recommande d'envelopper les liens d'une `<mat-nav-list>` dans une liste `<ul>` / `<li>` ; le [module 21](21-accessibilite-et-finitions.md) reprendra ce point.
- Suite dans le cours : le [module 16](16-dialoguer-avec-l-api.md) remplace les données en dur par des appels à l'API — et l'entrée `id` de la fiche deviendra le paramètre d'une requête.
