# Module 21 — Accessibilité et finitions

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md).
> Ce module a été **rejoué sur la solution de référence le 12 novembre 2026** (Angular 22.2, Angular Material, CDK et Angular Aria 22.2.7, Playwright 1.65.0, @axe-core/playwright 4.14, Fontsource Roboto 5.3 et Material Symbols Outlined 5.3), sur les écrans réels des modules 16 à 19 : `ng build` sans avertissement, 262 tests unitaires et seuils de couverture tenus, 33 parcours `ng e2e` (axe sans violation sur tous les écrans, dialogues et états d'erreur ; clavier ; polices), et un parcours au clavier dans Chromium contre l'API réelle (connexion, grille, enregistrement, soumission). **N'ont pas été exécutés** : Lighthouse et l'essai avec un lecteur d'écran (NVDA, Narrateur). Un contrôle automatique sans violation ne prouve pas qu'un écran est accessible : l'essai au lecteur d'écran reste à faire par vous.

Le cahier des charges tient en deux lignes sur ce sujet (§ 6) : l'interface est en français, dates et nombres compris, et elle est « utilisable au clavier et avec un lecteur d'écran ». Chronos affiche aujourd'hui `0.5` jour et `133%`, les libellés du paginateur sont en anglais, et sa feuille de temps se remplit bien plus vite à la souris qu'au clavier. Ce module corrige cela, puis soigne ce qu'on néglige d'ordinaire : les écrans vides, les attentes, le poids de l'application.

## Objectifs

À la fin de ce module, vous saurez :

- afficher dates, nombres et pourcentages au format français, dans vos écrans comme dans ceux d'Angular Material ;
- structurer un tableau de données et un formulaire pour qu'un lecteur d'écran les restitue correctement ;
- gérer le focus : le rendre visible, le placer après une navigation, le contenir dans un dialogue ;
- annoncer un changement qui ne se voit qu'à l'écran ;
- rendre une grille de saisie parcourable aux flèches avec Angular Aria ;
- vérifier l'accessibilité au clavier, avec axe, et avec un lecteur d'écran ;
- traiter les états vides et les chargements, différer un bloc, lire un budget de taille.

## Prérequis

- [Module 18](18-ecrans-metier.md) : la feuille de temps hebdomadaire et le plan de charge.
- [Module 19](19-authentification-cote-interface.md) : l'écran de connexion. [Module 20](20-tester-l-interface.md) : Playwright et le parcours de bout en bout.
- Savoir ouvrir les outils de développement du navigateur.

## Règles de gestion

Aucune nouvelle. Ce module met en œuvre les exigences non fonctionnelles « Langue » et « Accessibilité » du cahier des charges (§ 6). Il rend perceptibles par tous deux signalements : la surcharge (RG-24) et le dépassement d'une journée (RG-31).

## Notions

### De quoi parle-t-on ?

Un collaborateur aveugle utilise Chronos avec un **lecteur d'écran**, qui lui lit la page et lui annonce où il se trouve. Un autre, qui ne peut pas tenir une souris, ne dispose que du **clavier**. Un troisième distingue mal le rouge du vert. Une interface accessible fonctionne pour les trois — et pour vous, le jour où vous saisissez vos temps d'une main.

Deux référentiels disent ce qu'« accessible » veut dire :

| Référentiel | Version en vigueur | À retenir |
| --- | --- | --- |
| **WCAG** (W3C) | 2.2, recommandation du W3C | La référence internationale. Des critères classés en trois niveaux ; on vise le niveau AA. |
| **RGAA** (État français) | 4.1.2 | 106 critères en 13 thématiques, avec leur méthode de test. S'appuie sur WCAG 2.1. Une version 5 est annoncée pour fin 2026. |

Documentation : [WCAG 2.2](https://www.w3.org/TR/WCAG22/), [RGAA](https://accessibilite.numerique.gouv.fr/), [critères et tests du RGAA](https://accessibilite.numerique.gouv.fr/methode/criteres-et-tests/)

### HTML d'abord, ARIA ensuite

Un `<button>` reçoit le focus, s'active avec Entrée et Espace, et s'annonce « bouton ». Un `<div (click)="…">` ne fait rien de tout cela. La première règle est donc d'utiliser l'élément HTML qui correspond : `<button>`, `<a href>`, `<label>`, `<table>`, `<h2>`, `<nav>`, `<main>`.

**ARIA** est un jeu d'attributs (`role`, `aria-label`, `aria-live`…) qui décrit aux lecteurs d'écran ce que le HTML ne sait pas dire. ARIA ne **fait** rien : `role="button"` sur un `<div>` l'annonce comme un bouton, sans le rendre activable au clavier. Le guide du W3C le résume ainsi : un rôle est une promesse, et c'est à vous de la tenir.

Documentation : [guide des pratiques ARIA — à lire d'abord](https://www.w3.org/WAI/ARIA/apg/practices/read-me-first/), [accessibilité avec Angular](https://angular.dev/best-practices/a11y)

### Ce qu'Angular fournit

| Outil | Ce qu'il apporte |
| --- | --- |
| **Angular Material** | Des composants déjà accessibles : un `mat-form-field` relie tout seul le champ, son libellé et ses messages. |
| **CDK, paquet `a11y`** | Des briques : `LiveAnnouncer` (annoncer un message), `cdkTrapFocus` (retenir le focus dans une zone), `FocusMonitor` (savoir si un élément a reçu le focus au clavier ou à la souris). Installé avec Angular Material. |
| **Angular Aria** | Des directives sans apparence, qui apportent le comportement clavier et les attributs ARIA de composants complexes : grille, liste, menu, onglets, arbre… Stable depuis la v22. |

Documentation : [CDK `a11y`](https://material.angular.dev/cdk/a11y/overview), [Angular Aria](https://angular.dev/guide/aria/overview)

### La locale

La **locale** est le code qui désigne une langue et ses conventions : `fr`, `en-US`. Angular la porte dans le jeton `LOCALE_ID`, et ses *pipes* de formatage — `date`, `number`, `percent`, `currency` — s'y conforment. Par défaut, elle vaut `en-US`.

Afficher les dates en français et **traduire** l'application sont deux choses distinctes. Les libellés de Chronos sont écrits en français dans les templates, et c'est suffisant pour une application en une seule langue. Le paquet `@angular/localize` sert à produire la même application en plusieurs langues : le cours ne le met pas en œuvre.

Documentation : [formater selon la locale](https://angular.dev/guide/i18n/format-data-locale), [internationalisation](https://angular.dev/guide/i18n)

## Pas à pas

### 1. Passer l'application en français

Dans `angular.json`, déclarez la langue du projet, au même niveau que `architect` :

```json
"projects": {
  "chronos-web": {
    "i18n": {
      "sourceLocale": "fr"
    },
    "architect": { … }
  }
}
```

Arrêtez puis relancez `npm start`. Voici ce qui a été constaté avec ce seul réglage, sans installer `@angular/localize` :

- `LOCALE_ID` vaut `fr`, dans l'application servie, dans l'application compilée **et dans les tests** ;
- les données de la locale (noms des jours et des mois, séparateurs) sont ajoutées à l'application : un fichier de 2 ko ;
- la page compilée porte `<html lang="fr" dir="ltr">`, quoi que dise `src/index.html`.

Vous aviez déjà écrit `lang="fr"` dans `src/index.html` au module 13, et pour la même raison : c'est cet attribut qui dit au lecteur d'écran avec quelle voix lire la page. Sans lui, « Feuille de temps » est prononcé à l'anglaise. Les deux réglages disent maintenant la même chose.

Les *pipes* suivent aussitôt :

| Dans le template | Affiché |
| --- | --- |
| `{{ '2026-05-04' \| date: 'longDate' }}` | 4 mai 2026 |
| `{{ '2026-05-04' \| date: 'shortDate' }}` | 04/05/2026 |
| `{{ '2026-05-04' \| date: 'EEEE d MMMM' }}` | lundi 4 mai |
| `{{ 0.25 \| number: '1.0-2' }}` | 0,25 |
| `{{ 1234.5 \| number }}` | 1 234,5 |
| `{{ 4 / 3 \| percent }}` | 133 % |

Chaque *pipe* s'importe dans le composant qui l'utilise : `DatePipe`, `DecimalPipe`, `PercentPipe`, depuis `@angular/common`. Les `number` de `GrilleDeSaisie` et les `percent` d'`IndicateurCharge` et de `ConfirmationDeSurcharge`, que le module 18 laissait « à l'anglaise », sont corrigés sans y toucher. Les jours de la grille étaient déjà en français : `libelleJour`, dans `commun/dates.ts`, s'appuie sur `Intl.DateTimeFormat('fr-FR')`. Les deux voies se valent ; gardez `libelleJour` là où il est, et prenez le *pipe* `date` pour une date isolée dans un template, comme la date d'entrée d'une fiche.

Deux remarques pour vos tests. L'espace des milliers et celle qui précède `%` sont des **espaces insécables** (U+202F et U+00A0) : comparer à une chaîne tapée au clavier échouera. Et `percent` multiplie par 100 : il attend la fraction que renvoie l'API (`0.8`), pas `80`.

Angular Material suit la même locale : son jeton `MAT_DATE_LOCALE` reprend `LOCALE_ID` par défaut, et le calendrier commence alors la semaine le lundi (constaté avec `provideNativeDateAdapter()`). Les libellés du paginateur, eux, sont en anglais dans le code de Material. `src/app/commun/paginator-francais.ts` :

```ts
import { MatPaginatorIntl } from '@angular/material/paginator';

export function paginatorFrancais(): MatPaginatorIntl {
  const intl = new MatPaginatorIntl();
  intl.itemsPerPageLabel = 'Lignes par page';
  intl.firstPageLabel = 'Première page';
  intl.previousPageLabel = 'Page précédente';
  intl.nextPageLabel = 'Page suivante';
  intl.lastPageLabel = 'Dernière page';
  intl.getRangeLabel = (page, taille, total) => {
    if (total === 0) {
      return 'Aucun résultat';
    }
    const debut = page * taille + 1;
    const fin = Math.min((page + 1) * taille, total);
    return `${debut} à ${fin} sur ${total}`;
  };
  return intl;
}
```

Où le fournir ? Dans `app.config.ts`, l'import de `MatPaginatorIntl` fait entrer le paginateur de Material — et ce qu'il entraîne — dans le code chargé au démarrage : 173 kB de plus, constaté. Fournissez-le plutôt dans la route de chaque liste qui l'affiche, `collaborateurs.routes.ts` et `projets.routes.ts` ; les routes enfants (la fiche, le formulaire) en héritent :

```ts
{
  path: '',
  component: ListeCollaborateurs,
  providers: [{ provide: MatPaginatorIntl, useFactory: paginatorFrancais }],
  title: 'Collaborateurs — Chronos',
},
```

Passer en français fait échouer quelques tests des modules 18 et 20 qui comparaient l'affichage « à l'anglaise » : `'0.75'` devient `'0,75'`, `'80%'` devient `'80 %'`. Corrigez l'attendu, pas le code.

Une limite : le format d'un `<input type="date">` (fiche collaborateur, affectation) n'est décidé ni par Angular ni par `lang` : c'est le navigateur qui le choisit, selon sa propre langue. Un Chrome en anglais affiche `mm/dd/yyyy` dans une page en français ; la valeur échangée avec l'application reste `2026-05-04`.

Documentation : [locale du projet](https://angular.dev/guide/i18n/locale-id), [sélecteur de dates : internationalisation](https://material.angular.dev/components/datepicker/overview), [paginateur](https://material.angular.dev/components/paginator/overview)

### 2. Vérifier l'ossature de la page

Un utilisateur de lecteur d'écran ne parcourt pas la page de haut en bas : il saute de titre en titre et de région en région. Quatre repères lui suffisent, et vous en avez déjà posé trois au module 15.

- **Le titre de l'onglet**, annoncé en premier à l'arrivée sur une page. Chaque route porte un `title` depuis le module 15 : vérifiez que celles des modules 16 à 19 en ont un aussi (`'Feuille de temps — Chronos'`, `'Connexion — Chronos'`…).
- **Les titres** : un seul `<h1>`, « Chronos », dans la barre ; un `<h2>` par écran, qui reprend le titre de la route ; des `<h3>` à l'intérieur si besoin. On ne saute pas de niveau, et l'on ne choisit pas un niveau pour sa taille.
- **Les régions** : le menu (`<mat-nav-list>`, qui porte le rôle `navigation` et un `aria-label`) et le contenu (`role="main"`).
- **Le menu est une liste.** Le module 15 avait laissé ce point de côté : la documentation d'Angular Material recommande d'envelopper les liens d'une `<mat-nav-list>` dans une liste, pour que le lecteur d'écran annonce « liste, 3 éléments ». Dans `app.html` :

```html
<mat-nav-list aria-label="Menu principal">
  <ul>
    @for (lien of liensVisibles(); track lien.chemin) {
      <li>
        <a mat-list-item [routerLink]="lien.chemin" routerLinkActive #etat="routerLinkActive"
           [activated]="etat.isActive" ariaCurrentWhenActive="page">
          <mat-icon matListItemIcon>{{ lien.icone }}</mat-icon>
          <span matListItemTitle>{{ lien.libelle }}</span>
        </a>
      </li>
    }
  </ul>
</mat-nav-list>
```

Et dans `app.scss`, pour retirer les puces et les marges du navigateur :

```scss
ul {
  list-style: none;
  margin: 0;
  padding: 0;
}
```

- **Le menu n'existe que pour un utilisateur connecté.** Sur l'écran de connexion, un lecteur d'écran annonçait jusqu'ici « Menu principal, liste, 4 éléments », et Tab traversait quatre liens vers des écrans protégés avant d'atteindre le champ de l'adresse. Dans `app.ts`, `liensVisibles` renvoie une liste vide quand personne n'est connecté, et `app.html` n'affiche le bouton du menu et `<mat-nav-list>` que s'il y a des liens :

```ts
protected readonly liensVisibles = computed(() =>
  this.session.estConnecte()
    ? this.liens.filter((lien) => !lien.roles || this.session.aLeRole(...lien.roles))
    : [],
);
```

```html
@if (liensVisibles().length > 0) {
  <mat-nav-list aria-label="Menu principal">
    …
  </mat-nav-list>
}
```

Un panneau vide resterait pourtant à l'écran, avec ses 240 pixels de marge à gauche du formulaire de connexion. Il ne s'ouvre donc que s'il a quelque chose à montrer :

```html
<mat-sidenav #menu mode="side" [opened]="liensVisibles().length > 0">
```

- **Un `<h2>` par écran, sans exception.** La fiche collaborateur n'avait qu'un titre de carte : elle reçoit `<h2>Fiche collaborateur</h2>`, et le nom devient un `<h3 mat-card-title>`.

Documentation : [listes : accessibilité](https://material.angular.dev/components/list/overview), [titres de page](https://angular.dev/guide/routing/define-routes)

### 3. Un tableau que l'on peut lire sans le voir

Dans un tableau, un lecteur d'écran annonce chaque cellule avec ses en-têtes : « Projet Alpha, lundi 4 mai, 0,5 ». Encore faut-il que le HTML dise quelles cellules sont des en-têtes. Le module 18 vous a fait écrire `GrilleDeSaisie` dans cet esprit ; relisez son template avec ces quatre points en tête :

```html
<table>
  <caption>Temps saisis par projet et par jour, en fractions de journée</caption>
  <thead>
    <tr>
      <th scope="col">Projet</th>
      @for (jour of jours(); track jour) {
        <th scope="col">{{ libelleJour(jour) }}</th>
      }
      <th scope="col">Total</th>
    </tr>
  </thead>
  <tbody>
    @for (ligne of lignes(); track ligne.projetId; let i = $index) {
      <tr>
        <th scope="row">{{ ligne.libelle }}</th>
        @for (jour of jours(); track jour) {
          <td [class.ferme]="joursFermes().has(jour)">
            <select
              #choix
              [aria-label]="ligne.libelle + ', ' + libelleJour(jour)"
              [disabled]="!estSaisissable(ligne, jour)"
              (change)="saisir(ligne, jour, choix.value)"
            >
              …
            </select>
          </td>
        }
        <td>{{ totauxParLigne()[i] | number: '1.0-2' }}</td>
      </tr>
    }
  </tbody>
</table>
```

- **`<caption>`** nomme le tableau. C'est ce qui est annoncé quand on y entre. Si la vôtre dit seulement « Grille », réécrivez-la.
- **`<th scope="col">`** et **`<th scope="row">`** désignent les en-têtes de colonne et de ligne. Le nom du projet est un `<th>`, pas un `<td>`.
- **`aria-label`** donne à chaque liste un nom complet. Un champ isolé dans une cellule n'a pas de `<label>` visible ; sans nom, il serait annoncé « liste déroulante », trente-cinq fois de suite. Le module 18 l'écrivait `[attr.aria-label]` ; la forme directe, `[aria-label]`, fonctionne aussi : un attribut ARIA se lie comme n'importe quel attribut.
- **Une liste de cinq valeurs** plutôt qu'une saisie libre : RG-30 est respectée par construction, et un `<select>` natif est utilisable au clavier sans rien ajouter.

Un jour fermé (week-end, férié) porte la classe `ferme`, qui le grise. Le gris seul ne dit rien à un lecteur d'écran : écrivez-le dans l'en-tête de la colonne, par un texte que seul le lecteur d'écran lit.

```html
<th scope="col" [class.ferme]="joursFermes().has(jour)">
  {{ libelleJour(jour) }}
  @if (joursFermes().has(jour)) {
    <span class="masque"> (jour fermé)</span>
  }
</th>
```

```scss
// Un texte lu par les lecteurs d'écran, invisible à l'écran.
.masque {
  border: 0;
  clip-path: inset(50%);
  height: 1px;
  overflow: hidden;
  position: absolute;
  white-space: nowrap;
  width: 1px;
}
```

Pourquoi pas `display: none` ? Un texte masqué ainsi n'est lu par personne.

Documentation : [tableaux accessibles (W3C)](https://www.w3.org/WAI/tutorials/tables/)

### 4. Relier chaque erreur à son champ

Un message d'erreur affiché en rouge sous un champ est invisible pour qui ne voit pas l'écran, sauf si le champ **pointe** vers lui.

Avec `mat-form-field`, c'est fait. Voici l'`<input>` de la quotité après une saisie invalide, tel que relevé dans un test :

```html
<input matInput type="number" id="mat-input-a3" aria-invalid="true" aria-describedby="mat-mdc-error-a0" …>
```

`aria-invalid` annonce « non valide » ; `aria-describedby` désigne l'élément `<mat-error>`, dont le texte est lu à la suite du libellé. Vous n'avez rien écrit de cela.

Avec des éléments HTML simples, c'est à vous de le faire. `[formField]` ne pose pas ces attributs sur un champ natif (constaté). Pour le motif de rejet d'une feuille de temps (RG-34), dans le dialogue de l'exercice 3 du module 18 :

```html
<label for="motif">Motif du rejet</label>
<textarea
  id="motif"
  rows="4"
  aria-required="true"
  [formField]="formulaire.motif"
  [aria-invalid]="enErreur()"
  [aria-describedby]="enErreur() ? 'motif-erreur' : null"
></textarea>
@if (enErreur()) {
  <div id="motif-erreur" role="alert">
    @for (erreur of formulaire.motif().errors(); track $index) {
      <p class="erreur">{{ erreur.message }}</p>
    }
  </div>
}
```

```ts
protected readonly enErreur = () => this.formulaire.motif().touched() && this.formulaire.motif().invalid();
```

- **`<label for="motif">`** et **`id="motif"`** : le libellé est lu quand le champ reçoit le focus, et cliquer sur le libellé place le curseur dans le champ.
- **`null`** retire l'attribut : on ne désigne pas un élément qui n'existe pas.
- **`aria-required`** annonce « obligatoire » : la règle `required` du formulaire ne pose rien sur un champ natif.
- **`role="alert"`** fait lire le message dès qu'il apparaît, sans attendre que l'utilisateur revienne sur le champ.

### 5. Annoncer ce qui change

Après « Enregistrer le brouillon », la feuille est rechargée et rien ne bouge à l'écran, ou si peu. Un utilisateur qui ne voit pas la page ne sait pas si son action a abouti. Une **zone vivante** (`aria-live`) est une partie de la page dont le lecteur d'écran lit les changements, où que soit le focus.

Pour un message qui s'affiche dans la page, un rôle suffit, et le module 18 les a déjà posés dans `FeuilleDeTempsHebdo` :

```html
<p role="status">Feuille rejetée : {{ motif }}</p>
```

```html
<p role="alert">Le total dépasse une journée : …</p>
```

`role="status"` attend que le lecteur d'écran ait fini sa phrase ; `role="alert"` l'interrompt. Réservez `alert` à ce qui bloque l'utilisateur.

Pour un message qui ne s'affiche nulle part, `LiveAnnouncer` tient une zone vivante invisible à votre disposition. Dans `FeuilleDeTempsHebdo`, donnez à `envoyer` le message à annoncer :

```ts
import { LiveAnnouncer } from '@angular/cdk/a11y';
```

```ts
private readonly annonceur = inject(LiveAnnouncer);
private readonly titre = viewChild.required<ElementRef<HTMLElement>>('titre');

private envoyer(requete: Observable<unknown>, annonce: string, focusSurLeTitre = false): void {
  this.envoiEnCours.set(true);
  requete.pipe(finalize(() => this.envoiEnCours.set(false))).subscribe({
    next: () => {
      this.feuille.reload();
      void this.annonceur.announce(annonce, 'polite');
      if (focusSurLeTitre) this.titre().nativeElement.focus();
    },
    error: () => {}, // déjà notifiée par l'intercepteur ; la saisie reste à l'écran
  });
}
```

`enregistrer` passe « Brouillon enregistré. », `soumettre` « Feuille de temps soumise. » et `true` : une fois la feuille soumise, ses boutons disparaissent, et le focus qui était sur « Soumettre » tomberait sur `<body>`. Il va au titre, `<h2 #titre tabindex="-1">` (étape 7). `FeuillesAValider` fait de même pour « Feuille de temps validée. » et « Feuille de temps rejetée. ».

Constaté dans Chromium : l'annonce est émise une seule fois, une centaine de millisecondes après la réponse de l'API (le délai par défaut de `LiveAnnouncer`). Les messages de `MatSnackBar` — ceux de `erreursInterceptor` — sont annoncés de la même façon, sans rien ajouter. La barre de progression du module 16 porte un `aria-label` : elle est annoncée elle aussi.

### 6. Ne jamais dire une chose par la couleur seule

Dans le plan de charge, une case rouge signale une surcharge. Un utilisateur daltonien, ou un écran mal réglé, n'y verra qu'une case. C'est le critère 3.1 du RGAA : l'information ne doit pas être donnée uniquement par la couleur.

Le module 18 a appliqué la règle deux fois, et c'est le moment de vérifier qu'elle tient partout :

- `IndicateurCharge` pose la couleur par une classe sur son élément hôte, et écrit **toujours** le libellé de `LIBELLES_NIVEAU` : « Surcharge », « Intercontrat », « Sous-charge »…
- dans `GrilleDeSaisie`, un total supérieur à 1 (RG-31) prend la classe `depassement` **et** le texte « Plus d'un jour ».

Le contrôle est simple : faites une capture du plan de charge, passez-la en niveaux de gris dans n'importe quel outil d'image, et relisez-la. Tout ce que vous ne comprenez plus est à écrire.

Pour les couleurs elles-mêmes, gardez les variables du thème (`var(--mat-sys-error)`, `var(--mat-sys-error-container)` avec `var(--mat-sys-on-error-container)`…) plutôt que des valeurs écrites à la main : les couples prévus par Material 3 sont conçus pour être lisibles ensemble. WCAG demande un rapport de contraste d'au moins 4,5 pour 1 pour le texte courant ; l'étape 9 le mesure.

Documentation : [thème Angular Material](https://material.angular.dev/guide/theming)

### 7. Maîtriser le focus

**Le rendre visible.** Débranchez votre souris et appuyez sur Tab : savez-vous, à chaque instant, où vous êtes ? Angular Material dessine son propre indicateur, sauf sur les liens du menu, où un fond à peine grisé ne suffit pas : `app.scss` leur donne un contour (`a[mat-list-item]:focus-visible`). Pour vos éléments, dans `grille-de-saisie.scss` :

```scss
table :focus-visible {
  outline: 3px solid var(--mat-sys-primary);
  outline-offset: 2px;
}
```

`:focus-visible` ne s'applique que lorsque le focus vient du clavier. On n'écrit jamais `outline: none` sans le remplacer.

**Le placer après une navigation.** Dans une application à page unique, changer d'écran ne recharge rien : le focus reste sur le lien du menu, et le lecteur d'écran ne dit rien. Envoyez-le sur le titre du nouvel écran. Dans le composant racine :

```ts
import { afterNextRender, Component, inject, Injector } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { distinctUntilChanged, filter, map, skip } from 'rxjs';

export class App {
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);

  constructor() {
    this.router.events
      .pipe(
        filter((evenement) => evenement instanceof NavigationEnd),
        map((evenement) => evenement.urlAfterRedirects.split(/[?#]/)[0]),
        distinctUntilChanged(),
        skip(1),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        afterNextRender(() => this.donnerLeFocusAuTitre(), { injector: this.injector });
      });
  }

  private donnerLeFocusAuTitre(): void {
    document.querySelector<HTMLElement>('[role=main] h2')?.focus();
  }
}
```

Et sur le titre de chaque écran : `<h2 tabindex="-1">Feuille de temps</h2>`.

- **`tabindex="-1"`** permet à un élément de recevoir le focus par programme, sans l'ajouter au parcours de la touche Tab.
- **`map` puis `distinctUntilChanged`** : seul un changement de **chemin** compte. La première version de ce code réagissait à chaque `NavigationEnd` ; or la liste des collaborateurs écrit sa recherche dans l'adresse (`?recherche=dur`) à chaque frappe, et le focus quittait le champ de recherche après la première lettre (constaté).
- **`skip(1)`** ignore la première navigation : au chargement de la page, le navigateur fait déjà son travail.
- **`afterNextRender`** attend que le nouvel écran soit dessiné ; au moment de `NavigationEnd`, son `<h2>` n'existe pas encore.
- Le routeur émet ses événements dans un Observable : c'est un cas où RxJS reste l'outil adapté.

Le lien d'évitement de l'exercice 5 réutilise `donnerLeFocusAuTitre` : `<a class="evitement" href="#contenu" (click)="allerAuContenu($event)">`, dont la méthode appelle `preventDefault()` puis place le focus sur le titre, sans changer l'adresse.

**Ne pas le perdre.** Le focus tombe sur `<body>` chaque fois que l'élément qui le porte disparaît ou se désactive. Trois cas dans Chronos : le bouton « Modifier » d'une fiche, remplacé par le formulaire (le focus va au titre du formulaire, puis revient au titre de la fiche à la fermeture) ; « Confirmer » d'une surcharge, remplacé par « confirmée » ; et « Enregistrer le brouillon », désactivé pendant l'envoi — constaté au clavier contre l'API réelle. Pour ce dernier, Angular Material prévoit l'entrée `disabledInteractive` : le bouton prend l'apparence désactivée et `aria-disabled="true"`, mais garde le focus. Le clic n'est alors plus bloqué : la méthode doit vérifier elle-même `if (!this.peutEnvoyer()) return;`.

```html
<button matButton="outlined" type="button" disabledInteractive [disabled]="!peutEnvoyer()" (click)="enregistrer()">
```

**Le contenir dans un dialogue.** Un dialogue modal doit retenir le focus tant qu'il est ouvert, et le rendre à l'élément qui l'a ouvert quand il se ferme. `MatDialog` fait les deux par défaut. Pour un panneau que vous construiriez vous-même, la directive `cdkTrapFocus` du CDK retient le focus ; `cdkTrapFocusAutoCapture` l'y place à l'ouverture.

Documentation : [dialogue : gestion du focus](https://material.angular.dev/components/dialog/overview)

### 8. Parcourir la grille aux flèches

Avec le tableau de l'étape 3, la touche Tab traverse les trente-cinq listes une par une. Cela fonctionne, et c'est long. Les utilisateurs d'un tableur attendent les **flèches**. C'est un comportement de « grille », que décrit le guide du W3C et que fournit Angular Aria.

```powershell
npm install @angular/aria
```

Dans `GrilleDeSaisie`, ajoutez `Grid`, `GridRow`, `GridCell` et `GridCellWidget`, de `@angular/aria/grid`, aux `imports`. Dans le template, quatre directives s'ajoutent au tableau, sans rien retirer :

```html
<table ngGrid rowWrap="nowrap" colWrap="nowrap">
  <caption>Temps saisis par projet et par jour, en fractions de journée</caption>
  <thead>
    <tr ngGridRow>
      <th ngGridCell role="columnheader" scope="col">Projet</th>
      @for (jour of jours(); track jour) {
        <th ngGridCell role="columnheader" scope="col" [class.ferme]="joursFermes().has(jour)">…</th>
      }
      <th ngGridCell role="columnheader" scope="col">Total</th>
    </tr>
  </thead>
  <tbody>
    @for (ligne of lignes(); track ligne.projetId; let i = $index) {
      <tr ngGridRow>
        <th ngGridCell role="rowheader" scope="row">{{ ligne.libelle }}</th>
        @for (jour of jours(); track jour) {
          <td ngGridCell [class.ferme]="joursFermes().has(jour)">
            @if (estSaisissable(ligne, jour)) {
              <select ngGridCellWidget widgetType="editable" #choix [attr.aria-label]="…" (change)="…">
                …
              </select>
            } @else {
              {{ duree(ligne, jour) === 0 ? '—' : (duree(ligne, jour) | number: '1.0-2') }}
            }
          </td>
        }
        <td ngGridCell>{{ totauxParLigne()[i] | number: '1.0-2' }}</td>
      </tr>
    } @empty {
      <tr ngGridRow>
        <td ngGridCell [colSpan]="jours().length + 2">Aucune affectation sur cette semaine. …</td>
      </tr>
    }
  </tbody>
  <!-- le pied : <tr ngGridRow>, rowheader sur « Total du jour », ngGridCell sur chacune des autres cellules -->
</table>
```

Comportement relevé dans Chromium, sur la feuille de temps réelle :

| Touche | Effet |
| --- | --- |
| Tab | Entre dans la grille — **un seul arrêt** pour tout le tableau, sur la dernière cellule visitée —, puis en sort vers le bouton qui suit. |
| Flèches | Passent d'une cellule à sa voisine, en-têtes et totaux compris ; sur une case saisissable, le focus va sur sa liste. |
| Origine, Fin | Première et dernière cellule de la ligne. |
| Entrée | Ouvre la cellule : les flèches changent alors la valeur de la liste (Fin choisit 1). |
| Échap | Referme la cellule : les flèches déplacent de nouveau. |

Quatre points d'attention :

- **`ngGrid` donne au tableau le rôle `grid`**, et à chaque cellule le rôle `gridcell`. Les en-têtes doivent donc déclarer le leur, `columnheader` ou `rowheader`, sans quoi ils ne seraient plus annoncés comme des en-têtes.
- **`widgetType="editable"`** est ce qui sépare « se déplacer » de « modifier ». Sans lui, les flèches haut et bas changeraient la valeur de la liste au lieu de changer de ligne.
- **Une case qu'on ne saisit pas affiche un texte, pas une liste désactivée.** Un `<select disabled>` ne peut pas recevoir le focus : la grille sautait les jours fermés et, sur une feuille soumise ou dans les feuilles à valider, passait du nom du projet directement au total — les valeurs n'étaient plus lisibles au clavier (constaté). Avec un texte, chaque cellule est atteignable et dit ce qu'elle contient. Deux pièges rencontrés en chemin, pour qui voudrait garder les listes désactivées : `[disabled]` posé sur un élément qui porte `ngGridCellWidget` est capté par la directive et ne désactive plus la liste (il faut `[attr.disabled]`) ; et une cellule désactivée (`ngGridCell [disabled]`) reste par défaut la cellule active, sans pouvoir donner le focus à sa liste — le focus semble alors bloqué.
- **`rowWrap="nowrap"` et `colWrap="nowrap"`** : par défaut (`loop`), la flèche droite en fin de ligne ramènerait au début de la ligne, ce qu'un tableur ne fait pas. La ligne « Aucune affectation » s'étend sur toute la largeur : `ngGridCell` l'apprend par son entrée `colSpan`.

C'est l'illustration de la règle des notions : en prenant le rôle `grid`, on a promis un comportement au clavier. Ici, Angular Aria tient la promesse à notre place — à condition de vérifier qu'elle est tenue.

Documentation : [grille Angular Aria](https://angular.dev/guide/aria/grid), [modèle de grille (W3C)](https://www.w3.org/WAI/ARIA/apg/patterns/grid/)

### 9. Vérifier

**Au clavier, d'abord.** C'est le contrôle le plus rentable, et il ne demande aucun outil. Souris débranchée, connectez-vous, saisissez une semaine, soumettez-la. Quatre questions à chaque écran : tout est-il atteignable ? L'ordre est-il logique ? Voit-on toujours le focus ? Peut-on toujours ressortir ?

**Avec axe, dans le parcours de bout en bout.** axe-core est un moteur de règles d'accessibilité ; Playwright documente son usage dans les tests.

```powershell
npm install --save-dev @axe-core/playwright
```

`e2e/accessibilite.spec.ts`, avec l'API simulée du module 20 — à étendre aux collaborateurs, à la fiche, aux clients et aux feuilles à valider, que le module 20 ne simulait pas :

```ts
import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';
import { simulerApi } from './api-simulee';

const regles = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];

test('la feuille de temps ne présente aucune violation détectable', async ({ page }) => {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
  await simulerApi(page, { connecte: true });
  await page.goto('/feuille-de-temps');
  await expect(page.getByRole('grid')).toBeVisible();
  await expect(page).toHaveTitle('Feuille de temps — Chronos');
  await expect(page.locator('html')).toHaveAttribute('lang', 'fr');

  const resultats = await new AxeBuilder({ page }).withTags(regles).analyze();

  expect(resultats.violations).toEqual([]);
});
```

Faites de même pour chaque écran, chaque dialogue ouvert et chaque état d'erreur (formulaire soumis vide, dépassement d'une journée, feuille rejetée). Pour un dialogue, attendez la fin de son animation d'ouverture avant l'analyse : axe mesure les contrastes sur des couleurs encore en transition.

`e2e/clavier.spec.ts` fixe le comportement de l'étape 8 :

```ts
import { expect, Locator, test } from '@playwright/test';
import { simulerApi } from './api-simulee';

/** L'état de la cellule ("cell" : on se déplace ; "widget" : on modifie), posé par Angular Aria. */
async function attendreLeMode(select: Locator, mode: 'cell' | 'widget'): Promise<void> {
  await expect(select).toHaveAttribute('data-active-control', mode);
}

test('la grille se parcourt au clavier', async ({ page }) => {
  await page.clock.setFixedTime(new Date(2026, 4, 6, 10, 0));
  await simulerApi(page, { connecte: true });
  await page.goto('/feuille-de-temps');
  const lundi = page.getByLabel('Refonte du portail client, lun. 4 mai');
  await expect(lundi).toBeVisible();

  await page.getByRole('button', { name: 'Semaine suivante' }).focus();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('columnheader', { name: 'Projet' })).toBeFocused();

  await page.keyboard.press('ArrowDown');
  await expect(page.getByRole('rowheader', { name: 'Refonte du portail client' })).toBeFocused();
  await page.keyboard.press('ArrowRight');
  await expect(lundi).toBeFocused();

  await page.keyboard.press('Enter');
  await attendreLeMode(lundi, 'widget');
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('ArrowDown');
  await expect(lundi).toHaveValue('0.5');
  await page.keyboard.press('Escape');
  await attendreLeMode(lundi, 'cell');

  await page.keyboard.press('ArrowRight');
  await expect(page.getByLabel('Refonte du portail client, mar. 5 mai')).toBeFocused();
});
```

Les étiquettes (`wcag2aa`, `wcag22aa`…) choisissent les règles à appliquer ; axe en propose aussi une pour le RGAA, `RGAAv4`. Le contraste des couleurs fait partie des règles vérifiées. Le second test part du bouton « Semaine suivante », dernier élément avant la grille, et suit le focus touche après touche. Si l'un de ses `toBeFocused` échoue chez vous, c'est qu'un élément s'intercale — ou que la grille ne se comporte pas comme prévu : lisez le rapport de Playwright avant de corriger le test.

`attendreLeMode` répond à un piège constaté : Playwright envoie les touches bien plus vite qu'une main, et une touche envoyée juste après Échap pouvait être perdue. Le test attend donc que la cellule ait changé d'état. Cet état, Angular Aria l'écrit dans l'attribut `data-active-control`, **qui n'est pas documenté** : il n'est lu que par ce test, jamais par le code de l'application, et s'il change de nom à une mise à jour, le test échouera bruyamment — c'est le compromis accepté. Au clavier, à vitesse humaine, rien n'est perdu.

Le même fichier vérifie le parcours entier sans souris — se connecter, saisir, enregistrer, soumettre — et lit les annonces dans l'élément `.cdk-live-announcer-element`, où `LiveAnnouncer` écrit ses messages.

**Avec Lighthouse.** Dans les outils de développement de Chrome ou d'Edge, l'onglet *Lighthouse* audite la page affichée — connexion comprise — et note son accessibilité. Utile pour un premier tour d'horizon.

**Avec un lecteur d'écran.** Aucun outil ne remplace cet essai. Sous Windows, le Narrateur est intégré (touche Windows + Ctrl + Entrée) ; NVDA est gratuit et très répandu. Fermez les yeux, et remplissez votre feuille de temps.

> **Zéro violation n'est pas « accessible ».** Un outil automatique détecte un attribut manquant ou un contraste insuffisant. Il ne sait pas si un libellé a du sens, si l'ordre de lecture est logique, si l'annonce arrive au bon moment. La documentation de Playwright le dit elle-même : ces contrôles se combinent avec des essais manuels.

Documentation : [tests d'accessibilité avec Playwright](https://playwright.dev/docs/accessibility-testing), [étiquettes d'axe-core](https://github.com/dequelabs/axe-core/blob/develop/doc/API.md), [Lighthouse](https://developer.chrome.com/docs/lighthouse/overview), [NVDA](https://www.nvaccess.org/download/), [Narrateur](https://support.microsoft.com/fr-fr/windows/guide-complet-du-narrateur-e4397a0d-ef4f-b386-d8ae-c172f109bdb1)

### 10. Finitions

**Les états vides.** Un tableau sans ligne ressemble à une panne. Vos écrans disent déjà ce qu'il en est : « Aucun collaborateur ne correspond à « … » » (module 16), « Aucune affectation sur cette semaine. » (module 18). Relisez-les en vous demandant s'ils disent aussi **quoi faire** : effacer la recherche, changer de semaine, s'adresser à son manager. Faites le tour des autres listes — projets, plan de charge d'une équipe vide.

L'écran « Absences », lui, est resté depuis le module 15 un titre et une phrase : aucun module du cours ne le construit. Déposer une demande d'absence et la faire approuver (cahier des charges, § 4.5) passe par l'API du module 10, que vous savez maintenant appeler, et ne réclame rien que les modules 16 à 21 ne vous aient appris : une liste, un formulaire, deux boutons d'approbation. C'est un exercice tout indiqué pour vérifier ce que vous avez acquis — la solution de référence ne le contient pas.

**Les attentes.** La barre de progression du module 16 et son `aria-label` sont le minimum. Si un chargement dure, un **squelette** — des blocs gris à la place du contenu à venir — évite que la page ne saute à l'arrivée des données. Gardez alors la barre, ou un texte dans un élément `role="status"` : un bloc gris ne s'annonce pas.

**Différer ce qui n'est pas visible tout de suite.** `@defer` sort un morceau de template, et les composants qu'il utilise, du code chargé d'emblée. Par exemple, pour un panneau de détail placé sous le plan de charge :

```html
<section aria-live="polite" aria-atomic="true">
  @defer (on viewport) {
    <app-detail-charge />
  } @placeholder {
    <p>Le détail s'affichera ici.</p>
  } @loading (after 100ms; minimum 300ms) {
    <p>Chargement du détail…</p>
  } @error {
    <p>Le détail n'a pas pu être chargé.</p>
  }
</section>
```

`on viewport` déclenche le chargement quand le bloc entre dans la partie visible de la page. À la compilation, le composant différé apparaît dans la liste *Lazy chunk files*. La zone vivante qui l'entoure fait annoncer le contenu quand il arrive : c'est la recommandation de la documentation.

Chronos n'a pas, aujourd'hui, de tel panneau. Un `@defer` autour du formulaire de la fiche collaborateur a été essayé : aucun gain, puisque chaque écran est déjà chargé à la demande et que le formulaire partage l'essentiel de son code (Material, formulaires) avec l'écran qui l'affiche. `@defer` paie pour un bloc lourd, propre à lui seul, et pas tout de suite visible.

**Le budget de taille : trancher.** Depuis le module 14, `ng build` avertit que le chargement initial dépasse 500 kB, et le module 15 a laissé la question ouverte. Un avertissement permanent n'avertit plus de rien : il faut décider, en trois temps.

1. **Mesurer.** Dans le tableau de `npx ng build`, la ligne *Initial total* donne le poids brut — celui que compare le budget — et le poids transféré après compression. Chronos à la fin de ce module : 633 kB bruts, 147 kB transférés. Pour savoir **de quoi** il est fait, `npx ng build --stats-json` écrit `dist/chronos-web/browser-stats.json`, qu'un analyseur de *bundle* esbuild (celui du site d'esbuild, par exemple) affiche par paquet. Mesuré : le cœur d'Angular 197 kB, Angular Material 190 kB (dont la liste 52, le bouton 36, le panneau latéral 33, la *snackbar* 18), le CDK 116 kB (dont la couche des *overlays* 55), le routeur 99 kB, `@angular/common` 51 kB, `@angular/forms` 42 kB, RxJS 28 kB ; le code de Chronos, moins de 10 kB.
2. **Alléger ce qui peut l'être.** Tous les écrans sont déjà chargés à la demande. Ce qui demeure dans *Initial chunk files* est la coquille : Angular, le routeur, et les composants Material de la barre et du menu. Vérifiez qu'elle n'importe rien qu'elle n'affiche pas. Deux pistes ont été mesurées, sans gain : importer les composants un par un (`MatNavList`, `MatListItem`…) au lieu de `MatListModule` (−0,3 kB : le compilateur ne garde déjà que ce que le template utilise) ; `@defer` (voir plus haut). La seule vraie réserve est la *snackbar* de `erreursInterceptor`, qui n'apparaît qu'en cas d'erreur mais entraîne les *overlays* du CDK : la charger à la demande (`import('@angular/material/snack-bar')` dans l'intercepteur) retire 66 kB bruts et 14 kB transférés, au prix d'un code asynchrone dans l'intercepteur et dans ses tests. Chronos ne le fait pas ; c'est une décision, à écrire comme telle.
3. **Fixer un seuil qui ait un sens.** Le seuil de 500 kB est celui d'une application sans bibliothèque de composants. Une fois l'allègement fait, placez l'avertissement un peu au-dessus de votre mesure, et l'erreur guère plus haut :

```json
"budgets": [
  { "type": "initial", "maximumWarning": "700kB", "maximumError": "850kB" },
  { "type": "bundle", "name": "plan-de-charge", "maximumWarning": "45kB", "maximumError": "60kB" },
  { "type": "anyComponentStyle", "maximumWarning": "4kB", "maximumError": "8kB" }
]
```

Ces valeurs sont celles de la solution de référence (633 kB mesurés ; l'écran le plus lourd, `plan-de-charge`, 38 kB), qui compile alors sans avertissement ; les vôtres viennent de **votre** mesure. Le budget `bundle` désigne un fichier par le nom qu'affiche la colonne *Names* du tableau de `ng build`. Écrivez dans le message du commit ce que vous avez mesuré et pourquoi ce seuil. Le budget retrouve son rôle : signaler le jour où un commit ajoute cent kilo-octets au démarrage. Au-delà de `maximumError`, `ng build` échoue, et le job `web` avec lui.

**Les polices.** `ng add @angular/material` a écrit dans `index.html` des liens vers Google Fonts, pour la police Roboto et pour les icônes. À chaque ouverture de Chronos, le navigateur de l'utilisateur contacte donc les serveurs d'un tiers, qui reçoivent son adresse IP. Et sur un réseau sans accès à Internet, les polices ne se chargent pas : les icônes, qui sont des mots affichés avec une police spéciale, redeviendraient des mots. Pour un outil RH interne, les deux points méritent une décision ; l'exercice 6 propose d'héberger les polices avec l'application.

Ce qui a été constaté en le faisant avec Fontsource (`@fontsource/roboto`, `@fontsource/material-symbols-outlined`) :

- les fichiers CSS se déclarent dans `styles` d'`angular.json`, avant `src/styles.scss` : `"@fontsource/roboto/latin-400.css"` (et 300, 500), `"@fontsource/material-symbols-outlined/latin-400.css"` ; `ng build` copie les polices dans `media/` ;
- le paquet des icônes déclare la police, **pas la classe** `material-symbols-outlined` que `<mat-icon>` pose sur chaque icône, et que la feuille de Google Fonts fournissait : sans elle, les icônes redeviennent des mots. Recopiez cette classe dans `styles.scss` (`font-family: 'Material Symbols Outlined'`, `font-feature-settings: 'liga'`, `font-size: 24px`, `line-height: 1`…) ;
- la police des icônes pèse 323 kB (woff2), pour une quinzaine d'icônes utilisées. C'était déjà le cas depuis Google Fonts : rien n'est perdu, la police est mise en cache, et elle ne compte pas dans le budget *initial*. Un sous-ensemble limité aux icônes utilisées tiendrait en quelques kilo-octets, mais il faut un outil pour le produire et le refaire à chaque nouvelle icône : Chronos ne le fait pas ;
- un test de bout en bout, `e2e/polices.spec.ts`, vérifie qu'aucune requête ne part vers un autre site et que `document.fonts.check('24px "Material Symbols Outlined"')` répond `true`. L'API simulée du module 20 n'a plus à bloquer `fonts.googleapis.com`.

Documentation : [`@defer`](https://angular.dev/guide/templates/defer), [budgets de taille](https://angular.dev/tools/cli/build#configuring-size-budgets)

## À vous

1. **Partout en français.** Passez en revue tous vos écrans : dates, durées, taux de charge, quotités, budgets en jours. Remplacez chaque affichage brut par le *pipe* qui convient. Puis ajoutez à vos tests du module 20 trois vérifications de format ; attention aux espaces insécables. Le test d'`IndicateurCharge` qui cherchait « 133 » passe-t-il toujours ?

2. **Le plan de charge.** Le tableau de `PlanDeCharge` a déjà ses `<th scope>` et ses libellés. Donnez-lui une `<caption>`, vérifiez sur une capture en niveaux de gris que surcharge (RG-24), intercontrat (RG-25) et sous-charge restent lisibles, et ajoutez-lui un test axe. Le bouton « Confirmer » de chaque case en surcharge dit-il **quelle** surcharge il confirme, pour qui ne voit pas la case ? Faut-il donner à ce tableau le rôle `grid`, comme à la feuille de temps ? Justifiez.

3. **Le formulaire du collaborateur.** Parcourez `FormulaireCollaborateur` au clavier, puis au lecteur d'écran, en création et en modification. Chaque champ est-il annoncé avec son libellé, son caractère obligatoire, son erreur ? Les champs désactivés en modification disent-ils pourquoi ils le sont ? Le message d'un `409`, affiché au niveau du formulaire, est-il annoncé quand il apparaît ? Corrigez ce qui manque. Les dates se saisissent dans des `<input type="date">` : dans quel format le navigateur les présente-t-il, et qui en décide ?

4. **Les dialogues.** Ouvrez `ConfirmationDeSurcharge` au clavier : où est le focus à l'ouverture ? Peut-on sortir du dialogue avec Tab ? Avec Échap ? Où revient le focus à la fermeture ? Faites le même examen pour le dialogue de rejet d'une feuille de temps (exercice 3 du module 18), et reliez son message d'erreur au champ du motif, comme à l'étape 4.

5. **Un lien d'évitement.** Ajoutez en tête de page un lien « Aller au contenu », visible seulement quand il reçoit le focus, qui évite de retraverser la barre et le menu à chaque écran. Où doit-il mener, et comment vérifiez-vous au clavier qu'il fait gagner du temps ?

6. **Alléger le démarrage.** Appliquez à votre application les trois temps de l'étape 10 : mesurez, allégez, fixez votre seuil et justifiez-le dans le commit. Ajoutez un budget de type `bundle` pour l'écran le plus lourd. Puis traitez les polices : remplacez les liens vers Google Fonts de `index.html` par des polices servies avec l'application — [Fontsource](https://fontsource.org/docs/getting-started/install) en distribue sous forme de paquets npm ; cherchez ce qui existe pour Roboto et pour les icônes Material Symbols. Vérifiez dans l'onglet *Réseau* qu'aucune requête ne part plus vers un autre site.

## Critères de réussite

- Aucune date au format anglais, aucun nombre avec un point décimal dans l'interface ; `<html lang="fr">` dans la page servie.
- Chaque route a un titre ; après un changement d'écran, le focus est sur le `<h2>` du nouvel écran.
- Le parcours « se connecter, saisir une semaine, la soumettre » se fait **sans souris**, et le focus est toujours visible.
- Dans la grille, les flèches déplacent d'une cellule à l'autre — y compris sur une feuille soumise et dans les feuilles à valider —, et Tab en sort en une fois.
- Une surcharge et un dépassement de journée se lisent sur un écran en noir et blanc.
- `npx ng e2e` réussit, tests axe compris, sur chaque écran, ses dialogues et ses états d'erreur.
- Vous avez rempli une feuille de temps au lecteur d'écran, et noté ce qui vous a gêné.
- `npx ng build` ne signale aucun dépassement de budget.
- Comparez avec la solution de référence : `git diff module-20 module-21`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `<div (click)="ouvrir()">` | Ni focus, ni clavier, ni rôle annoncé. | `<button type="button">`. |
| `role="button"` sur un `<div>` | Le rôle est annoncé, le comportement manque. | L'élément HTML natif. |
| `outline: none` | Le focus devient invisible au clavier. | `:focus-visible` avec un contour net. |
| `tabindex="1"`, `"2"`… | Impose un ordre de tabulation qui contredit celui de la page. | L'ordre du HTML ; `0` ou `-1` seulement. |
| `placeholder` en guise de libellé | Disparaît à la saisie ; mal restitué. | `<label>`, ou `<mat-label>`. |
| `aria-label` sur un élément qui a déjà un texte visible | Le lecteur d'écran lit autre chose que ce qui est affiché. | Laisser le texte visible faire son travail. |
| Une cellule rouge pour dire « surcharge » | Imperceptible pour certains utilisateurs. | La couleur **et** un texte. |
| Un `<td>` pour le nom du projet en tête de ligne | Les cellules ne sont plus annoncées avec leur ligne. | `<th scope="row">`. |
| `role="grid"` écrit à la main sur un tableau | Promet une navigation aux flèches qui n'existe pas. | Un `<table>` simple, ou Angular Aria. |
| Un `<select disabled>` dans une cellule de grille | Il ne prend pas le focus : la cellule est sautée, ou la grille reste bloquée ; une feuille en lecture seule devient illisible aux flèches. | Le texte de la valeur à la place de la liste. |
| `[disabled]="envoiEnCours()"` sur le bouton qu'on vient d'activer | Le focus tombe sur `<body>` ; le lecteur d'écran repart du haut de la page. | `disabledInteractive`, et une garde dans la méthode. |
| Rendre le focus au titre à chaque `NavigationEnd` | Écrire un filtre dans l'adresse arrache le focus du champ de saisie. | Ne réagir qu'à un changement de chemin. |
| Un menu de navigation sur l'écran de connexion | Des liens vers des écrans interdits, à traverser avant le formulaire. | N'afficher le menu qu'à un utilisateur connecté. |
| `<html lang="en">` laissé par `ng new` | Le français est lu avec une voix anglaise. | `lang="fr"`. |
| `registerLocaleData(localeFr)` dans `main.ts`, avec `{ provide: LOCALE_ID, useValue: 'fr' }` | Fonctionne dans l'application, mais les tests restent en `en-US` et `lang` n'est pas corrigé (constaté). | `"i18n": { "sourceLocale": "fr" }` dans `angular.json`. |
| `{{ taux * 100 }} %`, `toFixed(2)`, `toLocaleDateString()` | Formats faits à la main, incohérents d'un écran à l'autre. | `percent`, `number`, `date`. |
| `expect(texte).toBe('133 %')` | L'espace affichée est insécable ; la comparaison échoue. | Écrire l'espace par son code, `\u` suivi de `00a0`, ou se contenter de `toContain('133')`. |
| Un test axe vert pris pour une preuve | L'outil ne détecte qu'une partie des défauts. | Clavier et lecteur d'écran en plus. |
| Laisser l'avertissement de budget s'afficher à chaque compilation, ou relever le seuil sans rien mesurer | Dans les deux cas, plus personne ne voit le poids croître. | Mesurer, alléger, puis fixer un seuil juste au-dessus de la mesure. |
| `@defer` autour d'un composant aussi importé ailleurs dans le même fichier | Le composant est chargé d'emblée ; rien n'est différé. | Ne l'utiliser que dans le bloc différé. |

La liste des pratiques Angular obsolètes est dans le [référentiel Angular 22](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- Qu'avez-vous découvert en utilisant Chronos au lecteur d'écran, que ni axe ni vos tests ne vous avaient dit ?
- « Un rôle est une promesse. » Quelles promesses avez-vous faites en donnant le rôle `grid` à la feuille de temps ? Qui les tient ?
- L'accessibilité est-elle une affaire de finition, comme le titre de ce module le laisse entendre ? À quel module du cours aurait-il fallu y penser, et qu'est-ce que cela aurait changé ?
- Les polices de Chronos viennent des serveurs de Google. Qui cela concerne-t-il — l'utilisateur, l'employeur, le délégué à la protection des données ? Quels arguments pour les héberger soi-même, et lesquels contre ?
- Votre employeur est-il tenu de respecter le RGAA ? Comment le savoir, et qui en répond ?
- Chronos est en français, libellés écrits dans les templates. Le jour où une filiale anglophone le demande, que faut-il reprendre ? Qu'auriez-vous pu faire dès aujourd'hui pour préparer ce jour, et était-ce justifié ?

## Pour aller plus loin

- [Comment satisfaire aux WCAG — référence rapide](https://www.w3.org/WAI/WCAG22/quickref/) : chaque critère, avec les techniques qui y répondent.
- [Modèles du guide des pratiques ARIA](https://www.w3.org/WAI/ARIA/apg/patterns/grid/) : le comportement clavier attendu de chaque composant.
- [Internationalisation avec Angular](https://angular.dev/guide/i18n) : marquer les textes, les extraire, produire une application par langue.
- [Créer un thème Angular Material](https://material.angular.dev/guide/theming) : palettes, mode sombre, contraste renforcé.
- Suite dans le cours : le [module 22](22-qualite-du-code.md) ajoute un lint, dont des règles d'accessibilité pour les templates ; le [module 23](23-conteneuriser-l-application.md) sert l'application compilée, budgets compris.
