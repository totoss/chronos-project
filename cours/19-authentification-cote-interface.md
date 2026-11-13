# Module 19 — Authentification côté interface

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md).
> Le code de ce module a été **rejoué le 12 novembre 2026 sur la solution de référence** (Angular 22.2.6, CLI 22.2.7, Angular Material 22.2.7, TypeScript 6.0.3, Vitest 5.1.0, Node.js 24.22.3), à la suite des modules 15 à 18 : `ng build` réussit (avec l'avertissement de budget) et les tests passent. Il a été parcouru dans Chromium (Playwright 1.65) contre la **véritable API .NET** du module 12, avec les quatre comptes de démonstration (collaboratrice, manager, gestionnaire RH, administrateur sans fiche) : gardes et retour à l'adresse demandée, session qui survit au rechargement sans rien stocker, en-tête `X-XSRF-TOKEN`, session expirée, déconnexion, menus par rôle, appels directs à l'API refusés. Les corrections apportées après cet essai sont signalées dans le texte (étapes 2, 6, 7 et 8). Signalez au mentor tout écart constaté.

Depuis le module 12, l'API sait qui l'appelle : elle répond `401` à un inconnu et `403` à quelqu'un qui n'a pas le droit. L'interface, elle, n'en sait rien : depuis le module 16, vous ouvrez la session à la main dans la console du navigateur, et l'identifiant du collaborateur est écrit en dur. Ce module la met à niveau : un écran de connexion, des routes qui exigent d'être connecté, et des menus qui ne proposent à chacun que ce qu'il peut faire.

## Objectifs

À la fin de ce module, vous saurez :

- expliquer pourquoi l'interface ne conserve aucun jeton, et ce que le navigateur fait à sa place ;
- écrire un service `Session` à signals qui connaît l'utilisateur courant et ses rôles ;
- construire l'écran de connexion et la déconnexion ;
- protéger les routes par des gardes fonctionnelles, rediriger vers la connexion et revenir à la page demandée ;
- réagir aux réponses `401` et `403` dans un intercepteur ;
- dire ce qu'Angular fait contre les attaques CSRF, et ce qui reste à la charge de l'API ;
- adapter l'interface au rôle, en sachant que ce n'est pas une sécurité.

## Prérequis

- [Module 12](12-securite-et-tracabilite.md) : l'API expose `POST /api/compte/login?useCookies=true`, `POST /api/compte/logout` et `GET /api/compte/moi`, et répond `401`, `403` ou `404` selon le cas. Relisez le tableau « Ce que l'interface devra faire » de son étape 5 : ce module l'applique ligne à ligne.
- [Module 15](15-routage.md) : routes chargées à la demande, `withComponentInputBinding()`.
- [Module 16](16-dialoguer-avec-l-api.md) : `HttpClient`, `httpResource`, intercepteurs, et le proxy qui relaie `/api` vers l'API.
- [Module 17](17-formulaires.md) : Signal Forms, `FormulaireCollaborateur`, le jeton `ERREURS_DE_SAISIE_GEREES`. [Module 18](18-ecrans-metier.md) : `FeuilleDeTempsHebdo`, `FeuillesDeTempsApi`, `PlanDeCharge`, et la constante provisoire `COLLABORATEUR_COURANT_ID`.

## Règles de gestion

| Règle | Ce que fait l'interface |
| --- | --- |
| **RG-50** — un collaborateur ne voit que ses propres données | Elle demande les données de l'utilisateur connecté, plus celles d'un identifiant écrit en dur. |
| **RG-51** — un manager voit en plus son équipe | Elle ne propose le plan de charge qu'aux managers et aux gestionnaires RH. |
| **RG-52** — les informations contractuelles ne sont modifiables que par un gestionnaire RH | Elle met ces champs en lecture seule pour les autres, et ne leur propose ni création ni enregistrement. |

Dans les trois cas, **c'est l'API qui applique la règle** (module 12) : elle répond `404` à qui demande une donnée hors de son périmètre, `403` à qui tente une écriture réservée, et n'envoie les dates de contrat qu'au gestionnaire RH et à l'intéressé. L'interface se contente de ne pas proposer ce qui serait refusé.

## Notions

### Un cookie, et aucun jeton dans l'interface

Quand la connexion réussit, l'API répond avec un en-tête `Set-Cookie`. Le navigateur range ce cookie — `chronos.session`, depuis le module 12 — et le renvoie de lui-même avec chaque requête vers la même origine. L'interface n'a rien à stocker ni à ajouter aux requêtes.

Ce cookie porte l'attribut `HttpOnly` : le code JavaScript de la page ne peut pas le lire. C'est tout l'intérêt. Si un script malveillant parvenait à s'exécuter dans Chronos, il ne pourrait pas emporter la session de l'utilisateur.

À l'inverse, un jeton rangé dans `localStorage` est lisible par n'importe quel script de la page. La documentation d'ASP.NET Core recommande donc les cookies pour une application qui tourne dans un navigateur. **Dans Chronos, on n'écrit ni `localStorage.setItem('token', …)`, ni en-tête `Authorization`.**

Cela fonctionne parce que l'interface et l'API ont la **même origine** : en développement grâce au proxy du module 16, en production grâce au serveur web du module 23.

Documentation : [Identity pour une API appelée par une SPA](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0), [cookies HTTP (MDN)](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Cookies)

### L'interface ne protège rien

Tout le code de l'interface s'exécute dans le navigateur de l'utilisateur, qui peut le lire, le modifier, ou appeler l'API sans lui. Une garde de route, un bouton masqué, un champ en lecture seule sont du **confort** : ils évitent de proposer une action vouée à l'échec. La décision appartient à l'API, et à elle seule.

La documentation d'Angular le dit sans détour : ne jamais faire des gardes côté client l'unique contrôle d'accès.

### Gardes : `canActivate` et `canMatch`

Une garde est une fonction que le routeur appelle avant d'afficher une route. Elle renvoie :

- `true` : la navigation continue ;
- `false` : la navigation est refusée ;
- un `UrlTree` ou un `RedirectCommand` : le routeur va ailleurs. C'est la manière de rediriger ; on ne renvoie pas `false` avant d'appeler `router.navigate()`.

Deux moments d'appel nous intéressent :

| | `canActivate` | `canMatch` |
| --- | --- | --- |
| Question posée | « Peut-on entrer dans cette route ? » | « Cette route existe-t-elle pour cet utilisateur ? » |
| Sur `false` | La navigation est annulée, on reste où l'on est — et au premier chargement de la page, nulle part : écran vide. | La route est ignorée ; le routeur essaie les suivantes. |
| Code chargé à la demande | Avec `loadChildren`, les routes enfants sont téléchargées **avant** l'appel de la garde. | Rien n'est téléchargé. |

La dernière ligne a été constatée par un test : avec `canActivate` qui renvoie `false`, la fonction `loadChildren` est tout de même appelée ; avec `canMatch`, jamais. (Pour `loadComponent`, les deux évitent le téléchargement.)

Documentation : [gardes de route](https://angular.dev/guide/routing/route-guards), [`RedirectCommand`](https://angular.dev/api/router/RedirectCommand)

### `401`, `403` et `404`

- `401` : l'API ne sait pas qui vous êtes. La session a expiré, ou n'a jamais existé. L'API de Chronos ne redirige jamais vers une page : elle répond `401`, et c'est à l'interface de réagir. Réponse de l'interface : retour à l'écran de connexion.
- `403` : l'API sait qui vous êtes, et refuse l'action. Se reconnecter n'y changerait rien. Réponse de l'interface : un message, et l'on reste sur la page.
- `404` : la donnée n'existe pas — ou elle est hors de votre périmètre (RG-50, RG-51), ce que l'API ne distingue volontairement pas. Pour l'interface, c'est un « introuvable » ordinaire, déjà traité au module 16.

### CSRF : le revers du cookie

Le navigateur joint le cookie à toute requête vers Chronos. Un autre site pourrait tenter d'en déclencher une à l'insu de l'utilisateur — un formulaire piégé, par exemple. C'est l'attaque CSRF (*cross-site request forgery*).

Le module 12 lui oppose deux barrières. La première est l'attribut `SameSite=Strict` du cookie de session : le navigateur ne le joint pas aux requêtes venues d'un autre site. La seconde est un **second secret**, que seule une page de Chronos peut connaître :

1. l'API dépose un cookie `XSRF-TOKEN`, lisible par JavaScript celui-là — elle le fait dans la réponse à `GET /api/compte/moi` ;
2. l'interface le recopie dans un en-tête `X-XSRF-TOKEN` à chaque requête qui modifie des données ;
3. l'API refuse la requête, par un `400`, si l'en-tête manque ou ne correspond pas.

Un site tiers peut provoquer l'envoi des cookies, mais il ne peut ni les lire ni ajouter un en-tête : sa requête est rejetée.

**Angular fait l'étape 2 tout seul.** `HttpClient` contient un intercepteur qui lit le cookie `XSRF-TOKEN` et pose l'en-tête `X-XSRF-TOKEN`. Voici son comportement, relevé dans des tests et dans Chromium :

| Requête | En-tête `X-XSRF-TOKEN` |
| --- | --- |
| `POST`, `PUT`, `DELETE` vers `/api/...` (URL relative, même origine) | Ajouté |
| `GET`, `HEAD` | Jamais |
| Requête vers une autre origine (`https://autre.example/...`) | Jamais |
| Appel direct à `fetch()`, sans `HttpClient` | Jamais : l'intercepteur n'est pas sur le chemin |

Les étapes 1 et 3 reviennent à l'API : le module 12 les a écrites. Les deux noms, `XSRF-TOKEN` et `X-XSRF-TOKEN`, sont les valeurs par défaut d'Angular, celles que montre la documentation d'ASP.NET Core, et celles que le module 12 a retenues : il n'y a **rien à configurer** dans l'interface. Seule la connexion échappe à la vérification — avant elle, il n'y a pas de session à détourner.

Documentation : [protection XSRF de `HttpClient`](https://angular.dev/best-practices/security#httpclient-xsrf-csrf-security), [prévenir les attaques CSRF dans ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)

## Pas à pas

Le code de ce module va dans `src/app/compte/`.

### 1. Décrire l'utilisateur

`src/app/compte/utilisateur.ts` est le miroir de la réponse de `GET /api/compte/moi` :

```ts
export type Role = 'Collaborateur' | 'Manager' | 'GestionnaireRh' | 'Administrateur';

export interface Utilisateur {
  collaborateurId: number | null;
  nom: string | null;
  prenom: string | null;
  email: string;
  roles: Role[];
}
```

- Le type `Role` énumère les quatre valeurs possibles : une faute de frappe dans `'GestionaireRh'` ne compilera pas.
- `collaborateurId`, `nom` et `prenom` peuvent valoir `null` : un compte n'est pas forcément rattaché à une fiche de collaborateur — celui d'un administrateur technique, par exemple. Le type le dit, et TypeScript vous obligera à traiter le cas.

### 2. Le service `Session`

```powershell
npx ng generate service compte/session
```

La CLI crée `session.ts`, avec une classe décorée `@Service()`, et `session.spec.ts`. Gardez les fichiers `.spec.ts` que la CLI crée tout au long de ce module : le [module 20](20-tester-l-interface.md) les remplira.

```ts
import { HttpClient } from '@angular/common/http';
import { computed, inject, Service, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Role, Utilisateur } from './utilisateur';

@Service()
export class Session {
  private readonly http = inject(HttpClient);
  private readonly utilisateurCourant = signal<Utilisateur | null>(null);

  readonly utilisateur = this.utilisateurCourant.asReadonly();
  readonly estConnecte = computed(() => this.utilisateurCourant() !== null);
  readonly roles = computed<readonly Role[]>(() => this.utilisateurCourant()?.roles ?? []);

  aLeRole(...roles: Role[]): boolean {
    return this.roles().some((role) => roles.includes(role));
  }

  async charger(): Promise<void> {
    try {
      const utilisateur = await firstValueFrom(this.http.get<Utilisateur>('/api/compte/moi'));
      this.utilisateurCourant.set(utilisateur);
    } catch {
      this.utilisateurCourant.set(null);
    }
  }

  async connecter(email: string, password: string): Promise<void> {
    await firstValueFrom(
      this.http.post('/api/compte/login', { email, password }, { params: { useCookies: true } }),
    );
    await this.charger();
  }

  async deconnecter(): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/api/compte/logout', {}));
    } catch {
      // Session déjà expirée (401) ou API injoignable : l'interface se déconnecte quand même.
    }
    this.utilisateurCourant.set(null);
  }

  oublier(): void {
    this.utilisateurCourant.set(null);
  }
}
```

Ce qu'il faut y lire :

- **Un seul signal modifiable, et il est privé.** Le reste de l'application reçoit `utilisateur`, en lecture seule. Seul `Session` décide qui est connecté.
- **`estConnecte` et `roles` sont des `computed`** : ils se recalculent quand l'utilisateur change, et jamais autrement.
- **`aLeRole(...)` est une méthode, parce qu'elle prend des paramètres**, mais elle lit le signal `roles`. Appelée dans un template ou dans un `computed`, elle est donc suivie comme un signal : le menu se mettra à jour tout seul à la connexion.
- **`charger()` ne lève jamais d'erreur.** Un `401` de `/api/compte/moi` n'est pas une panne : il signifie « personne n'est connecté ».
- **`connecter()` laisse passer l'erreur**, au contraire : c'est l'écran de connexion qui doit savoir que la tentative a échoué.
- **`connecter()` enchaîne sur `charger()`**, pour deux raisons. La réponse du `login` est vide : elle ne dit pas qui s'est connecté. Et c'est `GET /api/compte/moi` qui dépose le cookie `XSRF-TOKEN` : sans cet appel, la première écriture serait refusée.
- **Le service ne stocke que ce que l'API lui a dit.** Recharger la page vide le signal ; `charger()` le remplira de nouveau à partir du cookie.
- **`logout` reçoit un objet vide `{}`** : l'API du module 12 l'exige, et répond `400` sans lui. Elle supprime alors `chronos.session` et `XSRF-TOKEN`. (Le cookie `.AspNetCore.Antiforgery.…`, `HttpOnly`, reste : c'est la moitié serveur du jeton anti-CSRF, inutilisable sans session.)
- **`deconnecter()` ne lève jamais d'erreur.** Si la session a déjà expiré, l'API répond `401` au `logout` (constaté) ; sans le `try`, l'erreur remonterait et l'utilisateur resterait affiché comme connecté. Se déconnecter doit toujours ramener à l'écran de connexion.

Documentation : [services](https://angular.dev/guide/di/creating-and-using-services), [signals](https://angular.dev/guide/signals)

### 3. Charger la session au démarrage

Quand l'utilisateur recharge la page, le cookie est toujours là, mais le signal est vide. Il faut interroger l'API **avant** que le routeur n'évalue les gardes. Dans `src/app/app.config.ts` :

```ts
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { erreursInterceptor } from './commun/erreurs-interceptor';
import { Session } from './compte/session';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([erreursInterceptor])),
    provideAppInitializer(() => inject(Session).charger()),
  ],
};
```

`provideAppInitializer` enregistre une fonction exécutée pendant le démarrage. Comme elle renvoie une promesse, Angular **attend** sa résolution avant d'afficher quoi que ce soit : au moment de la première navigation, `Session` sait déjà si quelqu'un est connecté.

Ouvrez l'onglet *Réseau* des outils du navigateur et rechargez : `GET /api/compte/moi` part avant tout autre appel.

Si l'API est arrêtée, `charger()` reçoit une erreur, la traite comme « personne n'est connecté », et l'application démarre sur l'écran de connexion (constaté, requête avortée). En revanche, une réponse qui n'arrive **jamais** laisse la page blanche aussi longtemps : Angular attend la promesse, et `HttpClient` n'a pas de délai d'expiration par défaut. Ce cas, plus rare, n'est pas traité ici.

Documentation : [`provideAppInitializer`](https://angular.dev/api/core/provideAppInitializer)

### 4. L'écran de connexion

```powershell
npx ng generate component compte/connexion
```

La CLI crée le dossier `compte/connexion/`. `connexion.ts` :

```ts
import { Component, inject, input, signal } from '@angular/core';
import { email, form, FormField, FormRoot, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { ErreurApi } from '../../commun/erreur-api';
import { Session } from '../session';

@Component({
  selector: 'app-connexion',
  imports: [FormField, FormRoot, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './connexion.html',
})
export class Connexion {
  // Sans « ?retour= » dans l'adresse, le routeur passe undefined : la transformation rétablit '/'.
  readonly retour = input('/', { transform: (valeur: string | undefined) => valeur ?? '/' });

  private readonly session = inject(Session);
  private readonly router = inject(Router);

  protected readonly echec = signal<string | null>(null);
  protected readonly modele = signal({ email: '', password: '' });

  protected readonly formulaire = form(
    this.modele,
    (champs) => {
      required(champs.email, { message: "L'adresse électronique est obligatoire." });
      email(champs.email, { message: "L'adresse électronique est invalide." });
      required(champs.password, { message: 'Le mot de passe est obligatoire.' });
    },
    {
      submission: {
        action: async (champs) => {
          this.echec.set(null);
          const { email, password } = champs().value();
          try {
            await this.session.connecter(email, password);
          } catch (erreur) {
            // API injoignable : le dire, plutôt que de laisser croire à un mot de passe erroné.
            const injoignable = erreur instanceof ErreurApi && erreur.statut === 0;
            this.echec.set(
              injoignable ? erreur.message : 'Adresse électronique ou mot de passe incorrect.',
            );
            return;
          }
          await this.router.navigateByUrl(this.destination());
        },
      },
    },
  );

  private destination(): string {
    const retour = this.retour();
    return retour.startsWith('/') && !retour.startsWith('//') ? retour : '/';
  }
}
```

`connexion.html` :

```html
<h2>Connexion</h2>

<form [formRoot]="formulaire">
  <mat-form-field>
    <mat-label>Adresse électronique</mat-label>
    <input matInput type="email" autocomplete="username" [formField]="formulaire.email" />
    @if (formulaire.email().touched() && formulaire.email().invalid()) {
      <mat-error>{{ formulaire.email().errors()[0].message }}</mat-error>
    }
  </mat-form-field>

  <mat-form-field>
    <mat-label>Mot de passe</mat-label>
    <input matInput type="password" autocomplete="current-password" [formField]="formulaire.password" />
    @if (formulaire.password().touched() && formulaire.password().invalid()) {
      <mat-error>{{ formulaire.password().errors()[0].message }}</mat-error>
    }
  </mat-form-field>

  @if (echec(); as message) {
    <p role="alert">{{ message }}</p>
  }

  <button matButton="filled" type="submit" [disabled]="formulaire().submitting()">Se connecter</button>
</form>
```

- **`submission.action`** est appelée par `[formRoot]` à l'envoi du formulaire, et seulement s'il est valide. Pendant son exécution, `formulaire().submitting()` vaut `true` : le bouton est désactivé, ce qui empêche le double envoi.
- **Le message d'échec est volontairement vague.** « Adresse inconnue » ou « mot de passe erroné » diraient à un attaquant quelles adresses ont un compte. Seule exception : une API qui ne répond pas (`statut` 0, traduit par `erreursInterceptor` au module 16), qui n'apprend rien à personne.
- **`retour`** est un `input` : grâce à `withComponentInputBinding()`, il reçoit le paramètre `?retour=` de l'URL. On ne suit que les adresses internes à l'application, celles qui commencent par un seul `/`. `navigateByUrl` ne quitte de toute façon jamais l'application : essayé avec `?retour=https://exemple.test`, `//exemple.test` et `/\exemple.test`, on reste sur `localhost:4200` (les deux premiers mènent à l'accueil, le troisième à « Page introuvable »). Le contrôle reste utile : il documente l'intention, et protège le jour où quelqu'un remplacerait `navigateByUrl` par `window.location`.
- **`autocomplete`** indique au navigateur et aux gestionnaires de mots de passe quoi proposer dans chaque champ.

Faites pointer la route `connexion` du module 15 sur ce composant, à la place de `ConnexionProvisoire`, puis supprimez le dossier `compte/connexion-provisoire/`. La route reste hors de toute garde :

```ts
{
  path: 'connexion',
  title: 'Connexion — Chronos',
  loadComponent: () => import('./compte/connexion/connexion').then((m) => m.Connexion),
},
```

Ouvrez `http://localhost:4200/connexion` et connectez-vous avec un compte de démonstration. **Cet écran remplace l'étape 2 du module 16**, « Ouvrir une session, provisoirement » : vous n'avez plus rien à taper dans la console du navigateur. Ce provisoire-là prend fin ici.

Documentation : [soumission d'un Signal Form](https://angular.dev/guide/forms/signals/form-submission)

### 5. Exiger la connexion : `authGuard`

```powershell
npx ng generate guard compte/auth --implements CanActivate
```

La CLI crée `auth-guard.ts`, qui exporte une fonction `authGuard`. Complétez-la :

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Session } from './session';

export const authGuard: CanActivateFn = (route, state) => {
  if (inject(Session).estConnecte()) {
    return true;
  }
  return inject(Router).createUrlTree(['/connexion'], { queryParams: { retour: state.url } });
};
```

`state.url` est l'adresse que l'utilisateur voulait atteindre. Elle part dans le paramètre `retour`, que l'écran de connexion relira à l'étape 4.

Plutôt que de répéter la garde sur chaque route, regroupez les routes protégées sous une route parente **sans chemin**, dans `app.routes.ts` :

```ts
export const routes: Routes = [
  {
    path: 'connexion',
    loadComponent: () => import('./compte/connexion/connexion').then((m) => m.Connexion),
    title: 'Connexion — Chronos',
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'collaborateurs' },
      {
        path: 'collaborateurs',
        loadChildren: () =>
          import('./collaborateurs/collaborateurs.routes').then((m) => m.collaborateursRoutes),
      },
      // … puis, inchangées, vos routes des modules 15 à 18 : projets, plan-de-charge,
      // feuille-de-temps, feuilles-a-valider, absences …
      {
        path: '**',
        loadComponent: () =>
          import('./commun/page-introuvable/page-introuvable').then((m) => m.PageIntrouvable),
        title: 'Page introuvable — Chronos',
      },
    ],
  },
];
```

Ce sont vos routes du module 15, déplacées d'un cran : `connexion` dehors, tout le reste — route `**` comprise — sous la route protégée.

Essayez : sans être connecté, ouvrez `http://localhost:4200/feuille-de-temps`. L'adresse devient `/connexion?retour=%2Ffeuille-de-temps`. Connectez-vous : vous arrivez sur la feuille de temps.

> Pour ajouter des options à la redirection — ne pas laisser de trace dans l'historique, par exemple —, renvoyez `new RedirectCommand(urlTree, { replaceUrl: true })` à la place du `UrlTree`.

### 6. Réserver une route à un rôle : `canMatch`

`src/app/compte/role-guard.ts` :

```ts
import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { Session } from './session';
import { Role } from './utilisateur';

/** Pour `canMatch` : sans le rôle, la route n'existe pas, et le routeur essaie les suivantes. */
export function roleGuard(...roles: Role[]): CanMatchFn {
  return () => inject(Session).aLeRole(...roles);
}

/** Pour `canActivate` : sans le rôle, renvoie vers `repli` au lieu d'annuler la navigation. */
export function roleGuardAvecRepli(repli: string, ...roles: Role[]): CanActivateFn {
  return () => inject(Session).aLeRole(...roles) || inject(Router).parseUrl(repli);
}
```

`roleGuard` n'est pas une garde : c'est une fonction qui **fabrique** une garde à partir de ses paramètres. `roleGuardAvecRepli` sert à la fin de cette étape. Sur la route du plan de charge (module 18) :

```ts
{
  path: 'plan-de-charge',
  canMatch: [roleGuard('Manager', 'GestionnaireRh')],
  loadComponent: () => import('./plan-de-charge/plan-de-charge/plan-de-charge').then((m) => m.PlanDeCharge),
  title: 'Plan de charge — Chronos',
},
```

Voici ce qui a été observé, avec la route `**` placée comme à l'étape 5 :

| Qui ouvre `/plan-de-charge` | Ce qui se passe |
| --- | --- |
| Un manager | Le plan de charge s'affiche. |
| Un collaborateur sans autre rôle | La route est ignorée ; `**` prend le relais : « Page introuvable ». Le code du plan de charge n'est pas téléchargé. |
| Un visiteur non connecté | La route est ignorée, `**` correspond, puis `authGuard` — portée par la route parente — redirige vers `/connexion?retour=%2Fplan-de-charge`. |

Le troisième cas ne fonctionne que parce que `**` est **enfant** de la route protégée. Placée au même niveau que `connexion`, elle afficherait « Page introuvable » à un visiteur qui n'est simplement pas connecté.

Faites de même pour `feuilles-a-valider` (manager ou gestionnaire RH : RG-36), et pour `nouveau` dans `projets.routes.ts` : `canMatch: [roleGuard('Manager', 'GestionnaireRh')]`. Aucune route ne la suit dans ce fichier : sans le rôle, le routeur remonte jusqu'à `**`.

> **`canMatch` passe la main à la route suivante — quelle qu'elle soit.** Dans `collaborateurs.routes.ts`, la route `nouveau` est suivie de `:id`. Avec `canMatch`, un manager qui ouvre `/collaborateurs/nouveau` tomberait sur la fiche du collaborateur « nouveau » (constaté). Il faut donc `canActivate` sur cette route. Mais une garde `canActivate` qui renvoie `false` **annule** la navigation : depuis la liste, l'utilisateur reste sur la liste ; sur une adresse tapée ou un lien ouvert dans un nouvel onglet, il n'y a pas de page précédente, et l'on obtient un écran vide à l'adresse `/` (constaté). La documentation d'Angular le dit : pour éconduire, renvoyez un `UrlTree`. Écrivez donc :
>
> ```ts
> canActivate: [roleGuardAvecRepli('/collaborateurs', 'GestionnaireRh')],
> ```
>
> Sans le rôle, on revient à la liste des collaborateurs, d'où que l'on vienne.

### 7. Réagir aux `401` et aux `403`

Une session peut expirer pendant que l'utilisateur travaille. La garde ne le verra pas : elle ne s'exécute qu'aux changements de page. C'est la réponse de l'API qui l'apprend à l'interface.

**Le `401` : retour à la connexion.**

```powershell
npx ng generate interceptor compte/auth
```

Dans `auth-interceptor.ts` :

```ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { Session } from './session';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(Session);
  const router = inject(Router);

  return next(req).pipe(
    tap({
      error: (erreur: unknown) => {
        const sessionPerdue =
          erreur instanceof HttpErrorResponse &&
          erreur.status === 401 &&
          !req.url.startsWith('/api/compte/');
        if (sessionPerdue) {
          session.oublier();
          void router.navigate(['/connexion'], { queryParams: { retour: router.url } });
        }
      },
    }),
  );
};
```

- **`tap` observe l'erreur sans l'avaler** : le composant qui a lancé la requête la reçoit toujours, et peut remettre son écran en état.
- **Les routes `/api/compte/` sont exclues.** Un `401` sur `/api/compte/moi` ou sur `/api/compte/login` est une réponse attendue, déjà traitée par `Session` et par l'écran de connexion.
- **`session.oublier()`** aligne l'interface sur la réalité : l'API vient de dire que plus personne n'est connecté.

Dans `app.config.ts`, placez-le **après** l'intercepteur du module 16 :

```ts
provideHttpClient(withInterceptors([erreursInterceptor, authInterceptor])),
```

L'ordre compte. Une requête traverse les intercepteurs dans l'ordre de la liste ; sa réponse les retraverse en sens inverse. `authInterceptor`, dernier de la liste, voit donc la réponse en premier, sous la forme d'un `HttpErrorResponse`. Dans l'ordre inverse, `erreursInterceptor` l'aurait déjà traduite en `ErreurApi`, et le `401` passerait inaperçu (constaté par un test).

**Le `403` : un message.** `erreursInterceptor` notifie déjà toute écriture en échec. Il reste à lui donner les mots justes. Dans `src/app/commun/erreur-api.ts`, ajoutez deux lignes au tableau `LIBELLES` du module 16 :

```ts
const LIBELLES: Record<number, string> = {
  400: 'Les données envoyées sont invalides.',
  401: 'Votre session a expiré. Reconnectez-vous.',
  403: "Vous n'avez pas le droit d'effectuer cette action.",
  404: "La ressource demandée n'existe pas, ou vous n'y avez pas accès.",
  500: 'Une erreur est survenue sur le serveur.',
};
```

`versErreurApi` ne change pas : il prend le `detail` de l'API s'il y en a un, sinon ce libellé. Les réponses `401` et `403` de l'API n'ont qu'un `title` en anglais (« Forbidden ») : c'est donc le libellé français qui s'affiche (constaté).

**Une retouche à `erreursInterceptor`.** Tel quel, il afficherait aussi une notification pour une connexion refusée, que l'écran de connexion signale déjà. Ajoutez une condition à celle du module 17, pour exclure les routes du compte :

```ts
const geree = req.context.get(ERREURS_DE_SAISIE_GEREES) && erreur.estDeSaisie;
const duCompte = req.url.startsWith('/api/compte/');
if (req.method !== 'GET' && !geree && !duCompte) {
  snackBar.open(erreur.message, 'Fermer', { duration: 8000 });
}
```

Documentation : [intercepteurs](https://angular.dev/guide/http/interceptors)

### 8. CSRF : vérifier, et ne rien casser

Il n'y a pas de code à écrire ici, mais quatre points à tenir :

- **Toutes les requêtes passent par `HttpClient` ou `httpResource`, avec une URL relative** (`/api/...`). Un appel direct à `fetch()` ou une URL absolue vers une autre origine partiraient sans l'en-tête.
- **Aucune modification par `GET`.** L'en-tête n'y est pas ajouté, et l'API ne vérifie pas ces requêtes.
- **On ne désactive pas la protection** : pas de `withNoXsrfProtection()`.
- **Le jeton est lié à l'utilisateur** : il doit être renouvelé après la connexion. C'est fait, puisque `Session.connecter()` enchaîne sur `GET /api/compte/moi`. Ne « simplifiez » pas cet enchaînement.

Vérifiez dans les outils du navigateur, API démarrée :

- onglet *Application*, rubrique *Cookies* : `chronos.session` est marqué `HttpOnly` et `SameSite=Strict` ; `XSRF-TOKEN` n'est pas `HttpOnly`, et il n'apparaît qu'après l'appel à `/api/compte/moi` ;
- onglet *Réseau* : enregistrez une feuille de temps, ouvrez la requête `PUT` ; l'en-tête `X-XSRF-TOKEN` y figure, avec la valeur du cookie. Il est absent des requêtes `GET` ;
- toujours dans *Application*, supprimez le cookie `XSRF-TOKEN`, puis enregistrez de nouveau : l'API répond `400`, « Jeton anti-CSRF absent ou invalide » (dans l'onglet *Réseau* ; la notification dit « Les données envoyées sont invalides. »). Faites l'essai sur une écriture que votre compte a le droit de faire : l'API contrôle les rôles **avant** le jeton, et une écriture interdite reçoit `403`, avec ou sans en-tête (constaté).

Si un jour l'API changeait le nom du cookie ou de l'en-tête, il faudrait les déclarer avec `withXsrfConfiguration({ cookieName: '…', headerName: '…' })` dans `provideHttpClient(...)`.

Documentation : [`withXsrfConfiguration`](https://angular.dev/api/common/http/withXsrfConfiguration)

### 9. Adapter l'interface au rôle

**Le menu.** Depuis le module 15, le composant racine décrit son menu par un tableau `liens`. Donnez à chaque lien, si besoin, les rôles qui y ont droit, et ne montrez que les liens permis. Dans `src/app/app.ts` :

```ts
interface LienDeNavigation {
  chemin: string;
  libelle: string;
  icone: string;
  roles?: Role[];
}
```

```ts
protected readonly session = inject(Session);
private readonly router = inject(Router);

private readonly liens: LienDeNavigation[] = [
  { chemin: '/collaborateurs', libelle: 'Collaborateurs', icone: 'group' },
  { chemin: '/projets', libelle: 'Projets', icone: 'folder' },
  { chemin: '/plan-de-charge', libelle: 'Plan de charge', icone: 'bar_chart', roles: ['Manager', 'GestionnaireRh'] },
  { chemin: '/feuille-de-temps', libelle: 'Feuille de temps', icone: 'schedule' },
  { chemin: '/feuilles-a-valider', libelle: 'Feuilles à valider', icone: 'fact_check', roles: ['Manager', 'GestionnaireRh'] },
  { chemin: '/absences', libelle: 'Absences', icone: 'event_busy' },
];

protected readonly liensVisibles = computed(() =>
  this.liens.filter((lien) => !lien.roles || this.session.aLeRole(...lien.roles)),
);

protected async seDeconnecter(): Promise<void> {
  await this.session.deconnecter();
  await this.router.navigateByUrl('/connexion');
}
```

Gardez vos liens et vos icônes des modules 15 à 18 : seule la propriété `roles` est nouvelle. Dans `app.html`, la boucle parcourt désormais `liensVisibles()` au lieu de `liens`. `liensVisibles` est un `computed` : il lit la session à travers `aLeRole`, et se recalcule à la connexion comme à la déconnexion. Avant la connexion, la session n'a aucun rôle : seuls restent les liens sans restriction, qui mènent de toute façon à l'écran de connexion.

**L'utilisateur et la déconnexion.** Dans la barre de titre, après le `<h1>` :

```html
@if (session.utilisateur(); as utilisateur) {
  <span>{{ utilisateur.prenom ?? utilisateur.email }} {{ utilisateur.nom }}</span>
  <button matButton type="button" (click)="seDeconnecter()">Se déconnecter</button>
}
```

Un compte sans fiche n'a ni prénom ni nom : on affiche alors son adresse.

**Un élément dans un écran.** Un simple `@if (session.aLeRole('GestionnaireRh'))` suffit dans la plupart des cas. Si la même condition revient dans beaucoup de templates, une **directive structurelle** l'exprime en un mot. `src/app/compte/si-role.ts` :

```ts
import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';
import { Session } from './session';
import { Role } from './utilisateur';

@Directive({ selector: '[appSiRole]' })
export class SiRole {
  readonly appSiRole = input.required<Role | Role[]>();

  private readonly session = inject(Session);
  private readonly gabarit = inject(TemplateRef);
  private readonly conteneur = inject(ViewContainerRef);

  constructor() {
    effect(() => {
      const roles = this.appSiRole();
      const autorise = this.session.aLeRole(...(Array.isArray(roles) ? roles : [roles]));
      this.conteneur.clear();
      if (autorise) {
        this.conteneur.createEmbeddedView(this.gabarit);
      }
    });
  }
}
```

```html
<a *appSiRole="'GestionnaireRh'" matButton routerLink="/collaborateurs/nouveau">Nouveau collaborateur</a>
```

L'étoile demande à Angular de traiter l'élément comme un gabarit, que la directive affiche ou non. L'`effect` est ici à sa place : il synchronise le DOM avec un signal, ce qu'un `computed` ne sait pas faire.

Documentation : [directives structurelles](https://angular.dev/guide/directives/structural-directives)

### 10. RG-52 : ne proposer la modification qu'à qui peut la faire

Depuis le module 12, l'API réserve au gestionnaire RH la création, la modification et l'archivage d'une fiche, et répond `403` aux autres. Elle n'envoie `dateEntree`, `dateSortie` et `quotite` qu'au gestionnaire RH et à l'intéressé : c'est pourquoi ces trois propriétés sont facultatives dans le type `Collaborateur`.

L'interface suit, en quatre endroits :

- **La création.** Dans `collaborateurs.routes.ts`, la route `nouveau` reçoit `canActivate: [roleGuardAvecRepli('/collaborateurs', 'GestionnaireRh')]` (étape 6), et le lien qui y mène n'est affiché qu'à ce rôle : `*appSiRole="'GestionnaireRh'"`, comme dans l'exemple de l'étape 9.
- **La modification.** Dans le template de `FicheCollaborateur`, le bouton « Modifier », qui passe `enEdition` à `true`, n'est proposé qu'au gestionnaire RH. Le formulaire ne s'ouvre donc que pour lui.

```html
@if (session.aLeRole('GestionnaireRh')) {
  <button matButton type="button" (click)="enEdition.set(true)">Modifier</button>
}
```

- **La suppression.** Même condition sur le bouton « Supprimer » de `CarteCollaborateur`, dans la liste. Le bouton « Nouveau projet » de `ListeProjets`, lui, est réservé aux managers et aux gestionnaires RH : `*appSiRole="['Manager', 'GestionnaireRh']"`.
- **L'affichage.** Un champ que l'API n'a pas envoyé ne s'affiche pas — ni vide, ni avec un tiret : `@if (c.dateEntree) { … }`. Un manager ne doit pas lire « Date d'entrée : — » et croire qu'elle manque.

`FicheCollaborateur` injecte `Session` dans un champ `protected readonly session`, pour que son template puisse l'interroger.

Dans le formulaire lui-même, le module 17 a déjà désactivé en modification le matricule, la catégorie, la date d'entrée et la quotité : ils ne changent pas par ce formulaire, quel que soit le rôle. Si vous ouvrez un jour ce formulaire à d'autres rôles — un collaborateur qui corrigerait son adresse électronique —, la même famille de fonctions verrouille un champ **selon le rôle** :

```ts
readonly(champs.dateSortie, () => !this.session.aLeRole('GestionnaireRh'));
```

`readonly` s'importe de `@angular/forms/signals`, comme `disabled` ; `[formField]` reporte l'état sur l'élément, qui reçoit l'attribut `readonly`. La déclaration de `session` doit alors précéder celle de `formulaire` : les champs d'une classe s'initialisent dans l'ordre où ils sont écrits.

Documentation : [états d'un champ](https://angular.dev/guide/forms/signals/field-state-management)

### 11. Remplacer `COLLABORATEUR_COURANT_ID`

Le module 18 a rangé l'identifiant provisoire dans `src/app/commun/collaborateur-courant.ts`, avec un commentaire qui annonçait ce moment. Ajoutez à `Session` de quoi le remplacer :

```ts
readonly collaborateurId = computed(() => this.utilisateurCourant()?.collaborateurId ?? null);
```

Dans `FeuilleDeTempsHebdo`, la constante disparaît, avec le commentaire « PROVISOIRE » du module 18 :

```ts
/** La fiche de l'utilisateur connecté (RG-50) ; `null` pour un compte qui n'en a pas. */
protected readonly collaborateurId = inject(Session).collaborateurId;

protected readonly feuille = this.api.deLaSemaine(this.collaborateurId, this.selection.semaine);
private readonly affectations = this.api.affectations(this.collaborateurId);
```

`collaborateurId` est maintenant un signal de type `number | null` : `null` pour un compte sans fiche. `FeuillesDeTempsApi` doit l'accepter, et ne rien demander dans ce cas :

```ts
deLaSemaine(collaborateurId: () => number | null, semaine: () => string) {
  return httpResource<FeuilleDeTemps>(() => {
    const id = collaborateurId();
    return id === null
      ? undefined
      : { url: this.url, params: { collaborateurId: id, semaine: semaine() } };
  });
}
```

Quand la fonction renvoie `undefined`, `httpResource` n'envoie aucune requête. Faites de même pour `affectations` et `absences`. `collaborateurId` est `protected` : le template de l'écran s'en sert pour le dire à l'utilisateur, `@if (collaborateurId() === null)`, « Ce compte n'est rattaché à aucun collaborateur. » L'administrateur garde l'entrée « Feuille de temps » dans son menu, et y lit ce message.

Supprimez enfin `commun/collaborateur-courant.ts` : la compilation vous montrera les usages oubliés.

L'API garde le paramètre `collaborateurId` — c'est ainsi qu'un manager consulte la feuille d'un membre de son équipe —, mais elle le confronte au périmètre de l'utilisateur : pour l'identifiant d'un autre, elle répond `404`.

## À vous

1. **Compléter le menu et les routes.** Donnez à chaque route du module 15 son niveau d'accès : qui peut ouvrir la liste des collaborateurs, créer un collaborateur, gérer les jours fériés ? Écrivez d'abord le tableau « route → rôles », faites-le relire, puis appliquez-le aux routes (`canMatch` ou `canActivate`, selon ce qui les suit) **et** au tableau `liens` du menu. Que se passe-t-il si les deux divergent ?

2. **RG-52 jusqu'au bout.** Connecté comme manager, vérifiez que ni « Nouveau collaborateur », ni « Modifier », ni la suppression ne vous sont proposés, et que la fiche d'un membre de votre équipe n'affiche aucune date de contrat. Puis envoyez tout de même une modification à l'API depuis la console du navigateur, avec `fetch`. Quelle réponse obtenez-vous sans l'en-tête `X-XSRF-TOKEN` ? Et avec lui ? Qu'en concluez-vous sur ce que protège chaque couche ?

3. **RG-36 à l'écran.** Les boutons « Valider » et « Rejeter » ne doivent apparaître qu'au manager du collaborateur ou à un gestionnaire RH, et jamais sur sa propre feuille. Dans Chronos, ils ne vivent que sur l'écran `FeuillesAValider` du module 18, réservé par l'étape 6, et cet écran n'affiche que ce que renvoie `GET /api/feuilles-de-temps/soumises`. Connectez-vous comme Sophie, puis comme Hugo : quelles feuilles chacun voit-il ? De quelles informations l'interface aurait-elle besoin pour refaire ce filtre elle-même (équipe, propre fiche) ? Les a-t-elle toutes ? Pourquoi vaut-il mieux laisser l'API choisir ?

4. **Session expirée.** Connectez-vous, ouvrez la feuille de temps, puis supprimez le cookie d'authentification dans les outils du navigateur. Changez de semaine. Décrivez ce que fait l'interface, étape par étape, jusqu'au retour sur la feuille de temps après une nouvelle connexion. Une saisie non enregistrée est-elle perdue ?

5. **Introuvable ou interdit ?** Un collaborateur qui ouvre `/plan-de-charge` lit « Page introuvable ». Proposez une variante qui affiche « Accès refusé » : où placez-vous la décision, et que renvoie la garde ? Laquelle des deux réponses préférez-vous, et pourquoi ?

6. **Désigner les requêtes autrement.** Les deux intercepteurs reconnaissent les routes du compte à leur URL. Remplacez ce test par un `HttpContextToken` que `Session` pose sur ses propres requêtes, sur le modèle de `ERREURS_DE_SAISIE_GEREES` au module 17. Qu'y gagne-t-on ?

## Critères de réussite

- `npm run build` et `npm test` réussissent.
- Sans être connecté, toute adresse de l'application mène à `/connexion`, et la connexion ramène à l'adresse demandée, paramètres compris.
- Après un rechargement de la page, l'utilisateur est toujours connecté, sans que l'interface ait rien stocké : `localStorage` et `sessionStorage` sont vides.
- Des identifiants erronés affichent un message unique, qui ne dit pas lequel des deux champs est faux.
- Un collaborateur sans autre rôle ne voit pas l'entrée « Plan de charge », et l'adresse `/plan-de-charge` lui affiche la page introuvable.
- Un manager qui tape l'adresse `/collaborateurs/nouveau` arrive sur la liste des collaborateurs, jamais sur un écran vide.
- « Se déconnecter » ramène à l'écran de connexion, même quand la session avait déjà expiré.
- La suppression du cookie d'authentification, suivie d'une action, ramène à l'écran de connexion.
- Les requêtes `POST`, `PUT` et `DELETE` portent l'en-tête `X-XSRF-TOKEN` ; sans lui, l'API répond `400`.
- La déconnexion reçoit `204`, et les cookies `chronos.session` et `XSRF-TOKEN` ont disparu.
- `commun/collaborateur-courant.ts` a disparu, et plus aucun identifiant de collaborateur n'est écrit en dur dans `src/app/`.
- Comparez avec la solution de référence : `git diff module-18 module-19`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `localStorage.setItem('token', …)` puis un intercepteur qui ajoute `Authorization: Bearer …` | Le jeton est lisible par tout script de la page ; et l'API de Chronos n'en émet pas. | Cookie `HttpOnly` géré par le navigateur ; l'interface ne stocke rien. |
| Masquer un bouton et considérer l'action comme protégée | L'utilisateur peut appeler l'API sans l'interface. | L'API refuse (`403`) ; l'interface masque par confort. |
| Une garde écrite en classe, `implements CanActivate` | Forme déconseillée. | Une fonction `CanActivateFn` ou `CanMatchFn`. |
| `router.navigate(['/connexion']); return false;` dans une garde | Deux navigations concurrentes ; la documentation le proscrit. | Renvoyer un `UrlTree` ou un `RedirectCommand`. |
| Charger l'utilisateur dans le `ngOnInit` du composant racine | Les gardes s'exécutent avant la réponse : l'utilisateur connecté est renvoyé à la connexion. | `provideAppInitializer`. |
| `APP_INITIALIZER` | Déprécié. | `provideAppInitializer(() => …)`. |
| La route `**` au même niveau que `connexion` | Un visiteur non connecté lit « Page introuvable » au lieu d'être invité à se connecter. | `**` en dernière enfant de la route protégée. |
| `canMatch` sur une route suivie de `:id` | La route suivante prend le relais : `/collaborateurs/nouveau` affiche la fiche « nouveau ». | `canActivate` sur cette route, avec `roleGuardAvecRepli`. |
| Une garde `canActivate` qui renvoie `false` pour un rôle manquant | Sur une adresse tapée, la navigation initiale est annulée : écran vide à l'adresse `/`. | Renvoyer un `UrlTree` vers une page sûre. |
| Traiter `401` et `403` de la même façon | Renvoyer à la connexion quelqu'un qui est déjà connecté ne lui apprend rien. | `401` : connexion. `403` : message. |
| `authInterceptor` placé avant `erreursInterceptor` dans la liste | Il reçoit l'erreur déjà traduite en `ErreurApi` et ne reconnaît plus le `401`. | L'intercepteur d'authentification en dernier. |
| `deconnecter()` qui laisse remonter l'erreur du `logout` | Session déjà expirée : l'API répond `401`, l'utilisateur reste affiché comme connecté. | Oublier l'utilisateur dans tous les cas. |
| `catchError(() => EMPTY)` dans l'intercepteur | Le composant n'apprend jamais l'échec ; son indicateur de chargement tourne sans fin. | `tap({ error })`, qui laisse passer l'erreur. |
| « Adresse électronique inconnue » | Révèle quelles adresses ont un compte. | Un message unique pour tout échec de connexion. |
| Suivre `?retour=` sans le contrôler | Le paramètre vient de l'URL, donc de n'importe qui. | N'accepter qu'un chemin interne. |
| Appeler l'API avec `fetch()` | La requête échappe aux intercepteurs, dont celui qui pose `X-XSRF-TOKEN`. | `HttpClient` ou `httpResource`. |
| `withNoXsrfProtection()` « parce que ça marche sans » | Tant que l'API ne vérifie pas le jeton, tout marche ; le jour où elle le fera, plus rien. | Laisser la protection par défaut. |
| `HttpClientXsrfModule` | Appartient aux `NgModule`. | Rien, ou `withXsrfConfiguration(...)`. |

La liste complète des pratiques obsolètes est dans le [référentiel Angular 22](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Qu'est-ce qu'un attaquant peut faire s'il exécute un script dans la page de Chronos ? Qu'est-ce que le cookie `HttpOnly` l'empêche de faire, et que ne l'empêche-t-il pas de faire ?
- Le cookie `XSRF-TOKEN` est lisible par JavaScript : pourquoi n'est-ce pas une faille ?
- L'interface connaît les rôles de l'utilisateur. Que se passe-t-il si un administrateur lui retire un rôle pendant qu'il est connecté ? Qui s'en aperçoit en premier, l'interface ou l'API ?
- `canMatch` répond « introuvable » là où `canActivate` répondrait « interdit ». Laquelle de ces réponses donne le moins d'informations à un curieux ? Laquelle aide le plus un utilisateur de bonne foi ?
- Les mêmes règles d'accès sont écrites deux fois : dans l'API et dans l'interface. Comment éviter qu'elles divergent ?
- Avant de se connecter, un visiteur qui ouvre `/collaborateurs` télécharge-t-il le code de la liste ? Vérifiez dans l'onglet *Réseau*. Est-ce un problème ? Qu'est-ce qui, dans Chronos, doit rester secret : le code ou les données ?
- Pourquoi le service `Session` ne propose-t-il pas de méthode `definirUtilisateur(...)` publique ?

## Pour aller plus loin

- [Sécurité d'une application Angular](https://angular.dev/best-practices/security) : XSS, politique de sécurité du contenu, et la section XSRF en entier.
- [`HttpContextToken`](https://angular.dev/guide/http/interceptors) : transmettre une information d'une requête à un intercepteur.
- [Prévenir les attaques CSRF dans ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0) : le côté serveur du mécanisme, et le rôle de l'attribut `SameSite` des cookies.
- Suite dans le cours : le [module 20](20-tester-l-interface.md) teste la session, les gardes et l'intercepteur ; le [module 21](21-accessibilite-et-finitions.md) rend l'écran de connexion utilisable au lecteur d'écran.
