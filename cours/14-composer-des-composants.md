# Module 14 — Composer des composants

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md).
> Les commandes et le code de ce module ont été **exécutés le 12 novembre 2026** dans un conteneur Linux `node:24` : Node.js 24.22.3, Angular CLI 22.2.7, Angular 22.2.6, Angular Material et CDK 22.2.7, TypeScript 6.0.3, Vitest 5.1.0. `ng add @angular/material`, `ng build` et `ng test` y réussissent ; le rendu (thème, icônes, cartes) a été contrôlé dans un navigateur. **Non exécuté** : les commandes sous Windows / PowerShell. Les exercices n'ont pas de solution rédigée ; seuls leurs points techniques délicats (comportement de `linkedSignal`, `mat-table` alimenté par un signal, `effect` vers `localStorage`) ont été essayés. Signalez au mentor tout écart constaté.

À la fin du module 13, tout l'écran des collaborateurs tient dans un composant : le champ de filtre, la liste, la mise en forme d'une ligne. Cela suffit pour cinq lignes de texte. Mais Chronos doit aussi afficher la fiche d'un collaborateur, et les projets auront bientôt leur propre liste, avec leur propre champ de recherche. Ce module découpe l'écran en composants qui se parlent, puis l'habille avec Angular Material. Les données restent en dur.

## Objectifs

À la fin de ce module, vous saurez :

- découper un écran en composants et justifier chaque frontière ;
- faire descendre des données d'un parent vers un enfant avec `input()` et `input.required()` ;
- faire remonter un événement avec `output()`, et lier une valeur dans les deux sens avec `model()` et `[(…)]` ;
- choisir entre `computed`, `linkedSignal` et `effect` — et dire quand ne pas écrire d'`effect` ;
- projeter du contenu dans un composant avec `<ng-content />` ;
- mettre en forme une date et un pourcentage avec un pipe ;
- installer Angular Material 22 et utiliser ses composants : bouton, icône, champ de saisie, carte, liste.

## Prérequis

- [Module 13](13-premiere-application-angular.md) : l'application existe dans `src/web`, avec le composant `ListeCollaborateurs`, son filtre et le type `Collaborateur`.
- Des notions de SCSS suffisent : le thème tient en un fichier, que la CLI écrit pour vous.

## Règles de gestion

Aucune règle n'est mise en œuvre. Deux sont *affichées* : la fiche montre la quotité (RG-03) et la date de sortie (RG-02) d'un collaborateur, sans les contrôler — c'est le rôle de l'API, puis des formulaires du [module 17](17-formulaires.md).

## Notions

### Pourquoi découper

Un composant mérite d'exister quand il répond à au moins une de ces questions : cette partie de l'écran sera-t-elle **réutilisée** (le champ de recherche servira aux projets) ? A-t-elle un **rôle qu'on peut nommer** (« la carte d'un collaborateur ») ? Peut-on la **tester seule** ?

Le découpage retenu pour l'écran des collaborateurs :

```
ListeCollaborateurs            possède l'état : la liste, la recherche, la sélection
├── BarreDeRecherche           saisit un texte                     (dossier commun/)
├── CarteCollaborateur × n     affiche un collaborateur, signale qu'on l'a choisi
└── FicheCollaborateur         affiche le détail du collaborateur choisi
```

Un seul composant **possède** l'état ; les autres le reçoivent et signalent ce que fait l'utilisateur. Les données descendent, les événements remontent.

### Entrées : `input()`

Une **entrée** est une valeur que le parent fournit à l'enfant par une liaison de propriété. Côté enfant, c'est un signal en lecture seule.

```ts
readonly libelle = input('Rechercher');                          // facultative, avec valeur par défaut
readonly collaborateur = input.required<Collaborateur>();        // obligatoire
readonly active = input(false, { transform: booleanAttribute }); // transformée à la réception
```

- Oublier une entrée `required` dans le template du parent est une **erreur de compilation**.
- `transform` convertit la valeur reçue. `booleanAttribute` permet d'écrire `<app-carte active />` comme un attribut HTML ; `numberAttribute` convertit une chaîne en nombre.
- Une entrée se lit dans le template, dans un `computed` ou dans un `effect` — pas dans le constructeur, où elle n'a pas encore de valeur.

Documentation : [entrées d'un composant](https://angular.dev/guide/components/inputs)

### Sorties : `output()`

Une **sortie** est un événement que l'enfant émet et que le parent écoute avec des parenthèses, comme un `(click)`.

```ts
readonly selection = output<Collaborateur>();
// …
this.selection.emit(collaborateur);
```

```html
<app-carte-collaborateur (selection)="selection.set($event)" />
```

`$event` contient la valeur émise. On nomme une sortie par ce qui s'est passé, en camelCase, sans préfixe `on`, et en évitant les noms d'événements du DOM (`click`, `change`).

Documentation : [sorties d'un composant](https://angular.dev/guide/components/outputs)

### Liaison dans les deux sens : `model()`

Un champ de recherche reçoit un texte *et* le modifie. Plutôt qu'une entrée et une sortie jumelles, on déclare un **modèle** : une entrée que l'enfant a le droit d'écrire.

```ts
readonly recherche = model('');
```

Le parent le relie à l'un de ses signals avec la syntaxe `[(…)]` — des parenthèses dans des crochets :

```html
<app-barre-de-recherche [(recherche)]="recherche" />
```

On passe **le signal lui-même**, sans parenthèses d'appel. Quand l'enfant fait `recherche.set('mar')`, le signal du parent prend la même valeur. Angular crée aussi une sortie `rechercheChange`, que le [module 15](15-routage.md) utilisera.

Documentation : [modèles](https://angular.dev/guide/components/inputs#model-inputs), [liaison bidirectionnelle](https://angular.dev/guide/templates/two-way-binding)

### `computed`, `linkedSignal`, `effect`

Trois outils réagissent aux changements de signals. Ils ne sont pas interchangeables.

| Outil | Ce qu'il produit | Quand l'utiliser dans Chronos |
| --- | --- | --- |
| `computed` | Une valeur **dérivée**, en lecture seule. | La liste filtrée, le nom complet, « est sorti ». C'est le choix par défaut. |
| `linkedSignal` | Une valeur **modifiable**, qui se **recalcule** quand sa source change. | Le collaborateur sélectionné : l'utilisateur le choisit, mais il doit être abandonné s'il disparaît de la liste filtrée. |
| `effect` | Rien : il **exécute une action** vers l'extérieur d'Angular. | Écrire dans `localStorage`, tracer dans la console, piloter une bibliothèque de graphiques. |

La documentation est explicite sur le dernier : on **n'utilise pas** un `effect` pour propager un état vers un autre état. Un `effect` qui lit un signal pour en modifier un second est presque toujours un `computed` ou un `linkedSignal` mal écrit, avec à la clé des mises à jour en boucle ou en retard.

Documentation : [`linkedSignal`](https://angular.dev/guide/signals/linked-signal), [`effect`](https://angular.dev/guide/signals/effect)

### Projection de contenu

Un composant peut laisser un **emplacement** que son parent remplit, avec `<ng-content />` :

```html
<!-- fiche-collaborateur.html -->
<mat-card-actions><ng-content /></mat-card-actions>
```

```html
<!-- chez le parent -->
<app-fiche-collaborateur [collaborateur]="choisi">
  <button matButton type="button" (click)="selection.set(null)">Fermer</button>
</app-fiche-collaborateur>
```

Le bouton projeté appartient au parent : son `(click)` appelle le code du parent. La fiche n'a pas à savoir quelles actions on lui confie. Ne placez pas `<ng-content />` dans un `@if` : le contenu est créé même quand l'emplacement n'est pas affiché.

Documentation : [projection de contenu](https://angular.dev/guide/components/content-projection)

### Pipes

Un **pipe** transforme une valeur au moment de l'afficher : `{{ c.dateEntree | date: 'dd/MM/yyyy' }}`, `{{ c.quotite | percent }}`. Chaque pipe utilisé s'importe individuellement (`DatePipe`, `PercentPipe`, depuis `@angular/common`) dans les `imports` du composant — on n'importe plus `CommonModule` en bloc.

Sans autre réglage, les pipes formatent à l'américaine : `date` sans format donnerait « Sep 1, 2021 ». Le cours impose donc le format pour l'instant ; le [module 21](21-accessibilite-et-finitions.md) configurera le français une fois pour toutes.

Documentation : [pipes](https://angular.dev/guide/templates/pipes), [`DatePipe`](https://angular.dev/api/common/DatePipe)

### `protected`, `readonly` et membres publics

Le guide de style fixe trois habitudes, déjà appliquées au module 13 :

- `readonly` sur tout ce qu'Angular initialise — `input`, `output`, `model` — et sur les signals : on change leur valeur, jamais l'objet ;
- `protected` sur ce que seul le template lit ;
- **public** (aucun mot-clé) pour les entrées, sorties et modèles : le template du *parent* doit pouvoir les atteindre. Le projet généré active d'ailleurs `strictInputAccessModifiers`, qui refuse une liaison vers une entrée `protected`.

Documentation : [guide de style](https://angular.dev/style-guide)

### Angular Material

Angular Material est la bibliothèque de composants d'interface maintenue par l'équipe Angular : boutons, champs, tableaux, menus, déjà accessibles au clavier et au lecteur d'écran. Elle applique **Material Design 3**.

- **Le thème** se déclare en SCSS avec le mixin `mat.theme`, qui produit des variables CSS `--mat-sys-…` (couleurs, typographie). Vos propres styles s'en servent pour rester cohérents avec les composants.
- **Les composants s'importent un par un**, dans le composant qui s'en sert : `MatButtonModule` depuis `@angular/material/button`, `MatCardModule` depuis `@angular/material/card`. C'est la forme que montrent les pages d'API et les exemples officiels.
- **Les icônes** viennent d'une police, Material Symbols : `<mat-icon>search</mat-icon>` affiche la loupe.

Documentation : [premiers pas](https://material.angular.dev/guide/getting-started), [thème](https://material.angular.dev/guide/theming), [variables du thème dans vos composants](https://material.angular.dev/guide/theming-your-components)

## Pas à pas

### 1. Installer Angular Material

Dans `src/web` :

```powershell
npx ng add @angular/material
```

`ng add` installe un paquet puis exécute son script d'installation ([`ng add`](https://angular.dev/cli/add)). La commande pose deux questions :

- **« The package @angular/material@^22.2.7 will be installed and executed. Would you like to proceed? »** : `Y` ;
- **« Select a pair of starter prebuilt color palettes for your Angular Material theme »** : gardez `Azure/Blue`.

Regardez ensuite ce qui a changé :

```powershell
git status --short
```

| Fichier | Modification |
| --- | --- |
| `package.json`, `package-lock.json` | `@angular/material` et `@angular/cdk` ajoutés aux dépendances. |
| `src/index.html` | Liens vers les polices Roboto et Material Symbols (Google Fonts). |
| `src/styles.scss` | Le thème. |

Rien d'autre : ni `app.config.ts` ni `angular.json` ne sont touchés, et **`@angular/animations` n'est pas installé**. Ce paquet est déprécié ; Angular Material 22 anime ses composants en CSS. Un tutoriel qui vous fait ajouter `provideAnimations()` ou `BrowserAnimationsModule` date d'avant.

Le thème écrit dans `src/styles.scss` :

```scss
@use '@angular/material' as mat;

html {
  height: 100%;
  @include mat.theme(
    (
      color: (
        primary: mat.$azure-palette,
        tertiary: mat.$blue-palette,
      ),
      typography: Roboto,
      density: 0,
    )
  );
}

body {
  color-scheme: light;
  background-color: var(--mat-sys-surface);
  color: var(--mat-sys-on-surface);
  font: var(--mat-sys-body-medium);
  margin: 0;
  height: 100%;
}
```

`mat.theme` reçoit trois réglages — couleurs, police, densité — et déclare les variables `--mat-sys-…`. Le sélecteur `body` s'en sert aussitôt pour le fond, la couleur du texte et la police de toute l'application. `color-scheme: light` fixe le mode clair.

Vérifiez que `src/index.html` porte toujours `lang="fr"` et le titre « Chronos ». Relancez `npm start` : la police de la page a changé.

### 2. Un premier composant Material

Dans `liste-collaborateurs.ts`, importez le module des boutons :

```ts
import { MatButtonModule } from '@angular/material/button';

@Component({
  imports: [MatButtonModule],
  // …
})
```

Et dans le template, ajoutez l'attribut `matButton` au bouton qui montre les collaborateurs sortis :

```html
<button matButton="outlined" type="button" (click)="basculerSortis()">
```

Le bouton prend l'apparence Material. C'est toujours un `<button>` HTML : Material le décore, il ne le remplace pas. `matButton` accepte les apparences `text` (par défaut), `filled`, `tonal`, `outlined` et `elevated` ; `matIconButton` sert aux boutons qui ne contiennent qu'une icône.

Documentation : [boutons](https://material.angular.dev/components/button/overview)

### 3. Extraire la barre de recherche

Le champ de filtre servira à plusieurs listes : il va dans `src/app/commun/`.

```powershell
npx ng generate component commun/barre-de-recherche
```

`barre-de-recherche.ts` :

```ts
import { Component, input, model } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

@Component({
  imports: [MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  selector: 'app-barre-de-recherche',
  styleUrl: './barre-de-recherche.scss',
  templateUrl: './barre-de-recherche.html',
})
export class BarreDeRecherche {
  readonly libelle = input('Rechercher');
  readonly recherche = model('');
}
```

`barre-de-recherche.html` :

```html
<mat-form-field appearance="outline" subscriptSizing="dynamic">
  <mat-label>{{ libelle() }}</mat-label>
  <mat-icon matPrefix>search</mat-icon>
  <input matInput type="search" #champ [value]="recherche()" (input)="recherche.set(champ.value)" />
  @if (recherche() !== '') {
    <button
      matIconButton
      matSuffix
      type="button"
      aria-label="Effacer la recherche"
      (click)="recherche.set('')"
    >
      <mat-icon>close</mat-icon>
    </button>
  }
</mat-form-field>
```

Ce qu'il faut y lire :

- `<mat-form-field>` enveloppe un champ et lui apporte son libellé flottant, son contour, ses préfixe et suffixe ; `matInput` désigne le champ HTML qu'il enveloppe ;
- `<mat-label>` **est** le libellé du champ : inutile d'ajouter un `<label>` ;
- le bouton d'effacement ne contient qu'une icône : `aria-label` lui donne un nom pour les lecteurs d'écran. Une icône seule ne dit rien à qui ne la voit pas ;
- le composant ne sait pas ce qu'on filtre. Il saisit un texte, c'est tout : il est réutilisable.

Dans `ListeCollaborateurs`, renommez le signal `filtre` en `recherche`, supprimez la méthode `effacerFiltre`, ajoutez `BarreDeRecherche` aux `imports`, et remplacez dans le template le `<label>`, l'`<input>` et le bouton « Effacer » par :

```html
<app-barre-de-recherche libelle="Filtrer par nom" [(recherche)]="recherche" />
```

`libelle="Filtrer par nom"`, sans crochets, passe un texte fixe. `[(recherche)]="recherche"` relie le modèle de l'enfant au signal du parent. La liste se filtre comme avant, mais `ListeCollaborateurs` ne contient plus une ligne de code sur la saisie.

Documentation : [champ de formulaire](https://material.angular.dev/components/form-field/overview), [`matInput`](https://material.angular.dev/components/input/overview), [icônes](https://material.angular.dev/components/icon/overview)

### 4. La carte d'un collaborateur

```powershell
npx ng generate component collaborateurs/carte-collaborateur
```

`carte-collaborateur.ts` :

```ts
import { DatePipe } from '@angular/common';
import { booleanAttribute, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { Collaborateur } from '../collaborateur';

@Component({
  imports: [DatePipe, MatButtonModule, MatCardModule, MatIconModule],
  selector: 'app-carte-collaborateur',
  styleUrl: './carte-collaborateur.scss',
  templateUrl: './carte-collaborateur.html',
})
export class CarteCollaborateur {
  readonly collaborateur = input.required<Collaborateur>();
  readonly active = input(false, { transform: booleanAttribute });
  readonly selection = output<Collaborateur>();

  protected readonly nomComplet = computed(
    () => `${this.collaborateur().prenom} ${this.collaborateur().nom}`,
  );
  protected readonly estSorti = computed(() => this.collaborateur().dateSortie !== null);
}
```

`carte-collaborateur.html` :

```html
<mat-card appearance="outlined" [class.active]="active()">
  <mat-card-header>
    <mat-card-title>{{ nomComplet() }}</mat-card-title>
    <mat-card-subtitle>{{ collaborateur().poste }}</mat-card-subtitle>
  </mat-card-header>

  <mat-card-content>
    <p>
      {{ collaborateur().matricule }} — entrée le
      {{ collaborateur().dateEntree | date: 'dd/MM/yyyy' }}
    </p>
    @if (estSorti()) {
      <p class="sorti">Sortie le {{ collaborateur().dateSortie | date: 'dd/MM/yyyy' }}</p>
    }
  </mat-card-content>

  <mat-card-actions align="end">
    <button matButton type="button" (click)="selection.emit(collaborateur())">
      <mat-icon>visibility</mat-icon>
      Voir la fiche
    </button>
  </mat-card-actions>
</mat-card>
```

`carte-collaborateur.scss` :

```scss
:host {
  display: block;
}

.active {
  border-color: var(--mat-sys-primary);
  background-color: var(--mat-sys-primary-container);
}

.sorti {
  color: var(--mat-sys-error);
}
```

La carte reçoit un collaborateur, en dérive deux valeurs avec `computed`, et émet `selection` quand on clique. Elle ne modifie rien : elle **signale**, et laisse le parent décider. Ses couleurs viennent des variables du thème, pas de valeurs écrites en dur : changer de palette à l'étape 1 les changerait aussi. `:host` désigne la balise `<app-carte-collaborateur>` elle-même.

Lancez `npm test`. Le test généré pour la carte échoue :

```
NG0950: Input "collaborateur" is required but no value is available yet.
```

Le test crée le composant sans parent, donc sans personne pour fournir l'entrée obligatoire. Dans `carte-collaborateur.spec.ts`, fournissez-la avant `whenStable()` :

```ts
    fixture = TestBed.createComponent(CarteCollaborateur);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('collaborateur', camille);
    await fixture.whenStable();
```

`camille` est une constante de type `Collaborateur` déclarée en tête du fichier de test. Vous aurez le même correctif à faire pour la fiche, à l'étape suivante.

Documentation : [cartes](https://material.angular.dev/components/card/overview), [erreur NG0950](https://angular.dev/errors/NG0950), [fournir une entrée dans un test](https://angular.dev/guide/testing/components-scenarios)

### 5. La fiche

La fiche affiche la quotité. Ajoutez-la au type `Collaborateur` :

```ts
  quotite: number;
```

La compilation signale aussitôt les cinq collaborateurs en dur à compléter : donnez `0.8` à Camille, `0.5` à Hugo, `1` aux autres. La quotité est une fraction, comme dans l'API : `0.8` pour 80 %.

```powershell
npx ng generate component collaborateurs/fiche-collaborateur
```

`fiche-collaborateur.ts` :

```ts
import { DatePipe, PercentPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { Collaborateur } from '../collaborateur';

@Component({
  imports: [DatePipe, PercentPipe, MatCardModule, MatIconModule, MatListModule],
  selector: 'app-fiche-collaborateur',
  styleUrl: './fiche-collaborateur.scss',
  templateUrl: './fiche-collaborateur.html',
})
export class FicheCollaborateur {
  readonly collaborateur = input.required<Collaborateur>();
}
```

`fiche-collaborateur.html` :

```html
@let c = collaborateur();

<mat-card>
  <mat-card-header>
    <mat-card-title>{{ c.prenom }} {{ c.nom }}</mat-card-title>
    <mat-card-subtitle>{{ c.poste }} — {{ c.categorie }}</mat-card-subtitle>
  </mat-card-header>

  <mat-card-content>
    <mat-list>
      <mat-list-item>
        <mat-icon matListItemIcon>badge</mat-icon>
        <span matListItemTitle>Matricule</span>
        <span matListItemLine>{{ c.matricule }}</span>
      </mat-list-item>
      <mat-list-item>
        <mat-icon matListItemIcon>mail</mat-icon>
        <span matListItemTitle>Adresse électronique</span>
        <span matListItemLine>{{ c.email }}</span>
      </mat-list-item>
      <mat-list-item>
        <mat-icon matListItemIcon>event</mat-icon>
        <span matListItemTitle>Date d'entrée</span>
        <span matListItemLine>{{ c.dateEntree | date: 'dd/MM/yyyy' }}</span>
      </mat-list-item>
      <mat-list-item>
        <mat-icon matListItemIcon>schedule</mat-icon>
        <span matListItemTitle>Quotité</span>
        <span matListItemLine>{{ c.quotite | percent }}</span>
      </mat-list-item>
    </mat-list>
  </mat-card-content>

  <mat-card-actions align="end">
    <ng-content />
  </mat-card-actions>
</mat-card>
```

`| percent` affiche `80%` pour `0.8`. `<ng-content />` réserve l'emplacement des actions, que le parent remplira.

Documentation : [listes](https://material.angular.dev/components/list/overview)

### 6. Assembler : la sélection

Reste à dire, dans `ListeCollaborateurs`, quel collaborateur est sélectionné. C'est un état que l'utilisateur modifie — donc pas un `computed`. Mais il dépend de la liste filtrée : si la recherche fait disparaître le collaborateur sélectionné, sa fiche doit se fermer. C'est le cas d'usage de `linkedSignal`.

```ts
import { Component, computed, linkedSignal, signal } from '@angular/core';
```

```ts
  protected readonly selection = linkedSignal<Collaborateur[], Collaborateur | null>({
    source: this.collaborateursFiltres,
    computation: (liste, precedent) =>
      liste.find((c) => c.id === precedent?.value?.id) ?? null,
  });
```

`selection` se manipule comme un signal ordinaire (`set`, `update`). En plus, chaque fois que `collaborateursFiltres` change, `computation` est rappelée avec la nouvelle liste et l'ancienne valeur : on conserve la sélection si le collaborateur est toujours là, sinon on revient à `null`.

Le template complet de `liste-collaborateurs.html` :

```html
<h2>Collaborateurs</h2>

<div class="outils">
  <app-barre-de-recherche libelle="Filtrer par nom" [(recherche)]="recherche" />

  <button matButton="outlined" type="button" (click)="basculerSortis()">
    @if (afficherSortis()) {
      Masquer les collaborateurs sortis
    } @else {
      Afficher les collaborateurs sortis
    }
  </button>
</div>

@let resultats = collaborateursFiltres();

<p>{{ resultats.length }} collaborateur(s) sur {{ collaborateurs().length }}</p>

<div class="contenu">
  <div class="cartes">
    @for (c of resultats; track c.id) {
      <app-carte-collaborateur
        [collaborateur]="c"
        [active]="c.id === selection()?.id"
        (selection)="selection.set($event)"
      />
    } @empty {
      <p>Aucun collaborateur ne correspond à « {{ recherche() }} ».</p>
    }
  </div>

  @if (selection(); as choisi) {
    <app-fiche-collaborateur [collaborateur]="choisi">
      <button matButton type="button" (click)="selection.set(null)">Fermer</button>
    </app-fiche-collaborateur>
  }
</div>
```

Les `imports` du composant deviennent `[BarreDeRecherche, CarteCollaborateur, FicheCollaborateur, MatButtonModule]`. Remplacez le contenu de `liste-collaborateurs.scss` (la classe `.sorti` a déménagé dans la carte) :

```scss
.outils {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 16px;
}

.contenu {
  display: flex;
  align-items: flex-start;
  gap: 24px;
}

.cartes {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 16px;
  flex: 1;
}
```

`@if (selection(); as choisi)` range dans `choisi` la valeur testée : à l'intérieur du bloc, TypeScript sait qu'elle n'est pas `null`.

Essayez : cliquez sur « Voir la fiche » de Camille Martin, la fiche s'ouvre et sa carte se colore. Tapez `mar` : Camille est toujours dans la liste, la fiche reste. Tapez `dub` : elle disparaît de la liste, la fiche se ferme.

### 7. Compiler

```powershell
npm run build
```

La compilation réussit, avec un avertissement nouveau :

```
▲ [WARNING] bundle initial exceeded maximum budget. Budget 500.00 kB was not met by 14.67 kB with a total of 514.67 kB.
```

`angular.json` fixe des **budgets** de taille : un avertissement au-delà de 500 kB pour le chargement initial, une erreur au-delà de 1 Mo. Angular Material vient de faire franchir le premier seuil. Ne modifiez rien pour l'instant : le [module 15](15-routage.md) sortira une partie du code du chargement initial, et vous déciderez alors quoi faire de ce seuil.

Documentation : [budgets de taille](https://angular.dev/tools/cli/build#configuring-size-budgets)

## À vous

1. **Extraire le compteur.** Faites de la ligne « 2 collaborateur(s) sur 5 » un composant `CompteurDeResultats` dans `commun/`, avec deux entrées obligatoires et un `computed` qui accorde le texte (« 1 collaborateur sur 5 », « Aucun collaborateur »). Le mot « collaborateur » doit pouvoir être remplacé par « projet ».

2. **Prédire `linkedSignal`.** Affichez les collaborateurs sortis, ouvrez la fiche de Sarah Petit, puis masquez les sortis. Prédisez *avant de cliquer* ce que devient la fiche. Remplacez ensuite le `linkedSignal` par un `signal` ordinaire et refaites l'essai : quel défaut apparaît ?

3. **Une vue en tableau.** Créez `TableauCollaborateurs`, qui reçoit la liste filtrée et émet la même sortie `selection` que la carte, mais affiche un `<table mat-table>` (nom, prénom, poste, date d'entrée, quotité). Ajoutez à `ListeCollaborateurs` un bouton qui bascule entre cartes et tableau. Indices : `MatTableModule`, la [documentation du tableau](https://material.angular.dev/components/table/overview). Vous y rencontrerez `*matCellDef` : la syntaxe `*` n'a pas disparu d'Angular, seuls `*ngIf` et `*ngFor` sont dépréciés.

4. **Mémoriser un réglage.** Faites en sorte que le choix « afficher les sortis » survive au rechargement de la page, en l'écrivant dans `localStorage` avec un `effect`. Pourquoi est-ce un bon usage d'`effect`, alors que filtrer la liste dans un `effect` en serait un mauvais ?

5. **Les projets.** Reprenez la liste de projets de l'exercice 5 du module 13 : réutilisez `BarreDeRecherche` telle quelle, et créez `CarteProjet`. Un projet facturable affiche son client, un projet interne n'en a pas (RG-11) ; un projet terminé ou annulé doit se distinguer visuellement (RG-13 interdira d'y affecter quelqu'un). Qu'avez-vous dû modifier dans `commun/` ? Si la réponse n'est pas « rien », le composant n'était pas assez générique — ou l'était trop peu pour deux usages : discutez-le.

6. **Le thème.** Changez la palette principale dans `styles.scss`, puis passez `color-scheme` à `light dark` et basculez votre système en mode sombre. Quels éléments de votre écran ne suivent pas ? Corrigez-les en remplaçant toute couleur écrite en dur par une variable `--mat-sys-…`.

## Critères de réussite

- `npm run build` réussit (l'avertissement de budget est attendu) et `npm test` passe, y compris les tests générés pour les nouveaux composants.
- La recherche filtre les cartes ; « Voir la fiche » ouvre la fiche et colore la carte ; la fiche se ferme quand le collaborateur sort de la liste filtrée, et par le bouton « Fermer ».
- La fiche de Camille Martin affiche « 01/09/2021 » et « 80% ».
- `BarreDeRecherche` ne contient aucune référence aux collaborateurs.
- Aucun `@Input()`, `@Output()`, `EventEmitter`, `CommonModule`, `ngOnChanges` ni `provideAnimations()` dans votre code ; aucun `effect` qui modifie un signal.
- Toutes les entrées, sorties et modèles sont `readonly` ; ce que seul le template lit est `protected`.
- Comparez avec la solution de référence : `git diff module-13 module-14`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `@Input() collaborateur!: Collaborateur;` | API à décorateurs : la valeur n'est pas un signal, on ne peut pas en dériver un `computed`. | `input()` / `input.required()`. |
| `@Output() selection = new EventEmitter<…>()` | Même constat ; `EventEmitter` n'a plus de raison d'être dans du code neuf. | `output()`. |
| `ngOnChanges` pour recalculer une valeur quand une entrée change | Code impératif, facile à oublier pour une entrée de plus. | `computed` sur l'entrée. |
| Un `effect` qui recopie un signal dans un autre | La documentation le déconseille : boucles, mises à jour en retard, erreurs `ExpressionChangedAfterItHasBeenChecked`. | `computed`, ou `linkedSignal` si la valeur doit rester modifiable. |
| Lire une entrée `required` dans le constructeur | Elle n'a pas encore de valeur : erreur NG0950. | La lire dans le template, un `computed` ou un `effect`. |
| Modifier l'objet reçu en entrée (`this.collaborateur().nom = …`) | L'enfant change en douce l'état du parent, qui n'en est pas prévenu. | Émettre une sortie ; le parent met son état à jour. |
| `[(recherche)]="recherche()"` | On passe la valeur au lieu du signal : erreur `NG5002: Unsupported expression in a two-way binding`. | `[(recherche)]="recherche"`. |
| `imports: [CommonModule]` | Importe tout pour un pipe. | `imports: [DatePipe]`. |
| `provideAnimations()`, `BrowserAnimationsModule` | `@angular/animations` est déprécié ; Material 22 ne l'utilise pas et `ng add` ne l'installe pas. | Ne rien ajouter. |
| `mat.define-theme`, `mat.define-light-theme`, `mat.define-dark-theme` | API de thème antérieures à la version 19 ; le guide actuel renvoie à la documentation de la version 18 pour elles. | `mat.theme`. |
| `color="primary"` sur un bouton | Sans effet avec un thème Material 3. | L'apparence (`matButton="filled"`) ou les variables du thème. |
| `mat-raised-button`, `mat-icon-button`… | Anciens sélecteurs, encore acceptés ; la documentation présente désormais `matButton` et `matIconButton`. | `matButton="…"`, `matIconButton`. |
| Police « Material Icons » ajoutée à la main | `ng add` installe Material Symbols, que `<mat-icon>` utilise par défaut ; mélanger les deux fait retomber sur l'ancienne. | Garder les liens écrits par `ng add`. |
| Un bouton qui ne contient qu'une icône, sans `aria-label` | Invisible pour un lecteur d'écran. | `aria-label="Effacer la recherche"`. |
| Un composant par balise | Dix fichiers pour un écran, des entrées et sorties à chaque étage. | Découper quand il y a réutilisation, rôle nommable ou besoin de tester seul. |

La liste complète des pratiques obsolètes, avec les sources, est dans le [référentiel Angular 22](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Reprenez votre découpage : pour chaque composant, lequel des trois critères (réutilisation, rôle, test) justifie son existence ? Y en a-t-il un de trop ?
- Qui possède l'état « collaborateur sélectionné » ? Que faudrait-il changer pour que l'adresse de la page le reflète ?
- `CarteCollaborateur` émet `selection` au lieu d'ouvrir elle-même la fiche. Qu'y gagne-t-on ? Dans quel cas serait-ce une complication inutile ?
- Montrez un endroit où vous avez hésité entre `computed` et `linkedSignal`. Qu'est-ce qui a tranché ?
- Le thème charge ses polices depuis les serveurs de Google à chaque visite. Est-ce acceptable pour une application RH interne ? Quelles questions poseriez-vous avant la mise en production ?
- Angular Material impose une apparence. Qu'est-ce que le projet y gagne, et qu'y perd-il face à des composants écrits à la main ?

## Pour aller plus loin

- [Angular Aria](https://angular.dev/guide/aria/overview) : des composants accessibles sans style imposé, pour qui veut sa propre charte graphique.
- [Schémas de génération d'Angular Material](https://material.angular.dev/guide/schematics) : `ng generate @angular/material:table`, `:navigation`… produisent des composants complets à étudier.
- [Élément hôte](https://angular.dev/guide/components/host-elements) : lier des classes et des événements à la balise du composant lui-même.
- [`viewChild` et requêtes](https://angular.dev/api/core/viewChild) : atteindre un élément ou un composant enfant depuis la classe.
- Suite dans le cours : le [module 15](15-routage.md) donne une adresse à chaque écran — et la sélection quittera le composant pour l'URL.
