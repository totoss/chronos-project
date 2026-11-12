# Module 20 — Tester l'interface

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md) et le [référentiel GitHub Actions](../docs/references/github-actions.md).
> Les tests de ce module ont été **exécutés le 12 novembre 2026** dans un conteneur `node:24` (Node.js 24.22.3, npm 11.19.1) : 35 tests Vitest 5.1.0 avec jsdom 30.1.3 sur Angular 22.2.6 et Angular Material 22.2.7, puis le parcours de bout en bout avec Playwright 1.65.0 dans Chromium. Les commandes du job `web` ont été enchaînées dans ce conteneur (`npm ci`, `ng test`, `ng build`, `playwright install`, `ng e2e`), mais **le workflow n'a pas été exécuté sur GitHub**, et `gh actions-lock` n'a pas été relancé. La lecture de `devEngines` par `actions/setup-node` est documentée, non exécutée. Les écrans testés étaient des versions minimales de ceux des modules 16 à 19, dans la coquille du module 15, face à une API simulée d'après le contrat du module 12. **Réécrits ensuite sur le code réel des modules 16 à 18 et non rejoués** : les tests de `SelectionSemaine` (étape 2), d'`IndicateurCharge` (étape 4) et de `ListeCollaborateurs` (étape 5), `api-simulee.ts` et le parcours (étape 9). Les étapes 3, 6, 7 et 8, et la mécanique de toutes les autres, ont été exécutées telles quelles.

L'interface compte maintenant une dizaine d'écrans, une session, des gardes. Chaque modification peut en casser un autre sans que vous le voyiez : personne ne reclique partout après chaque commit. Comme pour l'API au module 04, des tests automatisés le feront à votre place, et le workflow du [module 05](05-github-actions-integration-continue.md) les lancera à chaque *push*.

## Objectifs

À la fin de ce module, vous saurez :

- lancer les tests de l'interface, en continu sur votre poste et en une passe pour l'intégration continue ;
- tester du code à signals sans Angular, puis un service et un composant avec `TestBed` ;
- simuler l'API avec `HttpTestingController`, y compris derrière `httpResource` ;
- piloter un écran Angular Material par ses harnais ;
- tester une garde, un intercepteur et une navigation ;
- écrire un parcours de bout en bout avec Playwright ;
- ajouter un job `web` au workflow d'intégration continue.

## Prérequis

- [Module 19](19-authentification-cote-interface.md) : `Session`, `Connexion`, `authGuard`, `roleGuard`, `authInterceptor`.
- [Module 18](18-ecrans-metier.md) : `SelectionSemaine`, `GrilleDeSaisie`, `FeuilleDeTempsHebdo`, `IndicateurCharge`, et vos premiers tests de fonctions (`dates.ts`, `feuille-de-temps.ts`).
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

**Ce module prend les réponses simulées** : le job `web` reste indépendant du job `api`, et le parcours tient en une page. L'angle mort est couvert autrement : les tests d'intégration du [module 11](11-tests-d-integration.md) vérifient l'API. Le parcours contre l'application complète deviendra simple au [module 23](23-conteneuriser-l-application.md), quand une commande Compose la démarrera en entier.

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

### 2. Tester de la logique, sans Angular

Vous l'avez déjà fait au module 18, pour les fonctions de `commun/dates.ts` et de `feuilles-de-temps/feuille-de-temps.ts` : une valeur en entrée, une valeur attendue en sortie, aucun outillage. Un service à signals qui n'injecte rien se teste de la même façon. Prenez `SelectionSemaine` ; dans `src/app/commun/selection-semaine.spec.ts`, remplacez le test généré par la CLI :

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

Documentation : [horloge simulée de Vitest](https://vitest.dev/guide/mocking/timers)

### 3. Tester un service qui appelle l'API

`src/app/compte/session.spec.ts` :

```ts
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Session } from './session';
import { Utilisateur } from './utilisateur';

const camille: Utilisateur = {
  collaborateurId: 7,
  nom: 'Martin',
  prenom: 'Camille',
  email: 'camille.martin@chronos.example',
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
    expect(session.aLeRole('Manager')).toBe(true);
    expect(session.aLeRole('GestionnaireRh', 'Administrateur')).toBe(false);
  });

  it('se connecte par cookie puis charge le profil', async () => {
    const connexion = session.connecter('camille.martin@chronos.example', 'secret');

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
- **Une réponse d'erreur** se simule avec `flush(null, { status: 401, statusText: 'Unauthorized' })`. Écrivez sur ce modèle le test « reste déconnectée quand l'API répond 401 ».

### 4. Tester un composant

`IndicateurCharge`, du module 18, est un composant de présentation : une entrée, `charge` ; une sortie, `confirmer`. Il n'appelle pas l'API. Dans `src/app/plan-de-charge/indicateur-charge/indicateur-charge.spec.ts` :

```ts
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Charge } from '../charge';
import { IndicateurCharge } from './indicateur-charge';

const surcharge: Charge = {
  debut: '2026-05-04',
  fin: '2026-05-10',
  capacite: 2.4,
  chargePlanifiee: 3.2,
  tauxDeCharge: 4 / 3,
  surcharge: true,
  intercontrat: false,
};

describe('IndicateurCharge', () => {
  let fixture: ComponentFixture<IndicateurCharge>;
  let element: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [IndicateurCharge] });
    fixture = TestBed.createComponent(IndicateurCharge);
    element = fixture.nativeElement;
  });

  it('affiche une charge normale, sans bouton', async () => {
    fixture.componentRef.setInput('charge', { ...surcharge, chargePlanifiee: 2.4, tauxDeCharge: 1, surcharge: false });
    await fixture.whenStable();

    expect(element.querySelector('.taux')?.textContent).toContain('100');
    expect(element.textContent).toContain('Charge normale');
    expect(element.querySelector('button')).toBeNull();
  });

  it('signale la surcharge en toutes lettres (RG-24)', async () => {
    fixture.componentRef.setInput('charge', surcharge);
    await fixture.whenStable();

    expect(element.querySelector('.taux')?.textContent).toContain('133');
    expect(element.textContent).toContain('Surcharge');
    expect(element.classList.contains('surcharge')).toBe(true);
  });

  it('demande la confirmation au parent', async () => {
    const confirmer = vi.fn();
    fixture.componentInstance.confirmer.subscribe(confirmer);
    fixture.componentRef.setInput('charge', surcharge);
    await fixture.whenStable();

    element.querySelector('button')!.click();

    expect(confirmer).toHaveBeenCalledOnce();
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

Les nombres de `surcharge` sont ceux de Camille dans l'exemple du cahier des charges, une fois appliquée la décision du module 09 : 3,2 jours planifiés pour 2,4 de capacité, soit 133 %.

### 5. Tester un écran qui lit l'API

`ListeCollaborateurs` lit ses données avec `httpResource`, à travers `CollaborateursApi`. Dans `liste-collaborateurs.spec.ts` :

```ts
beforeEach(() => {
  TestBed.configureTestingModule({
    imports: [ListeCollaborateurs],
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
  fixture = TestBed.createComponent(ListeCollaborateurs);
  element = fixture.nativeElement;
  http = TestBed.inject(HttpTestingController);
});

afterEach(() => http.verify());

it('affiche la barre de progression, puis les collaborateurs', async () => {
  TestBed.tick();
  expect(element.querySelector('mat-progress-bar')).not.toBeNull();

  const requete = http.expectOne((req) => req.url === '/api/collaborateurs');
  expect(requete.request.params.get('page')).toBe('1');
  requete.flush(pageUne);
  await fixture.whenStable();

  expect(element.querySelector('mat-progress-bar')).toBeNull();
  expect(element.textContent).toContain('Martin');
});
```

`pageUne` est un objet `Page<Collaborateur>` écrit dans le fichier de test ; `provideRouter([])` est là pour les liens de la liste. Le cas d'erreur s'écrit de même, avec un `flush(null, { status: 500, … })`.

Ici, on ne peut pas commencer par `await fixture.whenStable()` : une requête est en attente, l'application n'est pas stable, et la promesse ne se résout qu'après la réponse (constaté) — que le test n'a pas encore donnée. **`TestBed.tick()`** fait faire un tour à Angular : la ressource émet sa requête, l'écran affiche sa barre de progression. Le test répond, puis attend la stabilité.

La recherche de cette liste est temporisée (`Subject` et `debounceTime`, module 16) : c'est un bon terrain pour l'horloge simulée de l'étape 2. `await vi.advanceTimersByTimeAsync(300)` fait passer les 300 ms sans les attendre.

Documentation : [tester un `httpResource`](https://angular.dev/guide/http/http-resource)

### 6. Piloter un écran Material par ses harnais

`src/app/compte/connexion/connexion.spec.ts` :

```ts
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatButtonHarness } from '@angular/material/button/testing';
import { MatFormFieldHarness } from '@angular/material/form-field/testing';
import { MatInputHarness } from '@angular/material/input/testing';
import { provideRouter, Router } from '@angular/router';
import { Connexion } from './connexion';
import { Session } from '../session';

describe('Connexion', () => {
  let fixture: ComponentFixture<Connexion>;
  let loader: HarnessLoader;
  const connecter = vi.fn<Session['connecter']>();

  beforeEach(() => {
    connecter.mockReset();
    TestBed.configureTestingModule({
      imports: [Connexion],
      providers: [provideRouter([]), { provide: Session, useValue: { connecter } }],
    });
    fixture = TestBed.createComponent(Connexion);
    loader = TestbedHarnessEnvironment.loader(fixture);
  });

  async function saisir(email: string, password: string): Promise<void> {
    const champEmail = await loader.getHarness(MatInputHarness.with({ selector: '[type=email]' }));
    const champMotDePasse = await loader.getHarness(MatInputHarness.with({ selector: '[type=password]' }));
    await champEmail.setValue(email);
    await champMotDePasse.setValue(password);
  }

  async function valider(): Promise<void> {
    const bouton = await loader.getHarness(MatButtonHarness.with({ text: 'Se connecter' }));
    await bouton.click();
  }

  it('connecte puis retourne à la page demandée', async () => {
    connecter.mockResolvedValue();
    const navigation = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fixture.componentRef.setInput('retour', '/feuille-de-temps?semaine=2026-05-04');

    await saisir('camille.martin@chronos.example', 'secret');
    await valider();
    await fixture.whenStable();

    expect(connecter).toHaveBeenCalledWith('camille.martin@chronos.example', 'secret');
    expect(navigation).toHaveBeenCalledWith('/feuille-de-temps?semaine=2026-05-04');
  });

  it("n'appelle pas l'API quand le formulaire est invalide", async () => {
    await saisir('pas-une-adresse', '');
    await valider();

    expect(connecter).not.toHaveBeenCalled();
    const champs = await loader.getAllHarnesses(MatFormFieldHarness);
    expect(await champs[0].getTextErrors()).toEqual(["L'adresse électronique est invalide."]);
  });
});
```

- **`{ provide: Session, useValue: { connecter } }`** remplace le vrai service par un objet qui ne contient que ce dont l'écran se sert. Aucune requête ne part : l'écran est testé seul.
- **`vi.fn<Session['connecter']>()`** donne à la fonction factice la signature de la vraie : si `connecter` change de paramètres, ce test ne compilera plus.
- **`mockResolvedValue()`** fait réussir la promesse renvoyée ; `mockRejectedValue(new Error('401'))` la ferait échouer. Écrivez ainsi le troisième test : connexion refusée, message générique dans l'élément `[role=alert]`, aucune navigation.
- **`TestbedHarnessEnvironment.loader(fixture)`** cherche les harnais dans le composant. Pour un élément affiché hors du composant — un dialogue, une liste déroulante —, utilisez `TestbedHarnessEnvironment.documentRootLoader(fixture)`.
- **`MatInputHarness.with({ selector })`**, **`MatButtonHarness.with({ text })`** : un filtre désigne l'élément voulu parmi plusieurs.

### 7. Tester une garde et une navigation

Une garde est une fonction ; on peut l'appeler directement. Elle utilise `inject()`, qui exige un contexte d'injection : `TestBed.runInInjectionContext` le fournit. Mais ce qui compte, c'est son effet sur une vraie navigation. `RouterTestingHarness` joue le rôle de l'application : il porte un `<router-outlet>` et navigue. Dans `src/app/compte/gardes.spec.ts` :

```ts
@Component({ selector: 'app-ecran-connexion', template: '<h1>Connexion</h1>' })
class EcranConnexion {}

@Component({ selector: 'app-ecran-plan', template: '<h1>Plan de charge</h1>' })
class EcranPlan {}

@Component({ selector: 'app-ecran-introuvable', template: '<h1>Page introuvable</h1>' })
class EcranIntrouvable {}

describe('gardes', () => {
  const roles = signal<Role[]>([]);
  const connecte = signal(false);
  const chargerPlan = vi.fn(async () => EcranPlan);

  beforeEach(() => {
    roles.set([]);
    connecte.set(false);
    chargerPlan.mockClear();

    TestBed.configureTestingModule({
      providers: [
        {
          provide: Session,
          useValue: {
            estConnecte: connecte,
            aLeRole: (...attendus: Role[]) => roles().some((role) => attendus.includes(role)),
          },
        },
        provideRouter([
          { path: 'connexion', component: EcranConnexion },
          {
            path: '',
            canActivate: [authGuard],
            children: [
              { path: 'plan-de-charge', canMatch: [roleGuard('Manager')], loadComponent: chargerPlan },
              { path: '**', component: EcranIntrouvable },
            ],
          },
        ]),
      ],
    });
  });

  it('authGuard, appelée directement, renvoie un UrlTree vers la connexion', () => {
    const etat = { url: '/plan-de-charge' } as RouterStateSnapshot;

    const resultat = TestBed.runInInjectionContext(() => authGuard({} as ActivatedRouteSnapshot, etat));

    expect(resultat).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(resultat as UrlTree)).toBe('/connexion?retour=%2Fplan-de-charge');
  });

  it('sans le rôle, la route n’existe pas et son code n’est pas chargé', async () => {
    connecte.set(true);
    roles.set(['Collaborateur']);
    const harnais = await RouterTestingHarness.create();
    await harnais.navigateByUrl('/plan-de-charge');

    expect(harnais.routeNativeElement?.textContent).toContain('Page introuvable');
    expect(chargerPlan).not.toHaveBeenCalled();
  });
});
```

Les imports viennent de `@angular/core`, `@angular/core/testing`, `@angular/router` et `@angular/router/testing`.

- **Le routeur est le vrai** (`provideRouter`), avec des routes de test et de petits composants écrits pour l'occasion. On ne simule pas le routeur : c'est lui qu'on veut voir réagir aux gardes.
- **La session, elle, est une doublure faite de deux signals** que chaque test règle à sa guise.
- **`chargerPlan`** est une fonction factice passée à `loadComponent` : `expect(chargerPlan).not.toHaveBeenCalled()` prouve que le code de l'écran n'est pas téléchargé.
- **À compléter** : un visiteur non connecté qui demande `/plan-de-charge` arrive sur `/connexion?retour=%2Fplan-de-charge` (`TestBed.inject(Router).url`), et un manager voit le plan de charge.

Un intercepteur se teste de la même façon qu'un service : `provideHttpClient(withInterceptors([erreursInterceptor, authInterceptor]))`, puis `provideHttpClientTesting()`, une requête par `HttpClient`, une réponse `401` avec `flush`, et l'on vérifie ce qu'ont reçu les doublures du routeur et de `MatSnackBar`. C'est l'exercice 2.

Documentation : [tester le routage](https://angular.dev/guide/routing/testing)

### 8. Mesurer la couverture

```powershell
npm install --save-dev @vitest/coverage-v8
```

```powershell
npx ng test --no-watch --coverage
```

Un tableau s'affiche en fin d'exécution, fichier par fichier, et un rapport HTML est écrit dans `coverage/` — dossier que le `.gitignore` de la CLI exclut déjà. Ouvrez `coverage/index.html` : les lignes jamais exécutées par un test sont surlignées.

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

La première commande ajoute `@playwright/test` aux dépendances, crée `playwright.config.ts` et un dossier `e2e/`, ajoute une cible `e2e` à `angular.json` et complète le `.gitignore`. La seconde télécharge le navigateur que Playwright pilote.

Dans `playwright.config.ts`, ne gardez que le projet `chromium` dans la liste `projects` : un seul navigateur suffit ici. Supprimez `e2e/example.spec.ts`.

Les réponses de l'API sont simulées dans un fichier à part, `e2e/api-simulee.ts`. On y retrouve chaque appel de `Session` et de `FeuillesDeTempsApi` :

```ts
import { Page } from '@playwright/test';

export interface EtatApi {
  connecte: boolean;
  statut: 'Brouillon' | 'Soumise' | 'Validee';
  saisies: unknown[];
  jetonRecu?: string;
}

const camille = {
  collaborateurId: 7,
  nom: 'Martin',
  prenom: 'Camille',
  email: 'camille.martin@chronos.example',
  roles: ['Collaborateur'],
};

/** Remplace l'API par des réponses simulées. Renvoie l'état, que le test peut lire et modifier. */
export async function simulerApi(page: Page, initial: Partial<EtatApi> = {}): Promise<EtatApi> {
  const etat: EtatApi = { connecte: false, statut: 'Brouillon', saisies: [], ...initial };
  const feuille = () => ({
    id: 42,
    collaborateurId: 7,
    semaine: '2026-05-04',
    statut: etat.statut,
    motifRejet: null,
    saisies: etat.saisies,
  });

  await page.route('**/api/compte/moi', (route) =>
    etat.connecte
      ? route.fulfill({ json: camille, headers: { 'Set-Cookie': 'XSRF-TOKEN=jeton-de-test; Path=/' } })
      : route.fulfill({ status: 401 }),
  );
  await page.route('**/api/compte/login?useCookies=true', (route) => {
    etat.connecte = true;
    return route.fulfill({ status: 200 });
  });
  await page.route('**/api/affectations?*', (route) =>
    route.fulfill({
      json: [
        { id: 1, collaborateurId: 7, projetId: 1, dateDebut: '2026-01-01', dateFin: '2026-12-31', tauxAllocation: 0.5 },
      ],
    }),
  );
  await page.route('**/api/projets?*', (route) =>
    route.fulfill({
      json: { elements: [{ id: 1, code: 'ALPHA', libelle: 'Projet Alpha' }], page: 1, taille: 100, total: 1 },
    }),
  );
  await page.route('**/api/jours-feries', (route) =>
    route.fulfill({ json: [{ date: '2026-05-08', libelle: 'Victoire 1945' }] }),
  );
  await page.route('**/api/feuilles-de-temps?*', (route) => route.fulfill({ json: feuille() }));
  await page.route('**/api/feuilles-de-temps/42/saisies', (route) => {
    etat.saisies = route.request().postDataJSON().saisies;
    etat.jetonRecu = route.request().headers()['x-xsrf-token'];
    return route.fulfill({ json: feuille() });
  });
  await page.route('**/api/feuilles-de-temps/42/soumission', (route) => {
    etat.statut = 'Soumise';
    return route.fulfill({ json: feuille() });
  });

  return etat;
}
```

`page.route` intercepte les requêtes du navigateur dont l'URL correspond au motif ; `route.fulfill` y répond sans qu'elles quittent la machine. Les réponses du compte suivent le contrat du [module 12](12-securite-et-tracabilite.md) : `login` répond `200` sans corps, et `moi` dépose le cookie `XSRF-TOKEN`. Les autres suivent les types du module 18 ; complétez l'objet du projet avec les propriétés de votre type `Projet`. La semaine simulée est celle de l'exemple du cahier des charges, vendredi 8 mai férié compris.

Le parcours, dans `e2e/feuille-de-temps.spec.ts` :

```ts
import { expect, test } from '@playwright/test';
import { simulerApi } from './api-simulee';

test('se connecter, saisir sa feuille de temps et la soumettre', async ({ page }) => {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
  const api = await simulerApi(page);

  await page.goto('/feuille-de-temps');
  await expect(page).toHaveURL(/\/connexion\?retour=%2Ffeuille-de-temps/);

  await page.getByLabel('Adresse électronique').fill('camille.martin@chronos.example');
  await page.getByLabel('Mot de passe').fill('un-mot-de-passe-de-test');
  await page.getByRole('button', { name: 'Se connecter' }).click();

  await page.getByLabel('Projet Alpha, lun. 4 mai').selectOption('0.5');
  await page.getByLabel('Projet Alpha, mar. 5 mai').selectOption('1');
  await expect(page.getByLabel('Projet Alpha, ven. 8 mai')).toBeDisabled();

  await page.getByRole('button', { name: 'Soumettre' }).click();

  await expect(page.getByText('Soumise')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Soumettre' })).toHaveCount(0);
  expect(api.jetonRecu).toBe('jeton-de-test');
  expect(api.saisies).toEqual([
    { projetId: 1, jour: '2026-05-04', duree: 0.5 },
    { projetId: 1, jour: '2026-05-05', duree: 1 },
  ]);
});
```

- **`page.clock.setFixedTime`** fige la date du navigateur, comme `vi.setSystemTime` à l'étape 2 : la feuille affichée est toujours celle du 4 mai 2026.
- **`getByRole`, `getByLabel`** désignent les éléments comme un utilisateur les perçoit : par leur rôle et leur libellé. C'est ce que recommande Playwright, de préférence aux sélecteurs CSS. Le libellé « Projet Alpha, lun. 4 mai » est l'`aria-label` que `GrilleDeSaisie` donne à chaque liste déroulante : ce qui sert au lecteur d'écran sert au test.
- **Les `expect` de Playwright attendent** que la condition devienne vraie, quelques secondes au plus. On n'écrit jamais de pause.
- **Le vendredi férié est fermé** (RG-32), et après la soumission le bouton a disparu (RG-35) : le parcours vérifie deux règles au passage.
- **`api.saisies`** contient ce que l'interface a réellement envoyé : le test vérifie aussi la requête, pas seulement l'écran.
- **`api.jetonRecu`** prouve, dans un vrai navigateur, que le jeton anti-CSRF déposé par `moi` est revenu dans l'en-tête `X-XSRF-TOKEN` de l'écriture. Sans lui, la vraie API répondrait `400`.

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
        uses: actions/checkout@v7

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

      - name: Installer Chromium pour Playwright
        run: npx playwright install --with-deps chromium

      - name: Tester de bout en bout
        run: npx ng e2e

      - name: Conserver le rapport Playwright
        if: ${{ !cancelled() }}
        uses: actions/upload-artifact@v7
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
- **`--with-deps`** installe aussi les bibliothèques système dont Chromium a besoin sur Linux.
- **`playwright.config.ts`** lit lui aussi la variable `CI` : il interdit `test.only` et relance deux fois un test en échec.

Le [module 22](22-qualite-du-code.md) insérera le contrôle du formatage et le lint dans ce job, entre `npm ci` et les tests.

Vous avez ajouté une action, `actions/setup-node` : comme au module 05, **régénérez le fichier de verrouillage**, sans quoi GitHub refusera de démarrer le workflow.

```powershell
gh actions-lock
```

```powershell
gh actions-lock --verify
```

Enfin, si vous avez mis en place le *ruleset* du module 05, ajoutez-y la vérification `Web`.

Documentation : [`setup-node`](https://github.com/actions/setup-node), [Playwright en intégration continue](https://playwright.dev/docs/ci-intro)

## À vous

1. **Le formulaire du collaborateur.** Avec les harnais, testez `FormulaireCollaborateur` en donnant à `CollaborateursApi` une doublure : en création, une quotité de 5 affiche l'erreur de RG-03 et n'envoie rien ; en modification, une date de sortie antérieure à la date d'entrée affiche celle de RG-02, et les champs matricule et quotité sont désactivés — `MatInputHarness` a une méthode pour le savoir. Faites rejeter `creer` par une `ErreurApi` de statut `409` : le message s'affiche **au niveau du formulaire**, et non sur un champ, puisque l'API ne dit pas lequel est en cause. Vérifiez enfin que la requête envoyée porte `quotite: 0.8` pour une saisie de 80.

2. **Les intercepteurs, ensemble.** Testez `erreursInterceptor` et `authInterceptor` dans l'ordre de `app.config.ts` : sur `401`, la session est oubliée, le routeur reçoit `/connexion` avec le retour, et l'appelant reçoit une `ErreurApi` ; sur `403`, le message en français s'affiche et l'on ne navigue pas ; une connexion refusée ne déclenche ni l'un ni l'autre. Donnez au routeur et à `MatSnackBar` des doublures réduites à `vi.fn()`. Puis inversez l'ordre des deux intercepteurs dans le test : lequel de vos tests échoue, et pourquoi ?

3. **La directive `*appSiRole`.** Une directive se teste à travers un composant hôte écrit dans le fichier de test. Vérifiez que l'élément apparaît et disparaît quand le rôle change **après** l'affichage.

4. **La feuille de temps.** Testez `GrilleDeSaisie`, composant de présentation : le total d'une journée et d'une ligne ; le texte « Plus d'un jour » quand un total dépasse 1 (RG-31) ; les listes fermées un jour férié (RG-32) et toutes fermées en `lectureSeule` (RG-35) ; la mise à jour du `model` `grille` quand on choisit une durée. Comment donnez-vous une valeur à un `model.required` dans un test ? Lesquelles de ces vérifications sont déjà faites, sans `TestBed`, par vos tests de `feuille-de-temps.ts` ?

5. **Un second parcours.** Un collaborateur sans autre rôle ouvre `/plan-de-charge` : écrivez le parcours qui vérifie ce qu'il voit. Puis celui d'un manager. Que devez-vous ajouter à `simulerApi` ?

6. **Un rapport de tests et un seuil.** Dans le job `web`, faites produire à `ng test` un rapport au format JUnit et conservez-le en artefact, même quand les tests échouent — les options sont dans la page de [`ng test`](https://angular.dev/cli/test). Puis fixez un seuil de couverture dans `angular.json` (`coverageThresholds`). Quel seuil choisissez-vous, et que se passe-t-il le jour où il n'est plus atteint ?

## Critères de réussite

- `npx ng test --no-watch` réussit ; `npx ng test --no-watch --coverage` écrit un rapport dans `coverage/`.
- Chaque fichier de `src/app/compte/` a son fichier `.spec.ts`, et aucun test ne contient `fakeAsync`, `waitForAsync` ni `HttpClientTestingModule`.
- Modifier `authGuard` pour qu'elle renvoie toujours `true` fait échouer au moins un test ; de même en retirant le `canMatch` du plan de charge.
- `npx ng e2e` réussit ; le parcours se connecte, saisit, soumet, et vérifie les saisies envoyées.
- Une *pull request* exécute les jobs `API` et `Web` ; un test d'interface en échec la rend rouge, et le rapport Playwright est téléchargeable.
- `gh actions-lock --verify` ne signale rien.
- Comparez avec la solution de référence : `git diff module-19 module-20`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `fakeAsync(() => { … tick(); })` | Repose sur `zone.js`, qui n'est plus installé. | `async` / `await`, `fixture.whenStable()`, horloge de Vitest. |
| `fixture.detectChanges()` après chaque ligne | Réflexe de l'époque `zone.js` : le test déclenche lui-même ce qu'Angular doit déclencher seul. | `await fixture.whenStable()`, que recommande la documentation. |
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
