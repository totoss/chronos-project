# Module 20 — Tester l'interface

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md) et le [référentiel GitHub Actions](../docs/references/github-actions.md).
> Ce module a été **rejoué le 12 novembre 2026 sur la solution de référence**, à la suite des modules 13 à 19, dans un conteneur Node.js 24.22.3 (npm 11.19.1) : Angular 22.2.6, Angular Material 22.2.7, Vitest 5.1.0, jsdom 30.1.3, `@vitest/coverage-v8` 5.1.0, `playwright-ng-schematics` 22.1.4, Playwright 1.65.0 (Chromium). Au début du module, la solution comptait déjà **215 tests**, écrits au fil des modules 13 à 19 ; à la fin, 249 (37 fichiers, environ 4 s), couverture 96 % des instructions, et trois parcours de bout en bout. Les extraits ci-dessous sont ceux de la solution. Les commandes du job `web` ont été enchaînées localement (`npm ci`, `ng test` avec `CI=true`, couverture et rapport JUnit, `ng build`, `playwright install`, `ng e2e`), `actionlint` ne signale rien et `gh actions-lock --verify` non plus ; **le workflow n'a pas été exécuté sur GitHub**, et la lecture de `devEngines` par `actions/setup-node` est documentée, non exécutée. Les réponses de l'API simulée ont été comparées, champ par champ, à celles de la véritable API du module 12.

L'interface compte maintenant une dizaine d'écrans, une session, des gardes. Chaque modification peut en casser un autre sans que vous le voyiez : personne ne reclique partout après chaque commit. Vous n'êtes pas parti de rien : depuis le module 13, chaque écran est venu avec ses tests — plus de deux cents dans la solution de référence. Mais vous les avez écrits au fil de l'eau, chacun sur le modèle du précédent. Ce module prend le temps de la **méthode** : ce que chaque outil vérifie, les pièges de l'application sans `zone.js`, comment savoir qu'un test prouve quelque chose. Il **comble les manques** — les fichiers `.spec.ts` de `compte/`, laissés vides au module 19, et quelques cas jamais vérifiés —, puis ajoute ce qui n'existe pas encore : les tests de bout en bout dans un vrai navigateur, et un job `web` dans le workflow du [module 05](05-github-actions-integration-continue.md), qui les lancera à chaque *push*.

## Objectifs

À la fin de ce module, vous saurez :

- lancer les tests de l'interface, en continu sur votre poste et en une passe pour l'intégration continue ;
- faire l'inventaire de vos tests, et mettre un test à l'épreuve en cassant le code qu'il protège ;
- tester du code à signals sans Angular, puis un service et un composant avec `TestBed` ;
- simuler l'API avec `HttpTestingController`, y compris derrière `httpResource` ;
- piloter un écran Angular Material par ses harnais ;
- tester une garde, un intercepteur et une navigation ;
- écrire un parcours de bout en bout avec Playwright ;
- ajouter un job `web` au workflow d'intégration continue.

## Prérequis

- [Module 19](19-authentification-cote-interface.md) : `Session`, `Connexion`, `authGuard`, `roleGuard`, `roleGuardAvecRepli`, `authInterceptor`, `SiRole`, et les fichiers `.spec.ts` de `compte/` que la CLI a créés et que vous avez gardés.
- Modules 14 à 19 : les tests de vos écrans (`liste-collaborateurs.spec.ts`, `formulaire-collaborateur.spec.ts`, `indicateur-charge.spec.ts`, `grille-de-saisie.spec.ts`…), de vos services d'API et des fonctions de `dates.ts` et `feuille-de-temps.ts` ; `app.routes.spec.ts`, qui navigue dans les vraies routes.
- [Module 05](05-github-actions-integration-continue.md) : le workflow `ci.yml`, son fichier `actions.lock`.
- Avoir déjà écrit des tests : c'était l'objet du [module 04](04-modeliser-le-domaine.md), en C#.

## Règles de gestion

Aucune nouvelle. Les tests de ce module vérifient que l'interface reflète correctement des règles déjà mises en œuvre : RG-24 (surcharge), RG-31, RG-32 et RG-35 (feuille de temps), RG-50 à RG-52 (accès).

## Notions

### Ce que `ng new` a mis en place

Vous n'avez rien à installer pour les tests unitaires. Voici ce que contient un projet créé par la CLI 22 :

| Élément | Constat |
| --- | --- |
| Lanceur | Vitest (`vitest` dans `devDependencies`). Ni Karma ni Jasmine. |
| Environnement | `jsdom` : une imitation du navigateur, en mémoire, dans Node.js. Aucun navigateur n'est lancé. |
| `angular.json` | Une cible `test`, de *builder* `@angular/build:unit-test`, sans option. |
| `tsconfig.spec.json` | Déclare `vitest/globals` : `describe`, `it`, `expect` et `vi` s'utilisent sans import. |
| Fichier de configuration Vitest | Aucun. La CLI le fabrique à la volée. |
| Fichiers de test | `*.spec.ts`, à côté du code testé. |

jsdom ne dessine rien : pas de mise en page, pas de couleurs, pas de défilement. Il suffit pour vérifier ce qu'un composant affiche et comment il réagit. Pour tout ce qui dépend d'un vrai navigateur, il y a les tests de bout en bout.

Documentation : [tester une application Angular](https://angular.dev/guide/testing)

### Trois sortes de tests, par coût croissant

| Sorte | Ce qu'elle exécute | Ce qu'elle attrape |
| --- | --- | --- |
| Test de logique | Une fonction, une classe, sans Angular. | Une erreur de calcul. |
| Test de composant ou de service | Angular, dans jsdom, avec une API simulée. | Un affichage faux, une requête mal formée, une garde qui laisse passer. |
| Test de bout en bout | L'application entière, dans un navigateur. | Deux pièces justes séparément qui ne fonctionnent pas ensemble. |

On écrit beaucoup des premiers, peu des derniers : un test de bout en bout est lent, et quand il échoue, il ne dit pas où.

### Sans `zone.js` : attendre que l'application soit stable

L'application est *zoneless* : Angular met l'affichage à jour quand un signal change, de manière asynchrone. Un test ne peut donc pas lire le DOM juste après avoir modifié un état. Il attend :

```ts
await fixture.whenStable();
```

`whenStable()` rend la main quand Angular n'a plus rien en attente : affichage à jour, requêtes terminées. C'est la forme que recommande la documentation, de préférence à `fixture.detectChanges()`. Les tests sont donc des fonctions `async`.

`whenStable()` n'attend que ce qu'Angular connaît. Deux cas, que vous avez déjà rencontrés, lui échappent :

- **une requête à laquelle le test n'a pas encore répondu** : l'application n'est pas stable, et la promesse ne se résout qu'après la réponse. On répond d'abord (`flush`), on attend ensuite ;
- **l'action de soumission d'un formulaire à signaux** (`submission.action`) : c'est une fonction `async` ordinaire. Constaté : avec un service qui répond en 50 ms, la navigation qui suit la connexion n'a pas encore eu lieu quand `whenStable()` rend la main. On attend alors ce que l'utilisateur verrait — le bouton redevenu actif — avec **`vi.waitFor`**, qui réessaie une vérification jusqu'à ce qu'elle passe (une seconde au plus, par défaut).

`vi.waitFor` n'est pas une pause : il s'arrête dès que la condition est vraie. On ne l'emploie que là où `whenStable()` ne suffit pas ; partout ailleurs, il masquerait un oubli.

`fakeAsync` et `tick()`, qu'on voit dans presque tous les tutoriels, reposent sur `zone.js`, qui n'est plus installé. Pour maîtriser le temps, on utilise l'horloge simulée de Vitest.

Documentation : [scénarios de test de composants](https://angular.dev/guide/testing/components-scenarios)

### Doublures

Un test remplace ce qui entoure le code testé par des **doublures** :

- `vi.fn()` crée une fonction factice, qui enregistre ses appels et renvoie ce qu'on lui dit ;
- `vi.spyOn(objet, 'methode')` espionne une méthode existante, et peut la remplacer ;
- `HttpTestingController` remplace le réseau : le test voit passer chaque requête et décide de la réponse.

Documentation : [API `vi` de Vitest](https://vitest.dev/api/vi), [tester les requêtes HTTP](https://angular.dev/guide/http/testing)

### Harnais de composants

Le bouton « Se connecter » d'Angular Material n'est pas un simple `<button>` : c'est une structure d'éléments et de classes CSS, qui change d'une version à l'autre. Un test qui écrit `querySelector('.mdc-button__label')` cassera à la prochaine montée de version.

Un **harnais** est une classe fournie par la bibliothèque pour piloter son composant comme le ferait un utilisateur : `MatButtonHarness.click()`, `MatInputHarness.setValue(...)`. Le test ne dépend plus de la structure interne. Chaque méthode d'un harnais attend d'elle-même la mise à jour de l'affichage.

Documentation : [harnais de composants](https://angular.dev/guide/testing/component-harnesses-overview), [utiliser les harnais dans les tests](https://angular.dev/guide/testing/using-component-harnesses)

### De bout en bout : contre quelle API ?

| | API réelle et base de test | Réponses simulées dans le navigateur |
| --- | --- | --- |
| Ce qui est vérifié | Toute la chaîne, base comprise. | L'interface, et le contrat qu'elle suppose. |
| Mise en place | SDK .NET, base en conteneur, jeu de données, comptes de test. | Rien d'autre que le navigateur. |
| Durée, stabilité | Plus lent ; sensible à l'état de la base. | Rapide ; même résultat à chaque fois. |
| Angle mort | Aucun, en principe. | Si l'API change, le test reste vert alors que l'application est cassée. |

**Ce module prend les réponses simulées** : le job `web` reste indépendant du job `api`, et le parcours tient en une page. L'angle mort se réduit en trois gestes, que l'étape 9 met en place :

1. **Les réponses simulées sont typées avec les types de l'application** (`Utilisateur`, `FeuilleDeTemps`, `Page<Projet>`…), qui sont le miroir des DTO de l'API depuis le module 16. Une simulation qui ne correspond plus aux types ne compile plus — à condition de compiler : Playwright exécute les fichiers TypeScript **sans vérifier les types** (constaté), d'où une commande `tsc` à part, dans le workflow.
2. **Tout appel non simulé échoue**, et le parcours vérifie qu'il n'y en a eu aucun : une requête nouvelle de l'interface ne passe pas inaperçue.
3. **Quand l'API change, on met à jour les types de l'interface**, en relisant le document OpenAPI comme au module 16 ; la compilation désigne alors les simulations à corriger.

Reste ce qu'aucun de ces gestes n'attrape : une API qui change sans que personne ne touche aux types. Les tests d'intégration du [module 11](11-tests-d-integration.md) protègent le contrat côté API. Le parcours contre l'application complète deviendra simple au [module 23](23-conteneuriser-l-application.md), quand une commande Compose la démarrera en entier.

## Pas à pas

Les commandes s'exécutent dans `src/web`.

### 1. Lancer les tests

```powershell
npm test
```

Vous connaissez cette commande depuis le module 13 : elle lance `ng test`, qui reste actif et relance les tests concernés à chaque fichier enregistré. C'est le mode de travail normal — gardez ce terminal ouvert à côté de l'éditeur.

Pour passer une option, appelez `ng` directement :

```powershell
npx ng test --no-watch
```

Exécute les tests une fois et rend la main, avec un code de sortie non nul si un test échoue. Quand la variable d'environnement `CI` est définie — c'est le cas sur GitHub Actions —, `ng test` se comporte ainsi sans option.

```powershell
npx ng test --filter "Session"
```

Ne lance que les tests dont le nom correspond. `--include src/app/compte/session.spec.ts` restreint à un fichier ; `--list-tests` affiche les fichiers trouvés, sans rien exécuter.

Documentation : [`ng test`](https://angular.dev/cli/test)

**Faites l'inventaire.** Lancez `npx ng test --no-watch` et lisez la liste : un fichier par écran, par service, par fonction. Repérez ceux qui ne contiennent que le test généré par la CLI (`should create`, `should be created`) : il prouve seulement que la classe se construit. Dans la solution de référence, au début de ce module, voici où en sont les sujets des étapes et des exercices :

| Sujet | Déjà testé, et où | Ce que ce module fait écrire |
| --- | --- | --- |
| Fonctions de dates et de feuille de temps | `dates.spec.ts`, `feuille-de-temps.spec.ts` (module 18) | Rien. |
| `SelectionSemaine` (étape 2) | Test généré seulement. | Le fichier entier. |
| `Session` (étape 3) | Test généré, et la déconnexion (module 19). | Chargement, connexion, refus. |
| `IndicateurCharge` (étape 4) | `indicateur-charge.spec.ts`, complet (module 18). | Rien : on le relit. |
| `ListeCollaborateurs` (étape 5) | `liste-collaborateurs.spec.ts` (modules 14 à 19) : requêtes, recherche, tri, erreurs, suppression. | L'état de chargement. |
| `Connexion` (étape 6) | Test généré seulement. | Le fichier entier, avec les harnais. |
| `authGuard`, `roleGuard` (étape 7) | Sur les vraies routes : `app.routes.spec.ts` (modules 15 et 19). | Chaque garde, isolée. |
| Intercepteurs (exercice 2) | `erreurs-interceptor.spec.ts` (modules 16 et 17) ; `authInterceptor` : test généré. | Les deux, ensemble. |
| `SiRole` (exercice 3) | Aucun fichier de test. | Le fichier entier. |
| `FormulaireCollaborateur`, `GrilleDeSaisie` (exercices 1 et 4) | Complets (modules 17 et 18). | Rien : on les met à l'épreuve. |
| Bout en bout, intégration continue (étapes 9 et 10) | Rien. | Tout. |

Votre propre inventaire sera un peu différent : c'est le vôtre qui compte. Ne réécrivez pas sous un autre nom un test qui existe déjà ; complétez-le s'il lui manque un cas.

### 2. Tester de la logique, sans Angular

Vous l'avez déjà fait au module 18, pour les fonctions de `commun/dates.ts` et de `feuilles-de-temps/feuille-de-temps.ts` : une valeur en entrée, une valeur attendue en sortie, aucun outillage. Un service à signals qui n'injecte rien se teste de la même façon. `SelectionSemaine`, du même module, n'a encore que le test généré par la CLI ; dans `src/app/commun/selection-semaine.spec.ts`, remplacez-le :

```ts
import { SelectionSemaine } from './selection-semaine';

describe('SelectionSemaine', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 4, 6, 10, 30));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("démarre sur le lundi de la semaine d'aujourd'hui", () => {
    const selection = new SelectionSemaine();

    expect(selection.semaine()).toBe('2026-05-04');
    expect(selection.jours()).toHaveLength(7);
    expect(selection.jours()[6]).toBe('2026-05-10');
  });

  it('recalcule les jours quand on avance', () => {
    const selection = new SelectionSemaine();

    selection.decaler(1);

    expect(selection.semaine()).toBe('2026-05-11');
    expect(selection.jours()[0]).toBe('2026-05-11');
  });

  it('se cale sur le lundi de la date demandée', () => {
    const selection = new SelectionSemaine();

    selection.allerA('2026-05-13');

    expect(selection.semaine()).toBe('2026-05-11');
  });
});
```

- **Pas de `TestBed`.** Un signal et un `computed` sont des fonctions ordinaires. Le service n'appelle pas `inject()` : on peut le créer avec `new`.
- **`jours` est un `computed`** : le deuxième test vérifie qu'il suit `semaine` sans qu'on le lui demande.
- **`vi.useFakeTimers()` puis `vi.setSystemTime(...)`** figent l'horloge : pour le code testé — ici la fonction `aujourdhui()` —, `new Date()` renvoie le mercredi 6 mai 2026. Sans cela, le test donnerait un résultat différent chaque semaine. `vi.useRealTimers()` rend l'horloge aux tests suivants.
- **En JavaScript, les mois commencent à 0** : `new Date(2026, 4, 6)` est le 6 mai. C'est l'une des raisons pour lesquelles le module 18 a enfermé `Date` dans un seul fichier.

Ajoutez les cas limites, là où les erreurs de dates se cachent : reculer d'une semaine (`decaler(-1)` donne `2026-04-27`), passer d'une année à l'autre (de la semaine du 30 décembre 2026 à celle du 4 janvier 2027), et un dimanche (`allerA('2026-05-17')` revient au lundi 11). Le dimanche n'est pas un cas d'école : en JavaScript, `getUTCDay()` y vaut `0`, et un calcul naïf du lundi part dans le mauvais sens. Essayez : remplacez `(jour + 6) % 7` par `jour - 1` dans `lundiDe` (`dates.ts`) ; dans ce fichier de test, seul ce dernier cas échoue. Remettez le code d'origine.

Documentation : [horloge simulée de Vitest](https://vitest.dev/guide/mocking/timers)

### 3. Tester un service qui appelle l'API

`src/app/compte/session.spec.ts` contient le test généré par la CLI et, depuis le module 19, celui de la déconnexion (`it.each`, « se déconnecte quand même quand… »). Gardez ce dernier, remplacez le test généré, et complétez le fichier :

```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Session } from './session';
import { Utilisateur } from './utilisateur';

const camille: Utilisateur = {
  collaborateurId: 2,
  nom: 'Durand',
  prenom: 'Camille',
  email: 'camille.durand@chronos.example',
  roles: ['Collaborateur', 'Manager'],
};

describe('Session', () => {
  let session: Session;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    session = TestBed.inject(Session);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it("charge l'utilisateur courant", async () => {
    const chargement = session.charger();
    http.expectOne('/api/compte/moi').flush(camille);
    await chargement;

    expect(session.estConnecte()).toBe(true);
    expect(session.collaborateurId()).toBe(2);
    expect(session.aLeRole('Manager')).toBe(true);
    expect(session.aLeRole('GestionnaireRh', 'Administrateur')).toBe(false);
  });

  it('se connecte par cookie puis charge le profil', async () => {
    const connexion = session.connecter('camille.durand@chronos.example', 'secret');

    const login = http.expectOne((req) => req.url === '/api/compte/login');
    expect(login.request.method).toBe('POST');
    expect(login.request.params.get('useCookies')).toBe('true');
    login.flush(null);

    const profil = await vi.waitFor(() => http.expectOne('/api/compte/moi'));
    profil.flush(camille);
    await connexion;

    expect(session.estConnecte()).toBe(true);
  });
});
```

- **`TestBed.configureTestingModule`** prépare un injecteur neuf pour chaque test. `provideHttpClientTesting()` remplace le réseau ; il s'écrit **après** `provideHttpClient()`.
- **L'ordre des lignes compte** : on lance l'appel sans l'attendre, on répond à la requête avec `flush`, puis seulement on attend le résultat.
- **`expectOne(url)`** échoue s'il n'y a pas exactement une requête en attente pour cette URL. L'URL comprend les paramètres ; pour le `login`, on passe donc une fonction qui ne compare que le chemin.
- **`http.verify()`** échoue s'il reste une requête à laquelle le test n'a pas répondu : aucun appel inattendu ne passe inaperçu.
- **`vi.waitFor`** réessaie jusqu'à ce que la fonction n'échoue plus. La requête du profil ne part qu'après la réponse du `login` : il faut lui laisser le temps d'apparaître.
- **Une réponse d'erreur** se simule avec `flush(null, { status: 401, statusText: 'Unauthorized' })`. Écrivez sur ce modèle le test « reste déconnectée quand l'API répond 401, sans lever d'erreur ».

Complétez avec ce qui fait la particularité de `Session` : l'état initial (personne de connecté, aucun rôle) ; un compte sans fiche, comme `admin@chronos.example`, dont `collaborateurId` vaut `null` ; `oublier()`. Et le refus d'une connexion, qui doit arriver jusqu'à l'écran :

```ts
it('laisse passer le refus d’une connexion, et reste déconnectée', async () => {
  const connexion = session.connecter('camille.durand@chronos.example', 'faux');
  const refus = expect(connexion).rejects.toMatchObject({ status: 401 });

  http
    .expectOne((req) => req.url === '/api/compte/login')
    .flush(null, { status: 401, statusText: 'Unauthorized' });
  await refus;

  http.expectNone('/api/compte/moi');
  expect(session.estConnecte()).toBe(false);
});
```

- **`expect(promesse).rejects`** vérifie qu'une promesse échoue. On le prépare **avant** de répondre, sans l'attendre : sinon le rejet arriverait alors que personne ne l'écoute encore.
- **`http.expectNone`** prouve que le profil n'a pas été demandé après l'échec.
- Le test reçoit un `HttpErrorResponse` (`status`), et non une `ErreurApi` : `erreursInterceptor` n'est pas installé ici. C'est le choix de ce fichier — tester `Session` seule.

### 4. Relire un test de composant

`IndicateurCharge`, du module 18, est un composant de présentation : deux entrées, `charge` et `confirmee` ; une sortie, `confirmer`. Il n'appelle pas l'API. Vous l'avez testé au module 18 : ce test est complet, n'en écrivez pas un autre. Relisez plutôt `src/app/plan-de-charge/indicateur-charge/indicateur-charge.spec.ts` avec un regard neuf ; en voici l'essentiel :

```ts
const normale: Charge = {
  debut: '2026-05-04',
  fin: '2026-05-10',
  capacite: 4,
  chargePlanifiee: 4,
  tauxDeCharge: 1,
  surcharge: false,
  intercontrat: false,
};
const surcharge: Charge = {
  ...normale,
  chargePlanifiee: 5.33,
  tauxDeCharge: 4 / 3,
  surcharge: true,
};

describe('IndicateurCharge', () => {
  let fixture: ComponentFixture<IndicateurCharge>;
  let ecran: HTMLElement;

  async function afficher(charge: Charge, confirmee = false): Promise<void> {
    fixture.componentRef.setInput('charge', charge);
    fixture.componentRef.setInput('confirmee', confirmee);
    await fixture.whenStable();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [IndicateurCharge] });
    fixture = TestBed.createComponent(IndicateurCharge);
    ecran = fixture.nativeElement;
  });

  it('écrit « Surcharge » en toutes lettres, en plus de la couleur (RG-24)', async () => {
    await afficher(surcharge);

    expect(ecran.querySelector('.taux')?.textContent).toContain('133');
    expect(ecran.textContent).toContain('Surcharge');
    expect(ecran.classList.contains('surcharge')).toBe(true);
  });

  it('propose de confirmer une surcharge, et le dit au parent', async () => {
    const confirmer = vi.fn();
    fixture.componentInstance.confirmer.subscribe(confirmer);
    await afficher(surcharge);

    ecran.querySelector('button')!.click();

    expect(confirmer).toHaveBeenCalledTimes(1);
  });
});
```

- **C'est la forme des tests que la CLI génère**, et que vous réparez depuis le module 14 : erreur `NG0950` quand une entrée obligatoire n'a pas reçu de valeur (`setInput`), `NG0201` quand le composant a besoin du routeur (`provideRouter([])`).
- **Un composant autonome se place dans `imports`**, pas dans `declarations` : il n'y a plus de module à déclarer.
- **`fixture`** enveloppe le composant créé ; `fixture.nativeElement` est son élément dans le DOM.
- **`fixture.componentRef.setInput('charge', …)`** donne une valeur à un `input()`, comme le ferait un composant parent. On ne peut pas écrire `component.charge = …` : un `input` est en lecture seule.
- **`await fixture.whenStable()`** : l'affichage n'est à jour qu'après.
- **Une sortie s'écoute avec `subscribe`**, et une fonction factice `vi.fn()` enregistre l'émission.
- **On vérifie ce que voit l'utilisateur** — un texte, une classe, un bouton —, pas les champs `niveau` ou `libelle`, qui sont d'ailleurs `protected`.
- **Une petite fonction `afficher`** regroupe ce que chaque test répète. Elle reste dans le fichier de test, lisible d'un coup d'œil.

**Mettez ce test à l'épreuve.** Un test qui passe ne prouve rien tant qu'on ne l'a pas vu échouer. Dans le gabarit d'`IndicateurCharge`, remplacez `@if (confirmee())` par `@if (false)` : quel test échoue, avec quel message ? Remettez le code. Faites de même avec une autre ligne du gabarit, en prédisant chaque fois le test qui doit tomber. Si aucun ne tombe, il manque un test.

### 5. Tester un écran qui lit l'API

`ListeCollaborateurs` lit ses données avec `httpResource`, à travers `CollaborateursApi`. Son fichier de test, `liste-collaborateurs.spec.ts`, s'est étoffé à chaque module : paramètres de la requête, recherche dans l'adresse, tri, erreur et bouton « Réessayer », suppression, droits (RG-52). Relisez son `beforeEach` : l'écran n'y est pas créé seul, mais ouvert par les **vraies routes** de l'application, dans un `RouterTestingHarness`, avec `HttpTestingController` à la place du réseau et une doublure de `Session`. Les paramètres d'adresse (`?recherche=`) arrivent ainsi comme dans l'application.

Un état n'y est jamais vérifié : ce que l'écran montre **pendant** le chargement. Ajoutez ce test :

```ts
it('affiche la barre de progression pendant le chargement, puis les collaborateurs', async () => {
  await harness.navigateByUrl('/collaborateurs', ListeCollaborateurs);
  ecran = harness.routeNativeElement as HTMLElement;
  TestBed.tick();

  expect(ecran.querySelector('mat-progress-bar')).not.toBeNull();
  expect(cartes()).toBe(0);

  requeteListe().flush(page([collaborateur(2, 'Durand')]));
  await harness.fixture.whenStable();

  expect(ecran.querySelector('mat-progress-bar')).toBeNull();
  expect(ecran.textContent).toContain('Durand');
});
```

`harness`, `requeteListe()`, `cartes()`, `page()` et `collaborateur()` existent déjà dans le fichier : réutilisez ce qu'il offre plutôt que de recommencer à côté.

Ici, on ne peut pas commencer par `await fixture.whenStable()` : une requête est en attente, l'application n'est pas stable, et la promesse ne se résout qu'après la réponse — que le test n'a pas encore donnée. **`TestBed.tick()`** fait faire un tour à Angular : la ressource émet sa requête, l'écran affiche sa barre de progression. Le test répond, puis attend la stabilité.

La recherche de cette liste est temporisée (`debounceTime`, module 16). Le test existant, « écrit la recherche dans l'adresse après une pause… », attend la requête avec `vi.waitFor(…, { timeout: 1000 })` : les 300 ms s'écoulent pour de bon. L'horloge simulée de l'étape 2 (`vi.advanceTimersByTimeAsync(300)`) est l'autre façon de faire ; elle n'a pas été employée dans la solution.

Documentation : [tester un `httpResource`](https://angular.dev/guide/http/http-resource)

### 6. Piloter un écran Material par ses harnais

`src/app/compte/connexion/connexion.spec.ts` ne contient que le test généré. Remplacez-le :

```ts
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MatFormFieldHarness } from '@angular/material/form-field/testing';
import { MatInputHarness } from '@angular/material/input/testing';
import { provideRouter, Router } from '@angular/router';
import { ErreurApi } from '../../commun/erreur-api';
import { Session } from '../session';
import { Connexion } from './connexion';

describe('Connexion', () => {
  let fixture: ComponentFixture<Connexion>;
  let loader: HarnessLoader;
  let navigation: ReturnType<typeof vi.spyOn>;
  const connecter = vi.fn<Session['connecter']>();

  beforeEach(() => {
    connecter.mockReset();
    TestBed.configureTestingModule({
      imports: [Connexion],
      providers: [provideRouter([]), { provide: Session, useValue: { connecter } }],
    });
    navigation = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture = TestBed.createComponent(Connexion);
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  async function saisir(email: string, password: string): Promise<void> {
    const champEmail = await loader.getHarness(MatInputHarness.with({ selector: '[type=email]' }));
    const champMotDePasse = await loader.getHarness(
      MatInputHarness.with({ selector: '[type=password]' }),
    );
    await champEmail.setValue(email);
    await champMotDePasse.setValue(password);
  }

  /** La soumission d'un formulaire à signaux n'est pas suivie par `whenStable` : on attend sa fin visible. */
  async function valider(): Promise<void> {
    const bouton = await loader.getHarness(MatButtonHarness.with({ text: 'Se connecter' }));
    await bouton.click();
    await vi.waitFor(async () => expect(await bouton.isDisabled()).toBe(false));
  }

  it('connecte puis retourne à la page demandée', async () => {
    connecter.mockResolvedValue();
    fixture.componentRef.setInput('retour', '/feuille-de-temps?semaine=2026-05-04');

    await saisir('camille.durand@chronos.example', 'secret');
    await valider();

    expect(connecter).toHaveBeenCalledWith('camille.durand@chronos.example', 'secret');
    expect(navigation).toHaveBeenCalledWith('/feuille-de-temps?semaine=2026-05-04');
  });

  it("n'appelle pas l'API quand le formulaire est invalide", async () => {
    await saisir('pas-une-adresse', '');
    await valider();

    expect(connecter).not.toHaveBeenCalled();
    const champs = await loader.getAllHarnesses(MatFormFieldHarness);
    expect(await champs[0].getTextErrors()).toEqual(["L'adresse électronique est invalide."]);
    expect(await champs[1].getTextErrors()).toEqual(['Le mot de passe est obligatoire.']);
  });

  it('refuse une connexion par un message générique, sans naviguer', async () => {
    connecter.mockRejectedValue(new ErreurApi(401, 'Non autorisé.'));

    await saisir('camille.durand@chronos.example', 'mauvais');
    await valider();

    const alerte = (fixture.nativeElement as HTMLElement).querySelector('[role=alert]');
    expect(alerte?.textContent?.trim()).toBe('Adresse électronique ou mot de passe incorrect.');
    expect(navigation).not.toHaveBeenCalled();
  });
});
```

- **`{ provide: Session, useValue: { connecter } }`** remplace le vrai service par un objet qui ne contient que ce dont l'écran se sert. Aucune requête ne part : l'écran est testé seul.
- **`vi.fn<Session['connecter']>()`** donne à la fonction factice la signature de la vraie : si `connecter` change de paramètres, ce test ne compilera plus.
- **`mockResolvedValue()`** fait réussir la promesse renvoyée ; `mockRejectedValue(...)` la fait échouer. On rejette avec une `ErreurApi`, ce que `connecter` lève réellement une fois passé par `erreursInterceptor`.
- **`valider()` attend avec `vi.waitFor`** que le bouton redevienne actif : il est désactivé pendant `submitting()`, puis réactivé quand l'action est finie, navigation comprise. `await fixture.whenStable()` ne suffit pas (voir les notions) : avec des doublures qui répondent tout de suite, il semble suffire, et le test cassera le jour où l'une d'elles mettra un peu de temps.
- **Le spy du routeur est posé dans `beforeEach`** : tous les tests peuvent vérifier qu'on n'a pas navigué.

Complétez avec ce que le module 19 a pris soin de coder : sans `retour`, on rejoint `/` ; un retour vers un autre site (`https://pirate.example/`, `//pirate.example/`) mène aussi à `/` (`it.each`) ; une API injoignable (`ErreurApi` de statut `0`) affiche son propre message, pas « mot de passe incorrect » ; le message d'échec disparaît à la tentative suivante (`mockRejectedValueOnce` puis `mockResolvedValueOnce`).
- **`TestbedHarnessEnvironment.loader(fixture)`** cherche les harnais dans le composant. Pour un élément affiché hors du composant — un dialogue, une liste déroulante —, utilisez `TestbedHarnessEnvironment.documentRootLoader(fixture)`.
- **`MatInputHarness.with({ selector })`**, **`MatButtonHarness.with({ text })`** : un filtre désigne l'élément voulu parmi plusieurs.

### 7. Tester une garde et une navigation

`app.routes.spec.ts` vérifie déjà les gardes **sur les vraies routes** de l'application : un visiteur renvoyé à la connexion, un collaborateur qui tombe sur « Page introuvable » à `/plan-de-charge`, un manager qui l'ouvre. C'est le test qui protège la configuration : retirez le `canMatch` du plan de charge dans `app.routes.ts`, il échoue. Mais il mêle tout — routes, écrans, API — et ne dit pas, quand il échoue, si c'est la garde ou la route qui a changé. Les fichiers `auth-guard.spec.ts` et `role-guard.spec.ts` (à créer : la garde de rôle a été écrite à la main) testent chaque garde **seule**, sur des routes écrites pour l'occasion. Un fichier de test par fichier de code : on sait où chercher.

Une garde est une fonction ; on peut l'appeler directement. Elle utilise `inject()`, qui exige un contexte d'injection : `TestBed.runInInjectionContext` le fournit. Mais ce qui compte, c'est son effet sur une vraie navigation. `RouterTestingHarness` joue le rôle de l'application : il porte un `<router-outlet>` et navigue. Dans `src/app/compte/auth-guard.spec.ts` :

```ts
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { authGuard } from './auth-guard';
import { Session } from './session';

@Component({ selector: 'app-ecran-connexion', template: '<h1>Connexion</h1>' })
class EcranConnexion {}

@Component({ selector: 'app-ecran-protege', template: '<h1>Écran protégé</h1>' })
class EcranProtege {}

describe('authGuard', () => {
  /** La session est une doublure d'un signal : chaque test décide si quelqu'un est connecté. */
  const connecte = signal(false);

  beforeEach(() => {
    connecte.set(false);
    TestBed.configureTestingModule({
      providers: [
        { provide: Session, useValue: { estConnecte: connecte } },
        provideRouter([
          { path: 'connexion', component: EcranConnexion },
          {
            path: '',
            canActivate: [authGuard],
            children: [{ path: 'protege', component: EcranProtege }],
          },
        ]),
      ],
    });
  });

  it('appelée directement, renvoie un UrlTree vers la connexion qui retient la page demandée', () => {
    const etat = { url: '/plan-de-charge?semaine=2026-05-04' } as RouterStateSnapshot;

    const resultat = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, etat),
    );

    expect(resultat).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(resultat as UrlTree)).toBe(
      '/connexion?retour=%2Fplan-de-charge%3Fsemaine%3D2026-05-04',
    );
  });

  it('renvoie un visiteur non connecté à la connexion, en retenant la page demandée', async () => {
    const harnais = await RouterTestingHarness.create();

    await harnais.navigateByUrl('/protege');

    expect(TestBed.inject(Router).url).toBe('/connexion?retour=%2Fprotege');
    expect(harnais.routeNativeElement?.textContent).toContain('Connexion');
  });
});
```

- **Le routeur est le vrai** (`provideRouter`), avec des routes de test et de petits composants écrits pour l'occasion. On ne simule pas le routeur : c'est lui qu'on veut voir réagir aux gardes.
- **La session, elle, est une doublure faite d'un signal** que chaque test règle à sa guise.
- **À compléter** : les deux cas d'une personne connectée — la garde appelée directement renvoie `true`, et la navigation ouvre l'écran protégé.

Dans `src/app/compte/role-guard.spec.ts`, même méthode. La doublure de `Session` reproduit `aLeRole` à partir d'un signal de rôles, et l'écran est chargé par une fonction factice :

```ts
const roles = signal<Role[]>([]);
const chargerPlan = vi.fn(async () => EcranPlan);
const session = {
  aLeRole: (...attendus: Role[]) => roles().some((role) => attendus.includes(role)),
};

// dans le beforeEach, après roles.set([]) et chargerPlan.mockClear() :
provideRouter([
  {
    path: 'plan-de-charge',
    canMatch: [roleGuard('Manager', 'GestionnaireRh')],
    loadComponent: chargerPlan,
  },
  { path: '**', component: EcranIntrouvable },
]),

it('sans le rôle, la route n’existe pas et son code n’est pas chargé', async () => {
  roles.set(['Collaborateur']);
  const harnais = await RouterTestingHarness.create();

  await harnais.navigateByUrl('/plan-de-charge');

  expect(harnais.routeNativeElement?.textContent).toContain('Page introuvable');
  expect(chargerPlan).not.toHaveBeenCalled();
});
```

- **`roleGuard('Manager', 'GestionnaireRh')`** : les rôles de la route réelle, dans `app.routes.ts`. Un `it.each<Role>(['Manager', 'GestionnaireRh'])` vérifie que chacun suffit, et que `chargerPlan` est alors appelée une fois.
- **`chargerPlan`** est une fonction factice passée à `loadComponent` : `expect(chargerPlan).not.toHaveBeenCalled()` prouve que le code de l'écran n'est pas téléchargé.
- **À compléter** : `roleGuardAvecRepli('/liste', 'GestionnaireRh')`, dans un second `describe` avec ses propres routes. Sans le rôle, on arrive sur `/liste` (la navigation est redirigée, pas annulée) ; avec, on passe.

Un intercepteur se teste de la même façon qu'un service : `provideHttpClient(withInterceptors([erreursInterceptor, authInterceptor]))`, puis `provideHttpClientTesting()`, une requête par `HttpClient`, une réponse `401` avec `flush`, et l'on vérifie ce qu'ont reçu les doublures du routeur et de `MatSnackBar`. C'est l'exercice 2.

Documentation : [tester le routage](https://angular.dev/guide/routing/testing)

### 8. Mesurer la couverture

```powershell
npm install --save-dev @vitest/coverage-v8
```

```powershell
npx ng test --no-watch --coverage
```

Un tableau s'affiche en fin d'exécution, fichier par fichier, et un rapport HTML est écrit dans `coverage/` — dossier que le `.gitignore` de la CLI exclut déjà. Ouvrez `coverage/chronos-web/index.html` : les lignes jamais exécutées par un test sont surlignées. Dans la solution, à la fin de ce module : 96 % des instructions, 94 % des branches, 92 % des fonctions — vos chiffres seront proches si vous avez testé chaque écran au fil des modules.

La couverture dit ce qui n'est **pas** testé. Elle ne dit pas que ce qui est exécuté est bien vérifié : un test sans `expect` couvre du code sans rien prouver.

Documentation : [couverture de code](https://angular.dev/guide/testing/code-coverage)

### 9. Un parcours de bout en bout avec Playwright

```powershell
npx ng e2e
```

La CLI répond qu'aucune cible `e2e` n'existe, et propose plusieurs outils. Le cours prend Playwright :

```powershell
npx ng add playwright-ng-schematics
```

```powershell
npx playwright install chromium
```

La première commande ajoute `playwright-ng-schematics` et `@playwright/test` aux dépendances, crée `playwright.config.ts` et un dossier `e2e/` (avec son `tsconfig.json` et un exemple), ajoute une cible `e2e` à `angular.json` et un script `e2e` à `package.json`, et complète le `.gitignore`. Elle déclare aussi `schematicCollections` dans `angular.json` et en reformate quelques lignes : regardez le `git diff`, c'est sans conséquence. La seconde télécharge le navigateur que Playwright pilote (sous Linux, ajoutez `--with-deps` pour les bibliothèques système).

La configuration générée prévoit trois navigateurs. Un seul suffit ici ; remplacez le contenu de `playwright.config.ts` par :

```ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  // En intégration continue (variable CI) : un `test.only` oublié fait échouer, un test en échec est rejoué deux fois.
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: process.env['CI'] ? 1 : undefined,
  // `open: 'never'` : le rapport HTML s'écrit dans playwright-report/ sans démarrer de serveur à la fin.
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    // `ng e2e` démarre le serveur de développement et fournit son adresse par cette variable.
    baseURL: process.env['PLAYWRIGHT_TEST_BASE_URL'] ?? 'http://localhost:4200',
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
```

Supprimez `e2e/example.spec.ts`.

Les réponses de l'API sont simulées dans un fichier à part, `e2e/api-simulee.ts`. On y retrouve chaque appel de `Session` et de `FeuillesDeTempsApi`, avec les données de démonstration de l'API — Camille Durand, fiche 2, affectée aux projets 1 et 2 :

```ts
import type { Page, Route } from '@playwright/test';
import type { Page as PageApi } from '../src/app/commun/page';
import type { Utilisateur } from '../src/app/compte/utilisateur';
import type {
  Absence,
  Affectation,
  FeuilleDeTemps,
  JourFerie,
  SaisieDeTemps,
  StatutFeuilleDeTemps,
} from '../src/app/feuilles-de-temps/feuille-de-temps';
import type { Projet } from '../src/app/projets/projet';

export interface EtatApi {
  connecte: boolean;
  statut: StatutFeuilleDeTemps;
  saisies: SaisieDeTemps[];
  /** Le jeton reçu dans l'en-tête `X-XSRF-TOKEN` de la dernière écriture. */
  jetonRecu?: string;
  /** Les appels que l'API simulée ne connaît pas : le parcours vérifie qu'il n'y en a aucun. */
  appelsNonSimules: string[];
}

const moi: Utilisateur = {
  collaborateurId: 2,
  nom: 'Durand',
  prenom: 'Camille',
  email: 'camille.durand@chronos.example',
  roles: ['Collaborateur'],
};

const affectations: Affectation[] = [
  {
    id: 1,
    collaborateurId: 2,
    projetId: 1,
    dateDebut: '2026-04-20',
    dateFin: '2026-12-18',
    tauxAllocation: 0.6,
  },
  {
    id: 2,
    collaborateurId: 2,
    projetId: 2,
    dateDebut: '2026-04-27',
    dateFin: '2026-12-31',
    tauxAllocation: 0.4,
  },
];

const projets: PageApi<Projet> = {
  elements: [
    {
      id: 1,
      code: 'BDL-REFONTE',
      libelle: 'Refonte du portail client',
      nature: 'Facturable',
      clientId: 1,
      clientNom: 'Banque du Littoral',
      dateDebut: '2026-03-02',
      dateFin: '2026-12-18',
      budgetJours: 240,
      responsableId: 1,
      responsableNom: 'Sophie Martin',
      statut: 'EnCours',
      version: '183c529c-97b0-4db1-8188-55f84c0fd61f',
      joursValides: 8.25,
    },
    // … le projet 2, INT-FORMATION, « Formation interne », sur le même modèle
  ],
  page: 1,
  taille: 100,
  total: 2,
};

const joursFeries: JourFerie[] = [{ date: '2026-05-08', libelle: 'Victoire 1945' }];

const absences: Absence[] = [];

/**
 * Remplace l'API par des réponses simulées. Renvoie l'état, que le test peut lire et modifier.
 * Tout appel de `/api/` qui n'est pas simulé échoue (501) et se retrouve dans `appelsNonSimules` :
 * il ne part jamais vers une vraie API.
 */
export async function simulerApi(page: Page, initial: Partial<EtatApi> = {}): Promise<EtatApi> {
  const etat: EtatApi = {
    connecte: false,
    statut: 'Brouillon',
    saisies: [],
    appelsNonSimules: [],
    ...initial,
  };
  const feuille = (): FeuilleDeTemps => ({
    id: 42,
    collaborateurId: 2,
    semaine: '2026-05-04',
    statut: etat.statut,
    motifRejet: null,
    saisies: etat.saisies,
  });

  // Les polices viennent d'Internet : le parcours n'en dépend pas.
  await page.route(/fonts\.(googleapis|gstatic)\.com/, (route) => route.abort());

  // Quand plusieurs routes correspondent, la dernière enregistrée passe en premier : celle-ci, la première,
  // ne reçoit donc que les appels qu'aucune des suivantes ne connaît.
  await page.route(
    (url) => url.pathname.startsWith('/api/'),
    (route) => {
      const requete = route.request();
      etat.appelsNonSimules.push(`${requete.method()} ${requete.url()}`);
      return route.fulfill({ status: 501 });
    },
  );

  const repondre = (methode: string, chemin: string, reponse: (route: Route) => Promise<void>) =>
    page.route(
      (url) => url.pathname === chemin,
      (route) => (route.request().method() === methode ? reponse(route) : route.fallback()),
    );

  await repondre('GET', '/api/compte/moi', (route) =>
    etat.connecte
      ? route.fulfill({
          json: moi,
          headers: { 'Set-Cookie': 'XSRF-TOKEN=jeton-de-test; Path=/' },
        })
      : route.fulfill({ status: 401 }),
  );
  await repondre('POST', '/api/compte/login', (route) => {
    etat.connecte = true;
    return route.fulfill({ status: 200 });
  });
  await repondre('GET', '/api/affectations', (route) => route.fulfill({ json: affectations }));
  await repondre('GET', '/api/projets', (route) => route.fulfill({ json: projets }));
  await repondre('GET', '/api/jours-feries', (route) => route.fulfill({ json: joursFeries }));
  await repondre('GET', '/api/absences', (route) => route.fulfill({ json: absences }));
  await repondre('GET', '/api/feuilles-de-temps', (route) => route.fulfill({ json: feuille() }));
  await repondre('PUT', '/api/feuilles-de-temps/42/saisies', (route) => {
    etat.saisies = route.request().postDataJSON().saisies;
    etat.jetonRecu = route.request().headers()['x-xsrf-token'];
    return route.fulfill({ json: feuille() });
  });
  await repondre('POST', '/api/feuilles-de-temps/42/soumission', (route) => {
    etat.statut = 'Soumise';
    return route.fulfill({ json: feuille() });
  });

  return etat;
}
```

`page.route` intercepte les requêtes du navigateur qui correspondent ; `route.fulfill` y répond sans qu'elles quittent la machine.

- **Les réponses sont typées** avec les types de l'application (`import type`) : la forme d'une `FeuilleDeTemps` ou d'un `Projet` n'est écrite qu'une fois. Elles reprennent celles de la vraie API : comparez-les une fois, champ par champ, aux réponses de l'API du module 12 (comptes de démonstration, Swagger ou `curl`).
- **On compare le chemin et la méthode** (`url.pathname`, `request().method()`), pas un motif d'URL : les paramètres (`?collaborateurId=2&semaine=…`) ne gênent pas, et un `PUT` n'est pas pris pour un `GET`. `route.fallback()` passe la main à la route suivante.
- **La route de rattrapage** répond `501` à tout appel de `/api/` qu'aucune autre ne connaît, et le note dans `appelsNonSimules`. Sans elle, l'appel partirait vers le serveur de développement, puis vers l'API réelle si elle tourne sur votre poste : le test réussirait chez vous et échouerait ailleurs.
- **Les polices Google sont coupées** : le parcours ne dépend pas d'Internet.
- Les réponses du compte suivent le contrat du [module 12](12-securite-et-tracabilite.md) : `login` répond `200` sans corps, et `moi` dépose le cookie `XSRF-TOKEN`. La semaine simulée est celle de l'exemple du cahier des charges, vendredi 8 mai férié compris.

**Playwright ne vérifie pas les types** : il exécute `api-simulee.ts` même si un champ y a le mauvais type (constaté, avec `tauxAllocation: '0.6'`). La vérification se lance à part :

```powershell
npx tsc -p e2e/tsconfig.json --noEmit
```

Le job `web` de l'étape 10 la lance à chaque *push*.

Le parcours, dans `e2e/feuille-de-temps.spec.ts` :

```ts
import { expect, test } from '@playwright/test';
import { simulerApi } from './api-simulee';

test('se connecter, saisir sa feuille de temps et la soumettre', async ({ page }) => {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
  const api = await simulerApi(page);

  await page.goto('/feuille-de-temps');
  await expect(page).toHaveURL(/\/connexion\?retour=%2Ffeuille-de-temps/);

  await page.getByLabel('Adresse électronique').fill('camille.durand@chronos.example');
  await page.getByLabel('Mot de passe').fill('un-mot-de-passe-de-test');
  await page.getByRole('button', { name: 'Se connecter' }).click();

  await expect(page.getByText('Semaine du lun. 4 mai')).toBeVisible();
  await page.getByLabel('Refonte du portail client, lun. 4 mai').selectOption('0.5');
  await page.getByLabel('Refonte du portail client, mar. 5 mai').selectOption('1');
  await expect(page.getByLabel('Refonte du portail client, ven. 8 mai')).toBeDisabled();

  await page.getByRole('button', { name: 'Enregistrer le brouillon' }).click();
  await expect.poll(() => api.saisies).toHaveLength(2);
  await expect(page.getByText('État : Brouillon')).toBeVisible();

  await page.getByRole('button', { name: 'Soumettre' }).click();

  await expect(page.getByText('État : Soumise')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Soumettre' })).toHaveCount(0);
  expect(api.jetonRecu).toBe('jeton-de-test');
  expect(api.saisies).toEqual([
    { projetId: 1, jour: '2026-05-04', duree: 0.5 },
    { projetId: 1, jour: '2026-05-05', duree: 1 },
  ]);
  expect(api.appelsNonSimules).toEqual([]);
});
```

- **`page.clock.setFixedTime`** fige la date du navigateur, comme `vi.setSystemTime` à l'étape 2 : la feuille affichée est toujours celle du 4 mai 2026.
- **`getByRole`, `getByLabel`, `getByText`** désignent les éléments comme un utilisateur les perçoit : par leur rôle, leur libellé, leur texte. C'est ce que recommande Playwright, de préférence aux sélecteurs CSS. Le libellé « Refonte du portail client, lun. 4 mai » est l'`aria-label` que `GrilleDeSaisie` donne à chaque liste déroulante : ce qui sert au lecteur d'écran sert au test.
- **Soyez précis dans les textes** : `getByText('Soumise')` désignerait tout élément qui contient ce mot ; `'État : Soumise'` désigne la ligne d'état, et rien d'autre.
- **Les `expect` de Playwright attendent** que la condition devienne vraie, quelques secondes au plus. On n'écrit jamais de pause. Pour une valeur qui n'est pas dans la page — ici, ce qu'a reçu l'API simulée —, `expect.poll` réessaie de même.
- **Le vendredi férié est fermé** (RG-32), et après la soumission le bouton a disparu (RG-35) : le parcours vérifie deux règles au passage.
- **`api.saisies`** contient ce que l'interface a réellement envoyé : le test vérifie aussi la requête, pas seulement l'écran.
- **`api.jetonRecu`** prouve, dans un vrai navigateur, que le jeton anti-CSRF déposé par `moi` est revenu dans l'en-tête `X-XSRF-TOKEN` de l'écriture. Sans lui, la vraie API répondrait `400`.
- **`api.appelsNonSimules`** vide : l'écran n'a rien demandé que la simulation ignore.

Si un élément d'un autre écran est difficile à désigner par son rôle ou son libellé, ne contournez pas avec une classe CSS : notez-le. C'est un défaut d'accessibilité, que le [module 21](21-accessibilite-et-finitions.md) corrigera.

```powershell
npx ng e2e
```

La commande démarre le serveur de développement, lance le parcours dans Chromium, sans fenêtre, puis arrête tout. `npx ng e2e --ui` ouvre l'interface de Playwright, qui montre chaque étape. Après un échec, `npx playwright show-report` ouvre le rapport.

Documentation : [tests de bout en bout avec la CLI](https://angular.dev/tools/cli/end-to-end), [simuler des réponses](https://playwright.dev/docs/mock), [localisateurs](https://playwright.dev/docs/locators), [horloge](https://playwright.dev/docs/clock)

### 10. Le job `web` du workflow

`actions/setup-node` lira la version de Node.js dans `src/web/package.json` : c'est le bloc `devEngines` que vous y avez écrit au module 13. Vérifiez qu'il s'y trouve toujours.

Dans `.github/workflows/ci.yml`, à la suite du job `api` :

```yaml
  web:
    name: Web
    runs-on: ubuntu-24.04
    timeout-minutes: 15
    defaults:
      run:
        working-directory: src/web
    steps:
      - name: Récupérer le code
        uses: actions/checkout@v7.0.1

      - name: Installer Node.js
        uses: actions/setup-node@v7
        with:
          node-version-file: src/web/package.json
          cache: npm
          cache-dependency-path: src/web/package-lock.json

      - name: Installer les dépendances
        run: npm ci

      - name: Tester
        run: npx ng test

      - name: Compiler
        run: npx ng build

      - name: Vérifier les types des parcours
        run: npx tsc -p e2e/tsconfig.json --noEmit

      - name: Installer Chromium pour Playwright
        run: npx playwright install --with-deps chromium

      - name: Tester de bout en bout
        run: npx ng e2e

      - name: Conserver le rapport Playwright
        if: ${{ !cancelled() }}
        uses: actions/upload-artifact@v7.0.2
        with:
          name: rapport-playwright
          path: src/web/playwright-report/
          if-no-files-found: ignore
```

Ce qui diffère du job `api` :

- **`node-version-file`** joue le rôle de `global-json-file` : la version de Node.js est écrite à un seul endroit. Dans un `package.json`, l'action lit le champ `devEngines.runtime`.
- **`cache: npm`** conserve les paquets téléchargés. La clé du cache vient de `package-lock.json`, que vous versionnez déjà.
- **`npm ci`**, et non `npm install` : l'installation suit strictement le fichier de verrouillage, et échoue s'il ne correspond pas à `package.json`. C'est l'équivalent de `dotnet restore --locked-mode`.
- **`npx ng test`** sans option : la variable `CI` est définie, les tests s'exécutent une fois.
- **`tsc -p e2e/tsconfig.json --noEmit`** : la vérification des types que Playwright ne fait pas (étape 9). Elle dure quelques secondes ; placée avant l'installation de Chromium, elle échoue tôt.
- **`--with-deps`** installe aussi les bibliothèques système dont Chromium a besoin sur Linux. Chromium et ces paquets sont téléchargés à chaque exécution : c'est voulu, la documentation de Playwright déconseille de les mettre en cache, la restauration prenant à peu près autant de temps que le téléchargement. `npx playwright` prend la version de Playwright de votre `package-lock.json` : navigateur et bibliothèque restent accordés.
- **`playwright.config.ts`** lit lui aussi la variable `CI` : il interdit `test.only`, relance deux fois un test en échec et n'utilise qu'un *worker*.
- **Les références d'actions** : `checkout` et `upload-artifact` sont déjà à leur version complète depuis l'étape 8 du module 05 ; `setup-node@v7`, nouvelle, le deviendra (`@v7.1.0` lors de l'essai) avec `gh actions-lock`, ci-dessous.

Le [module 22](22-qualite-du-code.md) insérera le contrôle du formatage et le lint dans ce job, entre `npm ci` et les tests.

Vous avez ajouté une action, `actions/setup-node` : comme au module 05, **régénérez le fichier de verrouillage**, sans quoi GitHub refusera de démarrer le workflow.

```powershell
gh actions-lock
```

```powershell
gh actions-lock --verify
```

Enfin, si vous avez mis en place le *ruleset* du module 05, ajoutez-y la vérification `Web`.

Documentation : [`setup-node`](https://github.com/actions/setup-node), [Playwright en intégration continue](https://playwright.dev/docs/ci-intro), [mise en cache des navigateurs](https://playwright.dev/docs/ci)

## À vous

1. **Le formulaire du collaborateur, mis à l'épreuve.** `formulaire-collaborateur.spec.ts` (module 17) vérifie déjà RG-03 (quotité de 5), RG-02 (sortie avant l'entrée), les champs grisés en modification, le `409` affiché **au niveau du formulaire**, et `quotite: 0.8` pour une saisie de 80. N'écrivez rien de neuf : retrouvez le test de chacune de ces vérifications, puis cassez le code un endroit à la fois — retirez le `min` de RG-03 du schéma, faites envoyer la quotité sans la diviser par 100, laissez la date de sortie modifiable. Chaque fois, prédisez le test qui doit échouer, vérifiez, remettez le code. Ce fichier simule le réseau (`HttpTestingController`) et lit le DOM ; celui de `Connexion` remplace le service et passe par les harnais. Quels avantages voyez-vous à chacun ?

2. **Les intercepteurs, ensemble.** Dans `auth-interceptor.spec.ts`, testez `erreursInterceptor` et `authInterceptor` dans l'ordre de `app.config.ts` : sur `401`, la session est oubliée, le routeur reçoit `/connexion` avec le retour, et l'appelant reçoit une `ErreurApi` ; sur `403`, le message en français s'affiche et l'on ne navigue pas ; une connexion refusée ne déclenche ni l'un ni l'autre. Donnez au routeur et à `MatSnackBar` des doublures réduites à `vi.fn()`. Puis, sans le garder, inversez l'ordre des deux intercepteurs dans le test : lequel de vos tests échoue, et pourquoi ?

3. **La directive `*appSiRole`.** Une directive se teste à travers un composant hôte écrit dans le fichier de test (`si-role.spec.ts`, à créer). Vérifiez que l'élément apparaît et disparaît quand le rôle de l'utilisateur change **après** l'affichage, et quand le rôle demandé change (une liste de rôles est acceptée).

4. **La feuille de temps, relue.** `grille-de-saisie.spec.ts` (module 18) teste déjà les totaux, « Plus d'un jour » (RG-31), les jours fermés (RG-32), la lecture seule (RG-35) et la mise à jour de `grille`. Lisez-le pour répondre : comment donne-t-il une valeur à un `model.required` ? Comment simule-t-il le choix d'une durée dans une liste déroulante native ? Lesquelles de ces vérifications sont déjà faites, sans `TestBed`, par `feuille-de-temps.spec.ts` — et lesquelles n'ont de sens qu'à l'écran ?

5. **Un second parcours.** Dans `e2e/plan-de-charge.spec.ts`, un collaborateur sans autre rôle ouvre `/plan-de-charge` : écrivez le parcours qui vérifie ce qu'il voit, et qu'aucun plan n'est demandé à l'API. Puis celui d'un manager, qui voit la surcharge et passe de l'affichage par semaine à l'affichage par mois. Que devez-vous ajouter à `simulerApi` (rôles, réponses de `/api/plan-de-charge` selon le paramètre `pas`) ? Attention : `.check()` échoue sur un `mat-button-toggle` ; cliquez-le (`.click()`), puis vérifiez `toBeChecked()`.

6. **Un rapport de tests et un seuil.** Dans le job `web`, faites produire à `ng test` un rapport au format JUnit et conservez-le en artefact, même quand les tests échouent — les options sont dans la page de [`ng test`](https://angular.dev/cli/test). Puis fixez un seuil de couverture dans `angular.json` (`coverageThresholds`). Le seuil n'est contrôlé qu'avec `--coverage` : que doit donc contenir l'étape « Tester » ? Quel seuil choisissez-vous, et que se passe-t-il le jour où il n'est plus atteint ? (Essayez avec un seuil trop haut : lisez le message, et le code de sortie.)

## Critères de réussite

- `npx ng test --no-watch` réussit ; `npx ng test --no-watch --coverage` atteint les seuils et écrit un rapport dans `coverage/`.
- Plus aucun fichier de test ne se limite au test généré par la CLI (`should create`). Chaque fichier de code de `src/app/compte/` a son `.spec.ts` — sauf `utilisateur.ts`, qui ne contient que des types. Aucun test ne contient `fakeAsync`, `waitForAsync`, `HttpClientTestingModule` ni `setTimeout`.
- Modifier `authGuard` pour qu'elle renvoie toujours `true` fait échouer au moins un test ; de même en retirant le `canMatch` du plan de charge dans `app.routes.ts`.
- `npx ng e2e` réussit ; le parcours se connecte, saisit, soumet, vérifie les saisies envoyées et l'absence d'appel non simulé ; les deux parcours du plan de charge aussi. `npx tsc -p e2e/tsconfig.json --noEmit` ne signale rien.
- Une *pull request* exécute les jobs `API` et `Web` ; un test d'interface en échec la rend rouge, et le rapport Playwright est téléchargeable.
- `gh actions-lock --verify` ne signale rien.
- Comparez avec la solution de référence : `git diff module-19 module-20`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `fakeAsync(() => { … tick(); })` | Repose sur `zone.js`, qui n'est plus installé. | `async` / `await`, `fixture.whenStable()`, horloge de Vitest. |
| `fixture.detectChanges()` après chaque ligne | Réflexe de l'époque `zone.js` : le test déclenche lui-même ce qu'Angular doit déclencher seul. | `await fixture.whenStable()`, que recommande la documentation. |
| `await fixture.whenStable()` après la soumission d'un formulaire à signaux | L'action de soumission n'est pas suivie : le test vérifie avant la fin, et ne passe que si les doublures répondent tout de suite. | `vi.waitFor` sur un signe visible de la fin (le bouton réactivé). |
| `vi.waitFor` partout, « par sécurité » | Masque un oubli d'`await` ou une requête sans réponse. | `whenStable()` d'abord ; `vi.waitFor` là où il ne suffit pas. |
| Réécrire un test qui existe déjà sous un autre nom | Deux tests à maintenir pour une seule vérification. | Compléter le fichier existant. |
| `.check()` sur un `mat-button-toggle` | Échoue : ce n'est pas une case à cocher native. | `.click()`, puis `expect(...).toBeChecked()`. |
| Croire que Playwright vérifie les types de `e2e/` | Il exécute le TypeScript sans le vérifier : une simulation fausse passe. | `npx tsc -p e2e/tsconfig.json --noEmit`, dans le workflow. |
| Une simulation sans route de rattrapage | Un appel oublié part vers la vraie API, si elle tourne : vert chez vous, rouge ailleurs. | Répondre `501` à tout le reste, et vérifier que la liste est vide. |
| `HttpClientTestingModule`, `RouterTestingModule` | Dépréciés. | `provideHttpClientTesting()`, `provideRouter([...])`. |
| `provideHttpClientTesting()` avant `provideHttpClient()` | Le réseau n'est pas remplacé. | `provideHttpClient()` d'abord. |
| `declarations: [MonComposant]` | Réservé aux composants de `NgModule`. | `imports: [MonComposant]`. |
| `component.charge = …` | Un `input()` est en lecture seule. | `fixture.componentRef.setInput('charge', …)`. |
| `jasmine.createSpy()`, `spyOn(...)`, `.and.returnValue(...)` | API de Jasmine. | `vi.fn()`, `vi.spyOn(...)`, `.mockReturnValue(...)`. |
| `ng test --watch=false --browsers=ChromeHeadless` | Options de Karma. | `ng test --no-watch`, ou la variable `CI`. |
| `querySelector('.mdc-button__label')` | Structure interne de Material, qui change. | `MatButtonHarness`. |
| Simuler le routeur avec `{ navigate: vi.fn() }` pour tester une garde | On ne teste plus la navigation, seulement un appel. | `provideRouter` et `RouterTestingHarness`. |
| `await page.waitForTimeout(2000)` | Trop court sur une machine lente, trop long partout ailleurs. | Les `expect` de Playwright, qui attendent. |
| `page.locator('table tr:nth-child(2) select')` | Casse au premier changement de mise en page. | `getByRole`, `getByLabel`. |
| Protractor | Disparu. | Playwright, par `ng e2e`. |
| `npm install` dans le workflow | Peut modifier `package-lock.json`. | `npm ci`. |
| `node-version: 20` | Version en fin de vie, qu'Angular 22 refuse. | `node-version-file`. |
| Un test qui passe quand on casse le code | Il ne vérifie rien. | Faire échouer chaque test une fois, exprès, avant de le croire. |

Les listes complètes sont dans les référentiels [Angular 22](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus) et [GitHub Actions](../docs/references/github-actions.md#3-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Qu'est-ce qu'un test de composant vérifie que le parcours de bout en bout ne vérifie pas, et l'inverse ?
- Le parcours tourne contre une API simulée. Que se passe-t-il le jour où l'API renomme un champ d'un DTO ? Qui s'en aperçoit, et quand ?
- Dans le test de `Connexion`, la session est une doublure ; dans celui de `Session`, c'est le réseau. Comment décidez-vous où couper ?
- Un test échoue une fois sur dix dans le workflow, jamais sur votre poste. Que faites-vous ? Que pensez-vous des deux nouvelles tentatives que Playwright accorde en intégration continue ?
- 100 % de couverture : est-ce un objectif ? Qu'est-ce qu'un seuil mesure vraiment ?
- Les tests désignent les éléments par leur rôle et leur libellé. Quel lien faites-vous avec l'accessibilité ?

## Pour aller plus loin

- [Mode navigateur de Vitest](https://angular.dev/guide/testing) : exécuter les tests de composants dans un vrai navigateur plutôt que dans jsdom (`ng test --browsers=chromium`, après installation d'un fournisseur de navigateur). Non essayé pour ce cours.
- [Créer un harnais pour ses propres composants](https://angular.dev/guide/testing/creating-component-harnesses) : offrir à la feuille de temps la même commodité qu'aux composants Material.
- [Tester un bloc `@defer`](https://angular.dev/guide/templates/defer) : choisir l'état affiché pendant un test.
- Suite dans le cours : le [module 21](21-accessibilite-et-finitions.md) ajoute un contrôle d'accessibilité au parcours ; le [module 22](22-qualite-du-code.md), le lint au job `web`.
