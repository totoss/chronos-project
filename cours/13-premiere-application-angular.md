# Module 13 — Première application Angular

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md).
> Les commandes et le code de ce module ont été **exécutés le 12 novembre 2026** dans un conteneur Linux `node:24` : Node.js 24.22.3, npm 11.19.1, Angular CLI 22.2.7, Angular 22.2.6, TypeScript 6.0.3, Vitest 5.1.0. `ng new`, `ng generate`, `ng build`, `ng test` et `ng serve` y réussissent, et le piège TypeScript 7 y a été reproduit. **Non exécuté** : les mêmes commandes sous Windows / PowerShell (le poste de rédaction n'a pas Node.js). Signalez au mentor tout écart constaté.

L'API de Chronos sait créer un collaborateur, calculer une charge, valider une feuille de temps. Mais personne, dans la société, ne va envoyer du JSON à la main : il faut des écrans. Ce module crée l'application Angular qui les portera, et y affiche une première liste de collaborateurs. Les données sont encore écrites en dur : le dialogue avec l'API viendra au [module 16](16-dialoguer-avec-l-api.md).

## Objectifs

À la fin de ce module, vous saurez :

- créer une application Angular 22 avec la CLI et dire à quoi sert chaque fichier généré ;
- lire et écrire un composant : sa classe, son template, son sélecteur ;
- lier le template à la classe : afficher une valeur, fixer une propriété, réagir à un événement ;
- conserver de l'état dans un `signal`, le modifier avec `set` et `update`, en dériver un autre avec `computed` ;
- écrire un template avec `@if`, `@for`, `@empty` et `@let` ;
- expliquer en deux phrases ce que « zoneless » et « OnPush » changent pour vous ;
- fixer les versions de Node.js et de TypeScript du projet.

## Prérequis

- [Module 00](00-environnement-de-travail.md) : Node.js 24 est installé, et vous savez appeler la CLI Angular par `npx`. Angular 22 exige Node.js `^22.22.3`, `^24.15.0` ou `^26.0.0` ([tableau de compatibilité](https://angular.dev/reference/versions)).
- Connaître HTML et CSS, et avoir déjà lu du TypeScript ou du JavaScript moderne (classes, fonctions fléchées, modules `import` / `export`).
- La partie 1 du cours : vous savez ce que l'API expose. Elle n'a pas besoin de tourner pendant ce module.

## Règles de gestion

Aucune. L'écran construit ici prépare le besoin « rechercher et filtrer les collaborateurs » du [cahier des charges](../docs/cahier-des-charges.md) (§ 4.1) ; les règles `RG-xx` arriveront avec les formulaires, au [module 17](17-formulaires.md).

## Notions

### Où se place Angular

Jusqu'ici, tout votre code tournait sur un serveur. Une application Angular tourne **dans le navigateur** de l'utilisateur : le serveur lui envoie une fois des fichiers HTML, CSS et JavaScript, puis l'application dessine elle-même les écrans et appelle l'API pour lire et écrire les données.

Conséquence à garder en tête pendant toute la partie 2 : ce code s'exécute sur une machine que vous ne maîtrisez pas. Tout contrôle fait dans l'interface (un bouton masqué, un champ validé) est un confort pour l'utilisateur, jamais une sécurité. Les règles de gestion restent garanties par l'API.

### La CLI et l'espace de travail

La **CLI** (`ng`) est l'outil en ligne de commande d'Angular : elle crée l'application, génère des fichiers, lance le serveur de développement, compile et teste. Elle joue le rôle de `dotnet` côté API.

Elle travaille dans un **espace de travail** : un dossier contenant `angular.json` (la configuration de la CLI) et `package.json` (les dépendances npm). Pour Chronos, ce dossier est `src/web`.

Documentation : [structure des fichiers d'un espace de travail](https://angular.dev/reference/configs/file-structure)

### Composant

Un écran Angular est un arbre de **composants**. Un composant réunit trois choses :

- une **classe** TypeScript, qui porte les données et le comportement ;
- un **template** HTML, qui décrit ce qui s'affiche ;
- un **sélecteur**, le nom de la balise par laquelle un autre template l'utilise (`<app-liste-collaborateurs />`).

Le décorateur `@Component` relie les trois. Un composant est **autonome** : pour utiliser un autre composant dans son template, il le cite dans sa propriété `imports`. Il n'y a pas de `NgModule`, et on n'écrit pas `standalone: true` — c'est le comportement par défaut.

Documentation : [anatomie d'un composant](https://angular.dev/guide/components)

### Les liaisons du template

Le template lit et appelle les membres de la classe de trois façons :

| Syntaxe | Sens | Exemple |
| --- | --- | --- |
| `{{ expression }}` | Affiche une valeur dans le texte. | `{{ c.nom }}` |
| `[propriete]="expression"` | Fixe une propriété d'un élément ou d'un composant. | `[disabled]="filtre() === ''"` |
| `(evenement)="instruction"` | Exécute du code quand l'événement se produit. | `(click)="effacerFiltre()"` |

Les crochets font descendre une valeur de la classe vers le template ; les parenthèses font remonter un événement du template vers la classe. `[class.sorti]="condition"` ajoute ou retire une classe CSS : c'est la forme à préférer à l'ancienne directive `ngClass`.

Documentation : [liaisons de texte, de propriétés et d'attributs](https://angular.dev/guide/templates/binding), [écouteurs d'événements](https://angular.dev/guide/templates/event-listeners)

### Signals

Un **signal** est une boîte qui contient une valeur et qui **prévient Angular quand cette valeur change**. C'est le mécanisme sur lequel repose toute la mise à jour de l'affichage.

```ts
const filtre = signal('');          // création, avec la valeur initiale
filtre();                           // lecture : on appelle le signal comme une fonction
filtre.set('mar');                  // remplacement de la valeur
filtre.update((v) => v.trim());     // nouvelle valeur calculée à partir de l'ancienne
```

Un signal **calculé** (`computed`) dérive sa valeur d'autres signals. Angular retient lesquels il a lus et le recalcule quand l'un d'eux change — et seulement si quelqu'un le lit. Il est en lecture seule : on ne peut pas lui appliquer `set`.

```ts
const nombre = computed(() => collaborateurs().length);
```

La règle pratique : **ce qui se déduit d'un autre état ne se stocke pas, il se calcule.** La liste filtrée de Chronos n'est pas un second tableau à tenir à jour ; c'est un `computed` de la liste complète et du filtre.

Documentation : [signals](https://angular.dev/guide/signals)

### Flux de contrôle

Les conditions et les boucles s'écrivent dans le template avec des blocs `@` :

- `@if (condition) { … } @else { … }` affiche un bloc ou l'autre ;
- `@for (c of liste; track c.id) { … } @empty { … }` répète un bloc pour chaque élément ; `@empty` s'affiche quand la liste est vide ;
- `@let nom = expression;` déclare une variable locale au template, tenue à jour par Angular.

`track` est **obligatoire** : il dit à Angular comment reconnaître un élément d'un rendu au suivant, pour ne toucher que les lignes qui changent. On lui donne un identifiant unique et stable — pour un collaborateur, son `id`.

Ces blocs remplacent `*ngIf` et `*ngFor`, dépréciés depuis la version 20.

Documentation : [flux de contrôle](https://angular.dev/guide/templates/control-flow), [variables de template et `@let`](https://angular.dev/guide/templates/variables)

### Zoneless et OnPush, en clair

Comment Angular sait-il qu'il doit redessiner l'écran ? Pendant dix ans, la réponse a été une bibliothèque, `zone.js`, qui interceptait tous les événements du navigateur et faisait revérifier toute l'application « au cas où ». Depuis la version 21, les nouvelles applications sont **zoneless** : `zone.js` n'est plus installée. Et depuis la version 22, tout composant est en **OnPush** sans qu'on l'écrive : Angular ne le revérifie que s'il a une raison de le faire.

Les raisons qui comptent pour vous sont au nombre de deux :

- un **signal lu dans le template** a changé ;
- un **événement écouté dans le template** (`(click)`, `(input)`…) vient de se produire.

Ce que cela implique : un simple champ de classe modifié en dehors de ces deux cas — dans un `setTimeout`, à l'arrivée d'une réponse réseau — **ne rafraîchit pas l'écran**. D'où la règle du cours : tout état affiché est un signal. L'exercice 6 vous le fera constater.

Documentation : [zoneless](https://angular.dev/guide/zoneless), [`ChangeDetectionStrategy`](https://angular.dev/api/core/ChangeDetectionStrategy)

## Pas à pas

### 1. Vérifier Node.js

```powershell
node --version
```

```powershell
npm --version
```

La première doit afficher une version acceptée par Angular 22 (voir les prérequis). Le cours a été exécuté avec Node.js 24.22.3 et npm 11.19.1.

### 2. Créer l'application

Placez-vous dans le dossier `src` du dépôt, à côté de `api` :

```powershell
cd src
```

```powershell
npx @angular/cli@22 new chronos-web --directory web --style scss --ssr false --skip-git
```

Décomposons :

| Élément | Rôle |
| --- | --- |
| `npx @angular/cli@22` | Télécharge la CLI en version 22 et l'exécute, sans rien installer globalement. npm demande une confirmation la première fois. |
| `new chronos-web` | Crée un espace de travail et une application nommés `chronos-web`. |
| `--directory web` | Les place dans `src/web` plutôt que dans un dossier `chronos-web`. |
| `--style scss` | Feuilles de style en SCSS : le thème Angular Material du [module 14](14-composer-des-composants.md) en a besoin. |
| `--ssr false` | Pas de rendu côté serveur : Chronos est une application de gestion interne, rendue dans le navigateur. |
| `--skip-git` | Ne crée pas de dépôt git : vous êtes déjà dans le vôtre. |

La CLI pose encore deux questions :

- **partager des données d'utilisation anonymes avec l'équipe Angular** : répondez `N` (la question n'apparaît qu'une fois par poste ; [détails](https://angular.dev/cli/analytics)) ;
- **« Which AI tools should Angular integrate with? »** : gardez `None` et validez. Cette option génère des fichiers d'instructions pour des assistants de code ; le cours ne s'en sert pas.

Sans `--style` et `--ssr`, elle aurait aussi demandé le système de feuilles de style et l'activation du rendu côté serveur.

La commande affiche la liste des fichiers créés, puis `Packages installed successfully.` Tout le reste vient des valeurs par défaut de la version 22 : composants autonomes, zoneless, tests avec Vitest, TypeScript strict, noms de fichiers sans suffixe ([options de `ng new`](https://angular.dev/cli/new)).

> Comme annoncé au module 00, le cours n'installe pas la CLI globalement, contrairement à la [documentation d'Angular](https://angular.dev/installation). Une fois le projet créé, **`npx ng`, lancé dans `src/web`, exécute la CLI du projet**, celle que fixe `package.json` : tout le monde utilise la même.

### 3. Faire le tour des fichiers

```
src/web/
  angular.json           configuration de la CLI : compilation, serveur, tests
  package.json           dépendances et scripts npm
  package-lock.json      versions exactes installées — à versionner
  tsconfig.json          options TypeScript communes
  tsconfig.app.json      … pour l'application
  tsconfig.spec.json     … pour les tests
  .editorconfig          règles d'indentation pour l'éditeur
  .prettierrc            règles de mise en forme (Prettier)
  .gitignore             exclut node_modules/, dist/, .angular/cache
  .vscode/               extension et tâches recommandées pour VS Code
  public/                fichiers servis tels quels (favicon.ico)
  src/
    index.html           la page unique ; elle contient <app-root>
    main.ts              le point d'entrée : démarre l'application
    styles.scss          styles globaux
    app/
      app.ts             le composant racine, App
      app.html           son template
      app.scss           ses styles
      app.spec.ts        ses tests
      app.config.ts      la configuration de l'application (fournisseurs)
      app.routes.ts      les routes — vides pour l'instant (module 15)
```

Ouvrez `package.json` et relevez deux choses : la ligne `"typescript": "~6.0.2"`, sur laquelle l'étape 10 revient, et les scripts `start`, `build` et `test`, qui appellent `ng serve`, `ng build` et `ng test`.

Deux remarques pour éviter des recherches inutiles. `tsconfig.json` ne contient pas de ligne `"strict": true` : avec TypeScript 6, le mode strict est la valeur par défaut. Et la CLI installe Prettier avec son fichier `.prettierrc` : laissez-les tels quels, le [module 22](22-qualite-du-code.md) s'en occupe.

`src/main.ts` tient en une instruction : démarrer le composant `App` avec la configuration `appConfig`.

```ts
bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
```

`src/app/app.ts` est le composant racine :

```ts
import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('chronos-web');
}
```

Ce qu'il faut y lire :

- la classe s'appelle `App` et le fichier `app.ts` — pas `AppComponent` dans `app.component.ts`, comme dans les tutoriels antérieurs à 2025 ;
- `selector: 'app-root'` est la balise que contient `index.html` ;
- `templateUrl` et `styleUrl` désignent les deux fichiers voisins ;
- `imports` cite ce que le template utilise ;
- `title` est un **signal**, `protected` parce que seul le template le lit, `readonly` parce qu'on ne remplacera jamais le signal lui-même (seulement sa valeur) ;
- il n'y a ni `standalone: true`, ni `changeDetection` : ce sont les valeurs par défaut.

### 4. Démarrer le serveur de développement

```powershell
cd web
```

```powershell
npm start
```

`npm start` lance `ng serve` : la CLI compile l'application, la sert sur `http://localhost:4200/` et recompile à chaque fichier enregistré. Le terminal doit se terminer par :

```
Watch mode enabled. Watching for file changes...
  ➜  Local:   http://localhost:4200/
```

Ouvrez cette adresse : vous voyez la page d'accueil générée, « Hello, chronos-web ». Laissez le serveur tourner et ouvrez un second terminal dans `src/web` pour la suite. `Ctrl+C` l'arrête.

Documentation : [`ng serve`](https://angular.dev/cli/serve)

### 5. Remplacer la page d'accueil

Le template généré, `src/app/app.html`, fait plus de 300 lignes de démonstration. Remplacez **tout** son contenu par :

```html
<header>
  <h1>{{ titre() }}</h1>
</header>

<main>
  <p>Bienvenue dans Chronos.</p>
</main>
```

Dans `app.ts`, renommez le signal et retirez `RouterOutlet`, que le template n'utilise plus (il reviendra au [module 15](15-routage.md)) :

```ts
import { Component, signal } from '@angular/core';

@Component({
  imports: [],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly titre = signal('Chronos');
}
```

`{{ titre() }}` est une **interpolation** : le template appelle le signal et affiche sa valeur. Notez les parenthèses — `{{ titre }}` afficherait le texte de la fonction, pas « Chronos ».

Dans `src/index.html`, passez la langue à `fr` et donnez son nom à l'onglet :

```html
<html lang="fr">
<head>
  <meta charset="utf-8">
  <title>Chronos</title>
```

L'attribut `lang` n'est pas décoratif : un lecteur d'écran s'en sert pour choisir sa prononciation.

Lancez maintenant les tests :

```powershell
npm test
```

Un test échoue : `app.spec.ts` cherchait encore « Hello, chronos-web » dans le `<h1>`. Corrigez son second test pour qu'il décrive la page actuelle :

```ts
  it('affiche le nom de l\'application', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Chronos');
  });
```

Dans un terminal, `npm test` reste en mode surveillance (`Watching for file changes...`) et relance les tests à chaque enregistrement ; la touche `q` le quitte. Les tests sont le sujet du [module 20](20-tester-l-interface.md) : d'ici là, on se contente de les garder verts.

### 6. Générer le premier composant métier

```powershell
npx ng generate component collaborateurs/liste-collaborateurs
```

La CLI crée le dossier `src/app/collaborateurs/liste-collaborateurs/` et quatre fichiers : `liste-collaborateurs.ts`, `.html`, `.scss` et `.spec.ts`. La classe s'appelle `ListeCollaborateurs` et son sélecteur `app-liste-collaborateurs`. Le préfixe `app-` évite toute collision avec une balise HTML existante ou future.

Chronos range son code **par fonctionnalité** : tout ce qui concerne les collaborateurs ira dans `src/app/collaborateurs/`, les projets dans `projets/`, et ainsi de suite. On ne crée pas de dossiers `components/` ou `services/` ([guide de style](https://angular.dev/style-guide)).

Utilisez le composant dans `app.html`, à la place du paragraphe de bienvenue :

```html
<main>
  <app-liste-collaborateurs />
</main>
```

Enregistrez : la compilation échoue sur `NG8001: 'app-liste-collaborateurs' is not a known element`. Le composant `App` ne connaît pas encore cette balise. Citez la classe dans ses `imports` :

```ts
import { ListeCollaborateurs } from './collaborateurs/liste-collaborateurs/liste-collaborateurs';

@Component({
  imports: [ListeCollaborateurs],
  // …
})
```

La page affiche « liste-collaborateurs works! ».

Documentation : [`ng generate component`](https://angular.dev/cli/generate/component)

### 7. Décrire et afficher les collaborateurs

Créez `src/app/collaborateurs/collaborateur.ts`, le type des données affichées :

```ts
export interface Collaborateur {
  id: number;
  matricule: string;
  nom: string;
  prenom: string;
  email: string;
  categorie: 'Consultant' | 'Interne';
  poste: string;
  dateEntree: string;
  dateSortie: string | null;
}
```

Il reprend la forme du JSON que renvoie l'API : identifiant numérique, énumération sous forme de chaîne, dates au format `"2026-05-04"`. Ce type est provisoire ; le module 16 l'alignera exactement sur le DTO de l'API.

Dans `liste-collaborateurs.ts`, placez quelques collaborateurs dans un signal :

```ts
import { Component, signal } from '@angular/core';
import { Collaborateur } from '../collaborateur';

@Component({
  imports: [],
  selector: 'app-liste-collaborateurs',
  styleUrl: './liste-collaborateurs.scss',
  templateUrl: './liste-collaborateurs.html',
})
export class ListeCollaborateurs {
  protected readonly collaborateurs = signal<Collaborateur[]>([
    { id: 1, matricule: 'C0001', nom: 'Martin', prenom: 'Camille', email: 'camille.martin@chronos.example', categorie: 'Consultant', poste: 'Consultante confirmée', dateEntree: '2021-09-01', dateSortie: null },
    { id: 2, matricule: 'C0002', nom: 'Bernard', prenom: 'Louis', email: 'louis.bernard@chronos.example', categorie: 'Consultant', poste: 'Consultant junior', dateEntree: '2025-02-03', dateSortie: null },
    { id: 3, matricule: 'C0003', nom: 'Dubois', prenom: 'Inès', email: 'ines.dubois@chronos.example', categorie: 'Consultant', poste: 'Manager', dateEntree: '2018-04-16', dateSortie: null },
    { id: 4, matricule: 'I0001', nom: 'Marchand', prenom: 'Hugo', email: 'hugo.marchand@chronos.example', categorie: 'Interne', poste: 'Gestionnaire RH', dateEntree: '2019-01-07', dateSortie: null },
    { id: 5, matricule: 'C0004', nom: 'Petit', prenom: 'Sarah', email: 'sarah.petit@chronos.example', categorie: 'Consultant', poste: 'Consultante senior', dateEntree: '2017-11-02', dateSortie: '2026-03-31' },
  ]);
}
```

Puis remplacez `liste-collaborateurs.html` :

```html
<h2>Collaborateurs</h2>

<ul>
  @for (c of collaborateurs(); track c.id) {
    <li>
      <strong>{{ c.nom }}</strong> {{ c.prenom }} — {{ c.poste }} ({{ c.matricule }})
    </li>
  } @empty {
    <li>Aucun collaborateur.</li>
  }
</ul>
```

Cinq lignes s'affichent. `@for` parcourt la valeur du signal ; `track c.id` identifie chaque ligne par l'identifiant du collaborateur.

### 8. Filtrer par nom

Le filtre demande trois ajouts dans la classe : un signal pour le texte saisi, un signal calculé pour la liste filtrée, et une méthode pour vider le filtre.

```ts
import { Component, computed, signal } from '@angular/core';
```

```ts
  protected readonly filtre = signal('');

  protected readonly collaborateursFiltres = computed(() => {
    const texte = this.filtre().trim().toLocaleLowerCase('fr');

    return this.collaborateurs().filter((c) =>
      c.nom.toLocaleLowerCase('fr').includes(texte),
    );
  });

  protected effacerFiltre(): void {
    this.filtre.set('');
  }
```

`collaborateursFiltres` lit deux signals, `filtre` et `collaborateurs`. Angular le recalculera dès que l'un des deux changera, sans que vous ayez à le demander.

Dans le template, ajoutez le champ de saisie et faites porter la boucle sur la liste filtrée :

```html
<h2>Collaborateurs</h2>

<label for="filtre">Filtrer par nom</label>
<input id="filtre" type="search" #champ [value]="filtre()" (input)="filtre.set(champ.value)" />
<button type="button" [disabled]="filtre() === ''" (click)="effacerFiltre()">Effacer</button>

@let resultats = collaborateursFiltres();

<p>{{ resultats.length }} collaborateur(s) sur {{ collaborateurs().length }}</p>

<ul>
  @for (c of resultats; track c.id) {
    <li>
      <strong>{{ c.nom }}</strong> {{ c.prenom }} — {{ c.poste }} ({{ c.matricule }})
    </li>
  } @empty {
    <li>Aucun collaborateur ne correspond à « {{ filtre() }} ».</li>
  }
</ul>
```

Tout le vocabulaire du module est là :

- `#champ` nomme l'élément `<input>` pour le reste du template ;
- `(input)="filtre.set(champ.value)"` : à chaque frappe, la valeur du champ est rangée dans le signal ;
- `[value]="filtre()"` : dans l'autre sens, le champ affiche la valeur du signal — c'est ce qui le vide quand on clique sur « Effacer » ;
- `[disabled]="filtre() === ''"` désactive le bouton tant qu'il n'y a rien à effacer ;
- `@let resultats = …` évite d'appeler deux fois `collaborateursFiltres()` et donne un nom lisible au résultat.

Tapez `mar` : il reste Martin et Marchand, et le compteur indique « 2 collaborateur(s) sur 5 ». Tapez `zzz` : le bloc `@empty` s'affiche.

### 9. Afficher ou masquer les collaborateurs sortis

Sarah Petit a quitté la société en mars 2026. Par défaut, la liste ne doit montrer que les collaborateurs présents. Ajoutez un signal booléen et tenez-en compte dans le calcul :

```ts
  protected readonly afficherSortis = signal(false);

  protected readonly collaborateursFiltres = computed(() => {
    const texte = this.filtre().trim().toLocaleLowerCase('fr');
    const avecSortis = this.afficherSortis();

    return this.collaborateurs().filter(
      (c) =>
        (avecSortis || c.dateSortie === null) &&
        c.nom.toLocaleLowerCase('fr').includes(texte),
    );
  });

  protected basculerSortis(): void {
    this.afficherSortis.update((valeur) => !valeur);
  }
```

`update` convient ici mieux que `set` : la nouvelle valeur dépend de l'ancienne.

Dans le template, sous le bouton « Effacer » :

```html
<button type="button" (click)="basculerSortis()">
  @if (afficherSortis()) {
    Masquer les collaborateurs sortis
  } @else {
    Afficher les collaborateurs sortis
  }
</button>
```

Et sur la ligne de la liste :

```html
    <li [class.sorti]="c.dateSortie !== null">
```

Avec, dans `liste-collaborateurs.scss` :

```scss
.sorti {
  color: gray;
  text-decoration: line-through;
}
```

La liste affiche quatre collaborateurs ; un clic sur le bouton en montre cinq, dont une ligne barrée. Ce style ne s'applique qu'au template de ce composant : Angular isole les styles de chaque composant ([styles des composants](https://angular.dev/guide/components/styling)).

### 10. Fixer les versions de Node.js et de TypeScript

**Node.js.** Rien, pour l'instant, n'empêche un collègue de lancer le projet avec une version de Node.js qu'Angular ne prend pas en charge : `ng new` ne crée ni champ `engines` ni champ `devEngines`. Ajoutez dans `package.json`, au même niveau que `dependencies`, le bloc essayé au module 00 :

```json
  "devEngines": {
    "runtime": {
      "name": "node",
      "version": "^24.15.0",
      "onFail": "error"
    }
  },
```

npm vérifie ce champ avant `npm install`, `npm ci` et `npm run` : avec une autre version, la commande s'arrête sur l'erreur `EBADDEVENGINES`, qui cite la version attendue et la version trouvée. La vérification ne porte que sur les commandes `npm` : préférez donc `npm start`, `npm run build` et `npm test` à l'appel direct de `ng`. Au [module 20](20-tester-l-interface.md), l'intégration continue lira la version de Node.js à installer dans ce même fichier.

Documentation : [`devEngines` dans `package.json`](https://docs.npmjs.com/cli/v11/configuring-npm/package-json)

**TypeScript.** La ligne `"typescript": "~6.0.2"` de `package.json` n'est pas à toucher. La dernière version de TypeScript est la 7, et Angular 22 n'accepte que `>=6.0.0 <6.1.0`. Le piège est sournois : `npm install typescript@latest` **réussit**, avec de simples avertissements, et c'est la compilation suivante qui échoue sur un message sans rapport apparent :

```
✘ [ERROR] Angular compilation initialization failed. [plugin angular-compiler]
  TypeError: Cannot read properties of undefined (reading 'Error')
```

Si vous rencontrez cette erreur, regardez d'abord `git diff package.json`. On ne change la version de TypeScript qu'en même temps que celle d'Angular, avec `ng update`.

### 11. Compiler et enregistrer

```powershell
npm run build
```

`ng build` produit la version de production dans `dist/chronos-web` et affiche la taille des fichiers générés — de l'ordre de 210 kB pour `main-….js` à ce stade.

```powershell
git status --short
```

`src/web/` apparaît comme nouveau dossier ; `node_modules/`, `dist/` et `.angular/` n'y figurent pas, grâce au `.gitignore` que la CLI a créé dans `src/web`. `package-lock.json`, lui, **se versionne** : c'est lui qui garantit à tous les mêmes versions. Validez votre travail.

## À vous

1. **Élargir la recherche.** Faites porter le filtre sur le nom, le prénom et le matricule. Une seule expression doit changer ; laquelle, et pourquoi le template n'a-t-il pas à être modifié ?

2. **Ignorer les accents.** Taper `ines` doit trouver « Inès ». Cherchez du côté de `String.prototype.normalize`. Écrivez la fonction de comparaison dans un fichier à part de `src/app/commun/`, et donnez-lui un nom qui dit ce qu'elle fait — pas `utils.ts`.

3. **Compter par catégorie.** Affichez sous le compteur « 3 consultants, 1 interne », calculé sur la liste filtrée. Aucun nouveau signal modifiable n'est nécessaire. Gérez le singulier et le pluriel dans la classe, pas dans le template.

4. **Trier.** Ajoutez un bouton qui bascule entre le tri par nom et le tri par date d'entrée. Attention : `Array.prototype.sort` modifie le tableau sur lequel on l'appelle. Que se passe-t-il si vous triez directement le tableau contenu dans le signal `collaborateurs` ? Corrigez sans modifier ce tableau.

5. **Une deuxième ressource.** Générez un composant `ListeProjets` dans `src/app/projets/`, avec un type `Projet` (code, libellé, nature, statut, client éventuel) et quatre ou cinq projets en dur, dont un projet interne et un projet terminé. Affichez le statut en toutes lettres avec un bloc `@switch`, et proposez un filtre par statut. Montrez les deux listes l'une sous l'autre dans `App`.

6. **Voir OnPush à l'œuvre.** Dans `App`, ajoutez un champ ordinaire `message = 'avant'`, affichez-le, et modifiez-le au bout de deux secondes avec `setTimeout`. Prédisez, *avant d'exécuter*, ce que montrera l'écran. Vérifiez. Transformez ensuite le champ en signal et recommencez. Retirez l'expérience une fois comprise.

## Critères de réussite

- `npm run build` et `npm test` se terminent sans erreur dans `src/web`.
- `http://localhost:4200/` affiche la liste ; taper `mar` ne laisse que Martin et Marchand ; le bouton montre et masque Sarah Petit ; une recherche sans résultat affiche un message.
- `package.json` contient `"typescript": "~6.0.2"` et un bloc `devEngines`.
- Votre code ne contient ni `standalone: true`, ni `changeDetection`, ni `*ngIf`, ni `*ngFor`, ni `NgModule`.
- Tout état affiché par un template est un signal ; la liste filtrée est un `computed`.
- `git status` ne liste ni `node_modules/` ni `dist/` ; `package-lock.json` est versionné.
- Comparez avec la solution de référence : `git diff module-12 module-13`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `npm install typescript@latest` | Installe TypeScript 7, qu'Angular 22 refuse ; l'installation réussit et c'est la compilation qui casse. | Garder `~6.0.2` ; ne changer TypeScript qu'avec `ng update`. |
| `app.component.ts`, `AppComponent`, `src/assets/` | Nommage et arborescence d'avant 2025 : les tutoriels qui les montrent ne correspondent plus à ce que génère la CLI. | `app.ts`, `App`, `public/`. |
| `standalone: true`, `changeDetection: ChangeDetectionStrategy.OnPush` | Redondants en version 22 : ce sont les valeurs par défaut. | Ne rien écrire. |
| `@NgModule`, `AppModule`, `CommonModule` | Organisation en modules, déconseillée pour du code neuf. | Composants autonomes ; chaque composant cite ce qu'il utilise dans `imports`. |
| `*ngIf`, `*ngFor` | Dépréciés depuis la version 20. | `@if`, `@for` avec `track`. |
| `track $index` sur une liste qui change | Angular confond les lignes dès qu'on trie ou filtre. | `track c.id` : un identifiant stable. |
| `{{ titre }}` sans parenthèses | Affiche le code de la fonction au lieu de la valeur. | `{{ titre() }}`. |
| `this.collaborateurs().push(x)` | Le tableau est modifié, mais le signal ne le sait pas : rien ne se rafraîchit. | `this.collaborateurs.update((liste) => [...liste, x])` : un nouveau tableau. |
| Un second signal `listeFiltree` recopié à la main à chaque frappe | Deux états à tenir synchronisés : un jour, ils divergent. | `computed`. |
| Un champ ordinaire affiché dans le template | En OnPush et sans `zone.js`, sa modification hors d'un événement du template n'est pas affichée. | Un signal. |
| `zone.js`, `NgZone`, `provideZoneChangeDetection()` | Mécanisme antérieur à la version 21, absent d'une application neuve. | Signals. |
| `npx` ou `npm` refusés par PowerShell (« l'exécution de scripts est désactivée ») | La stratégie d'exécution de Windows bloque les scripts qu'installe npm. | Voir l'encadré du [module 00](00-environnement-de-travail.md) (étape 4) et la note pour Windows de la page [installation locale](https://angular.dev/tools/cli/setup-local). |
| Lancer `ng new` sans `--skip-git` hors d'un dépôt | La CLI crée un dépôt git et un premier commit que vous n'avez pas demandés. Dans un dépôt existant, elle s'en abstient, mais autant le dire explicitement. | `--skip-git`. |

La liste complète des pratiques obsolètes, avec les sources, est dans le [référentiel Angular 22](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Dans votre code, qu'est-ce qui est un état (un `signal`) et qu'est-ce qui est dérivé (un `computed`) ? Comment avez-vous décidé pour chaque valeur ?
- Le filtre tourne dans le navigateur, sur cinq collaborateurs. Que deviendra-t-il avec 5 000 collaborateurs et une API paginée ? Qui devra filtrer ?
- Pourquoi `track` est-il obligatoire, alors que la liste s'affiche correctement quoi qu'on lui donne ?
- `protected readonly` devant un signal : que protège chacun des deux mots, et de qui ?
- L'interface désactive le bouton « Effacer » quand il n'y a rien à effacer. Est-ce une règle de gestion ? Où se trouvent les vraies règles ?
- Vous avez reproduit `Collaborateur` en TypeScript alors qu'il existe en C#. Quels sont les risques de cette duplication, et comment les limiter ?

## Pour aller plus loin

- [Tutoriel interactif d'Angular](https://angular.dev/tutorials/learn-angular) : les mêmes notions, dans le navigateur, par petites étapes.
- [Les signals, version courte](https://angular.dev/essentials/signals), dans la série « Essentials » de la documentation.
- [Angular Language Service](https://angular.dev/tools/language-service) : complétion et erreurs dans les templates ; l'extension VS Code est celle que recommande `.vscode/extensions.json`.
- [Angular DevTools](https://angular.dev/tools/devtools) : inspecter l'arbre des composants et la valeur des signals dans le navigateur.
- [Guide de style](https://angular.dev/style-guide) : nommage des fichiers, organisation par fonctionnalité, `protected` et `readonly`.
- Suite dans le cours : le [module 14](14-composer-des-composants.md) découpe cet écran en composants et l'habille avec Angular Material.
