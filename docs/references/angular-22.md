# Référentiel Angular 22 — pratiques à jour

> **Vérifié le 12 novembre 2026** sur angular.dev (documentation en v22.2.6), les notes de version GitHub et les schémas de la CLI.
> Ce document fait foi pour le cours : en cas de doute entre un tutoriel trouvé en ligne et ce référentiel, c'est le référentiel — ou mieux, la documentation officielle du jour — qui a raison.
> À revérifier à chaque nouvelle version mineure (22.3 attendue fin 2026) et avant toute montée de version majeure.

## 1. Versions

| Élément | Valeur | Source |
| --- | --- | --- |
| Angular (dernier correctif) | 22.2.6 | [releases](https://github.com/angular/angular/releases) |
| Sortie de la v22 | 3 juin 2026 | [calendrier](https://angular.dev/reference/releases) |
| Support actif / LTS | jusqu'à juin 2027 / juin 2028 | [calendrier](https://angular.dev/reference/releases) |
| Prochaine majeure (v23) | vers juin 2027 | [calendrier](https://angular.dev/reference/releases) |
| Node.js | `^22.22.3`, `^24.15.0` ou `^26.0.0` | [compatibilité](https://angular.dev/reference/versions) |
| TypeScript | `>=6.0.0 <6.1.0` | [compatibilité](https://angular.dev/reference/versions) |
| RxJS | `^6.5.3` ou `^7.4.0` | [compatibilité](https://angular.dev/reference/versions) |
| Angular Material / CDK | 22.2.7 | [releases](https://github.com/angular/components/releases) |

Depuis la v22, Angular publie **une version majeure par an** (au lieu de deux), avec 4 à 6 versions mineures entre-temps.

**Attention à TypeScript.** La dernière version de TypeScript est la 7.0, que Angular 22 n'accepte pas. On garde la version posée par `ng new` (`~6.0.2`) et on ne lance pas de `npm install typescript@latest`.

## 2. Ce que génère `ng new` en v22

Valeurs par défaut ([`ng new`](https://angular.dev/cli/new)) :

| Option | Défaut | Conséquence |
| --- | --- | --- |
| `standalone` | oui | Pas de `NgModule`. On n'écrit pas `standalone: true` : c'est implicite. |
| `zoneless` | oui | `zone.js` n'est pas installé. La détection de changement repose sur les signals. |
| `test-runner` | `vitest` | Tests unitaires avec Vitest et jsdom. |
| `file-name-style-guide` | `2025` | Fichiers sans suffixe de type : `app.ts`, `user-profile.ts`, `auth-guard.ts`. |
| `strict` | oui | TypeScript strict. |
| `ssr` | non (question posée) | Chronos est une application de gestion interne : rendu côté client uniquement. |
| `style` | `css` (question posée) | Le cours utilise SCSS, sur lequel repose la personnalisation du thème Angular Material. |

Points à connaître :

- Le composant racine s'appelle `App` et vit dans `src/app/app.ts` — et non `AppComponent` dans `app.component.ts`.
- Les fichiers statiques vont dans `public/` — et non `src/assets/`.
- Les fichiers `environment.ts` ne sont plus générés ; `ng generate environments` les crée si besoin.
- Le builder est `@angular/build:application` (esbuild, avec Vite pour le serveur de développement).
- `ng generate component` crée un composant en `OnPush`, qui est le mode par défaut depuis la v22.
- `ng generate service` crée une classe décorée `@Service()`.
- `ng generate guard` crée une garde fonctionnelle.

## 3. Ce que le cours enseigne

| Sujet | Pratique retenue | Source |
| --- | --- | --- |
| État local | `signal`, `computed`, `linkedSignal`. `effect` en dernier recours, jamais pour recopier un signal dans un autre. | [signals](https://angular.dev/guide/signals), [effect](https://angular.dev/guide/signals/effect) |
| Entrées / sorties | `input()`, `input.required()`, `output()`, `model()`, déclarés `readonly`. | [inputs](https://angular.dev/guide/components/inputs) |
| Requêtes de vue | `viewChild()`, `contentChild()`. | [API](https://angular.dev/api/core/viewChild) |
| Templates | `@if`, `@for` (avec `track`), `@switch`, `@let`, `@defer`. | [flux de contrôle](https://angular.dev/guide/templates/control-flow) |
| Classes et styles | `[class.actif]="..."`, `[style.width.px]="..."`. | [bindings](https://angular.dev/guide/templates/binding) |
| Élément hôte | Propriété `host` du décorateur. | [host](https://angular.dev/guide/components/host-elements) |
| Injection | `inject()`, dans un contexte d'injection (initialiseur de champ ou constructeur — pas dans `ngOnInit`). | [guide de style](https://angular.dev/style-guide) |
| Services | `@Service()` pour un singleton utilisant `inject()`. | [services](https://angular.dev/guide/di/creating-and-using-services) |
| Lecture HTTP | `httpResource()` : expose `value()`, `hasValue()`, `error()`, `isLoading()`, `reload()`. | [httpResource](https://angular.dev/guide/http/http-resource) |
| Écriture HTTP | `HttpClient` directement (POST, PUT, DELETE). | [requêtes](https://angular.dev/guide/http/making-requests) |
| Configuration HTTP | `HttpClient` fonctionne sans configuration, avec `fetch`. `provideHttpClient(withInterceptors([...]))` uniquement pour ajouter des intercepteurs. | [configuration](https://angular.dev/guide/http/setup) |
| Intercepteurs | Fonctions `HttpInterceptorFn`. | [intercepteurs](https://angular.dev/guide/http/interceptors) |
| Formulaires | **Signal Forms** (`@angular/forms/signals`), stables depuis la v22. | [Signal Forms](https://angular.dev/essentials/signal-forms) |
| Routage | Gardes et résolveurs fonctionnels (`CanActivateFn`, `ResolveFn`), `loadComponent` / `loadChildren`, `withComponentInputBinding()`. | [gardes](https://angular.dev/guide/routing/route-guards) |
| Redirection dans une garde | Retourner un `UrlTree` ou un `RedirectCommand` — pas `false` suivi d'un `router.navigate()`. | [gardes](https://angular.dev/guide/routing/route-guards) |
| Tests unitaires | Vitest, `TestBed`, `await fixture.whenStable()`, `provideHttpClientTesting()`. | [tests](https://angular.dev/guide/testing) |
| Tests de composants | Harnais du CDK et d'Angular Material. | [harnais](https://angular.dev/guide/testing/component-harnesses-overview) |
| Interface | Angular Material 22 (thème Material 3) ; Angular Aria pour des composants accessibles sans style imposé. | [Aria](https://angular.dev/guide/aria/overview) |
| Animations | CSS natif avec `animate.enter` / `animate.leave`. | [animations](https://angular.dev/guide/animations/css) |
| Gestion d'état | Signals dans des services. Aucune bibliothèque n'est recommandée officiellement ; le cours n'en introduit pas. | [bonnes pratiques](https://angular.dev/assets/context/best-practices.md) |
| Organisation | Un dossier par fonctionnalité, tests à côté du code, un concept par fichier. Pas de dossiers `components/` ou `services/`. | [guide de style](https://angular.dev/style-guide) |
| Visibilité | `protected` pour ce que seul le template lit. | [guide de style](https://angular.dev/style-guide) |

### Exemple de référence — composant de lecture

```ts
import {Component, input} from '@angular/core';
import {httpResource} from '@angular/common/http';

@Component({
  selector: 'app-collaborateur-detail',
  template: `
    @if (collaborateur.isLoading()) {
      <p>Chargement…</p>
    } @else if (collaborateur.error()) {
      <p>Le collaborateur n'a pas pu être chargé.</p>
    } @else if (collaborateur.hasValue()) {
      <h2>{{ collaborateur.value().prenom }} {{ collaborateur.value().nom }}</h2>
    }
  `,
})
export class CollaborateurDetail {
  readonly id = input.required<string>();

  protected readonly collaborateur = httpResource<Collaborateur>(
    () => `/api/collaborateurs/${this.id()}`,
  );
}
```

Ce qu'on ne voit **pas** dans cet exemple est aussi important : pas de `NgModule`, pas de `standalone: true`, pas de `changeDetection`, pas de constructeur, pas de `subscribe`, pas de `*ngIf`.

### Exemple de référence — formulaire

```ts
import {Component, signal} from '@angular/core';
import {email, form, FormField, FormRoot, required} from '@angular/forms/signals';

@Component({
  selector: 'app-collaborateur-form',
  imports: [FormField, FormRoot],
  template: `
    <form [formRoot]="formulaire">
      <input type="text" [formField]="formulaire.nom" />
      <input type="email" [formField]="formulaire.email" />
      @if (formulaire.email().touched() && formulaire.email().invalid()) {
        @for (erreur of formulaire.email().errors(); track erreur) {
          <p>{{ erreur.message }}</p>
        }
      }
      <button type="submit" [disabled]="formulaire().submitting()">Enregistrer</button>
    </form>
  `,
})
export class CollaborateurForm {
  protected readonly modele = signal({nom: '', email: ''});

  protected readonly formulaire = form(this.modele, (champs) => {
    required(champs.nom, {message: 'Le nom est obligatoire.'});
    email(champs.email, {message: "L'adresse électronique est invalide."});
  });
}
```

Le modèle d'un Signal Form est un objet simple, entièrement initialisé : chaîne vide pour un champ texte, jamais `undefined` ([modèles](https://angular.dev/guide/forms/signals/models)).

> Ces deux exemples sont assemblés à partir des API documentées et n'ont pas été compilés tels quels. Le code équivalent des modules 16 et 17 l'a été ; en cas d'écart, ce sont les modules qui font foi.

## 4. Pièges : ce qu'on trouve encore partout et qu'on n'écrit plus

« Déprécié » signifie que le retrait est annoncé. « Déconseillé » signifie que l'API fonctionne toujours mais que la documentation oriente vers autre chose pour du code neuf.

| Ce qu'on voit dans les tutoriels | Statut en v22 | À écrire à la place | Source |
| --- | --- | --- | --- |
| `NgModule`, `AppModule` | Déconseillé | Composants autonomes | [NgModules](https://angular.dev/guide/ngmodules/overview) |
| `standalone: true` | Redondant | Ne rien écrire | [bonnes pratiques](https://angular.dev/assets/context/best-practices.md) |
| `*ngIf`, `*ngFor`, `ngSwitch` | **Déprécié** depuis la v20 | `@if`, `@for`, `@switch` | [NgIf](https://angular.dev/api/common/NgIf) |
| `@Input()`, `@Output()` | Déconseillé | `input()`, `output()`, `model()` | [inputs](https://angular.dev/guide/components/inputs) |
| `@ViewChild()`, `@ContentChild()` | Déconseillé | `viewChild()`, `contentChild()` | [API](https://angular.dev/api/core/viewChild) |
| `@HostBinding`, `@HostListener` | Conservés pour compatibilité | Propriété `host` | [HostBinding](https://angular.dev/api/core/HostBinding) |
| Injection par constructeur | Déconseillé | `inject()` | [guide de style](https://angular.dev/style-guide) |
| `@Injectable({providedIn: 'root'})` | Déconseillé pour un nouveau singleton | `@Service()` | [migration](https://angular.dev/reference/migrations/injectable-to-service) |
| `CommonModule` | Déconseillé | Importer chaque pipe utilisé (`DatePipe`…) | [CommonModule](https://angular.dev/api/common/CommonModule) |
| `ngClass`, `ngStyle` | Déconseillé | `[class]`, `[style]` | [bindings](https://angular.dev/guide/templates/binding) |
| `HttpClientModule` | **Déprécié** | Rien, ou `provideHttpClient(...)` | [HttpClientModule](https://angular.dev/api/common/http/HttpClientModule) |
| `withFetch()` | **Déprécié** en v22 | Le supprimer : `fetch` est le défaut | [withFetch](https://angular.dev/api/common/http/withFetch) |
| Intercepteurs à base de classes | Déconseillé | `HttpInterceptorFn` | [intercepteurs](https://angular.dev/guide/http/interceptors) |
| Gardes à base de classes (`implements CanActivate`) | Déconseillé | `CanActivateFn` | [gardes](https://angular.dev/guide/routing/route-guards) |
| `ReactiveFormsModule`, `FormGroup`, `FormBuilder` | Toujours pris en charge ; réservé au code existant | Signal Forms | [comparaison](https://angular.dev/guide/forms/signals/comparison) |
| `zone.js`, `NgZone`, `fakeAsync` | Plus installé par défaut | Signals, `async`/`await` dans les tests | [zoneless](https://angular.dev/guide/zoneless) |
| `changeDetection: ChangeDetectionStrategy.OnPush` | Redondant en v22 | Ne rien écrire | [API](https://angular.dev/api/core/ChangeDetectionStrategy) |
| `ChangeDetectionStrategy.Default` | **Déprécié** | `Eager`, si vraiment nécessaire | [API](https://angular.dev/api/core/ChangeDetectionStrategy) |
| `ComponentFactoryResolver` | **Supprimé** en v22 | `ViewContainerRef.createComponent(Classe)` | [v22.0.0](https://github.com/angular/angular/releases/tag/v22.0.0) |
| `provideRoutes()` | **Supprimé** en v22 | `provideRouter(routes)` | [v22.0.0](https://github.com/angular/angular/releases/tag/v22.0.0) |
| `@angular/animations`, `provideAnimations()` | **Déprécié**, retrait visé en v23 | `animate.enter` / `animate.leave` | [animations héritées](https://angular.dev/guide/legacy-animations) |
| Karma et Jasmine | Pris en charge, mais plus le défaut | Vitest | [Karma](https://angular.dev/guide/testing/karma) |
| Jest via la CLI | Builder **supprimé** en v22 | Vitest | [CLI v22.0.0](https://github.com/angular/angular-cli/releases/tag/v22.0.0) |
| `HttpClientTestingModule`, `RouterTestingModule` | **Déprécié** | `provideHttpClientTesting()`, `provideRouter()` | [API](https://angular.dev/api/common/http/testing/HttpClientTestingModule) |
| Protractor | Disparu | Playwright ou Cypress via `ng e2e` | [e2e](https://angular.dev/tools/cli/end-to-end) |
| `platformBrowserDynamic` | **Déprécié** | `bootstrapApplication` (ou `platformBrowser`) | [API](https://angular.dev/api/platform-browser-dynamic/platformBrowserDynamic) |
| Fichiers `x.component.ts`, `x.service.ts`, `auth.guard.ts` | Ancien style (2016) | `x.ts`, `auth-guard.ts` | [structure](https://angular.dev/reference/configs/file-structure) |
| `src/assets/` | Ancien emplacement | `public/` | [structure](https://angular.dev/reference/configs/file-structure) |
| Builder webpack (`@angular-devkit/build-angular:browser`) | **Déprécié** | `@angular/build:application` | [migration](https://angular.dev/tools/cli/build-system-migration) |

### Ce qui n'est *pas* obsolète, contrairement à ce qu'on lit parfois

- **Le pipe `async`** reste recommandé pour afficher un Observable dans un template. `toSignal()` le complète, il ne le remplace pas ([AsyncPipe](https://angular.dev/api/common/AsyncPipe)).
- **RxJS** reste utile pour les flux d'événements (saisie avec temporisation, WebSocket). Les signals couvrent l'état, pas tous les cas d'asynchronisme.
- **`ng-packagr`** reste l'outil de construction des bibliothèques.

## 5. Points à surveiller

Ces points n'ont pas pu être tranchés par une source officielle, ou les sources officielles se contredisent. Ils sont à revérifier avant d'écrire le module concerné.

- **Formulaires.** La page générale [angular.dev/guide/forms](https://angular.dev/guide/forms) met encore en avant les Reactive Forms, alors que les [bonnes pratiques](https://angular.dev/assets/context/best-practices.md) et la [page de comparaison](https://angular.dev/guide/forms/signals/comparison) recommandent Signal Forms pour une nouvelle application. Le cours suit ces deux dernières.
- **Signal Forms avec Angular Material — tranché par l'essai** (Material 22.2.7, modules 17 et 19). `matInput`, `mat-select` et `mat-error` fonctionnent avec `[formField]` sans adaptation, et posent `aria-invalid`, `aria-describedby` et `aria-required` ; la [documentation de Material](https://material.angular.dev/components/input/overview) le confirme. Sur un champ natif, `[formField]` ne pose pas `aria-invalid`. Pour les dates, le cours retient `<input matInput type="date">` plutôt que `mat-datepicker` : avec l'adaptateur natif en `fr-FR`, « 08/05/2026 » saisi au clavier est lu comme le 5 août, et `toISOString()` décale d'un jour.
- **`ngClass`.** Les bonnes pratiques disent de ne pas l'utiliser ; le guide des bindings le mentionne encore pour quelques cas. Le cours s'en tient à `[class]`.
- **Node.js minimal.** La page de compatibilité (`^22.22.3`) et les notes de la CLI 22.0.0 (`22.22.0`) diffèrent légèrement. On prend la valeur la plus stricte.
- **Billet d'annonce de la v22.** Il n'a pas pu être consulté ; rien dans ce document ne s'appuie sur lui.

### Constats des essais du 12 novembre 2026

Relevés en compilant et en testant le code des modules 13 à 21 (Node.js 24.22.3, CLI 22.2.7, Angular 22.2.6, Material et CDK 22.2.7, TypeScript 6.0.3, Vitest 5.1.0). Chacun est expliqué dans le module cité.

| Constat | Module |
| --- | --- |
| La CLI se lance par `npx @angular/cli@22 …` puis `npx ng …`, sans installation globale ; angular.dev ne documente que l'installation globale. | 00, 13 |
| `ng new` 22 n'écrit ni `engines` ni `devEngines` : le module 13 ajoute `devEngines` (Node `^24.15.0`). Il crée `.prettierrc` et installe Prettier, mais ses propres fichiers échouent à `prettier --check .`. | 13, 22 |
| Avec TypeScript 6, `strict` vaut `true` par défaut et n'apparaît plus dans le `tsconfig.json` généré. | 13, 22 |
| `npm install typescript@latest` (TypeScript 7) réussit avec de simples avertissements ; c'est `ng build` qui échoue ensuite. | 13 |
| `ng add @angular/material` 22.2.7 ne pose qu'une question (la paire de palettes), n'installe pas `@angular/animations`, écrit `mat.theme(...)` dans `styles.scss` et charge Roboto et Material Symbols depuis Google Fonts. | 14 |
| Boutons Material : attributs `matButton="filled\|tonal\|outlined\|elevated"` et `matIconButton` ; `color="primary"` est sans effet avec Material 3. | 14 |
| Dès Material installé, `ng build` signale le dépassement du budget initial de 500 kB (512 kB en fin de module 15, 624 kB en fin de module 18). Le module 21 donne la méthode pour fixer le seuil. | 15, 21 |
| Routeur v22 : `CanMatchFn` reçoit trois paramètres ; `paramsInheritanceStrategy` vaut `'always'` par défaut. Un `canMatch` placé devant `:id` fait tomber `/collaborateurs/nouveau` sur la fiche. | 15, 19 |
| `debounced()` existe en v22 mais reste expérimental ([guide](https://angular.dev/guide/signals/debounced)) : le cours temporise la recherche avec RxJS (`Subject` et `debounceTime`). | 16 |
| Signal Forms : une erreur de soumission bloque tout nouvel envoi tant que rien n'est modifié. | 17 |
| `Temporal` est absent de Node.js 24 et de disponibilité limitée dans les navigateurs : le cours calcule les semaines sur des chaînes ISO, sans bibliothèque. | 18 |
| Protection XSRF : l'intercepteur d'Angular est actif par défaut, y compris sans `provideHttpClient()` et avec `fetch` ; il lit le cookie `XSRF-TOKEN` et pose `X-XSRF-TOKEN` sur les écritures de même origine ([sécurité](https://angular.dev/best-practices/security#httpclient-xsrf-csrf-security)). | 19 |
| Les tests générés échouent dès qu'un composant a un `input.required` (`NG0950`) ou utilise `RouterLink` (`NG0201`). `ng test` reste en mode surveillance dans un terminal : `--no-watch` pour une exécution unique. | 20 |
| `ng e2e` propose `playwright-ng-schematics`, un paquet communautaire. | 20 |
| `i18n.sourceLocale: "fr"` dans `angular.json` suffit à fixer `LOCALE_ID`, à embarquer les données de la locale et à écrire `lang="fr"`, tests compris, sans `@angular/localize`. | 21 |
| Angular Aria est stable en v22 ([feuille de route](https://angular.dev/roadmap)). | 21 |

## 6. Garder ce référentiel à jour

- La CLI fournit un serveur MCP (`npx -y @angular/cli mcp`) dont les outils `get_best_practices` et `search_documentation` interrogent la documentation de la version installée ([MCP](https://angular.dev/ai/mcp)).
- Les bonnes pratiques officielles tiennent en un fichier : [best-practices.md](https://angular.dev/assets/context/best-practices.md).
- `ng update` applique les migrations automatiques d'une version à l'autre ([migrations](https://angular.dev/reference/migrations)).
