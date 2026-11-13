# Module 17 — Formulaires

> Pratiques vérifiées le 12 novembre 2026 — voir le [référentiel Angular 22](../docs/references/angular-22.md).
> Le code de ce module a été **compilé et testé le 12 novembre 2026** (Angular 22.2.6, CLI 22.2.7, Angular Material 22.2.7, TypeScript 6.0.3, Vitest 5.1.0, Node.js 24.22.3), à la suite de celui du module 16. L'association de Signal Forms et d'Angular Material, signalée « à vérifier » par le référentiel, a fait l'objet d'un **essai réel** : tests avec les harnais de Material, puis saisie dans un navigateur. Le module a ensuite été **rejoué le 12 novembre 2026 sur la solution de référence, contre la véritable API .NET** (module 12, PostgreSQL 18) dans Chromium, avec les comptes du gestionnaire RH, de la manager et de la collaboratrice : 201, 400, 409 (unicité et concurrence), 422 (RG-03, RG-04, RG-11, RG-12) et panne réseau. Les compléments que ce module apporte à l'API (étape 6 : `regle` et `code` sur les `409`, messages de validation en français) ont été rejoués de même, avec leurs tests d'intégration. Signalez au mentor tout écart constaté.

Le gestionnaire RH crée la fiche d'un nouveau collaborateur, corrige une adresse, enregistre une date de sortie. Un formulaire, c'est beaucoup de petites décisions : quand afficher une erreur, quoi faire pendant l'envoi, comment montrer le refus de l'API sans effacer dix champs déjà saisis. Ce module les prend une par une, avec Signal Forms.

## Objectifs

À la fin de ce module, vous saurez :

- construire un formulaire à partir d'un modèle en signal et d'un schéma de règles ;
- valider un champ, un champ par rapport à un autre, un champ selon une condition ;
- afficher les erreurs au bon moment, dans des champs Angular Material ;
- soumettre le formulaire, et rattacher les erreurs renvoyées par l'API au champ ou au formulaire ;
- utiliser le même composant pour créer et pour modifier, y compris en cas de modification concurrente ;
- expliquer pourquoi la validation de l'interface ne dispense pas de celle de l'API.

## Prérequis

- [Module 16](16-dialoguer-avec-l-api.md) : `CollaborateursApi`, `ErreurApi`, `erreursInterceptor`, la fiche lue par `httpResource`, et une session ouverte avec un compte de gestionnaire RH.
- [Module 15](15-routage.md) : la route `/collaborateurs/nouveau` affiche le composant provisoire `FormulaireCollaborateur`.
- [Module 08](08-une-api-robuste.md) : `CreerCollaborateurRequete`, `ModifierCollaborateurRequete`, et les réponses 400, 409 et 422 de l'API : `Reponses.Conflit`, `ConflitExceptionHandler`, l'extension `regle` des `422`. L'étape 6 les complète, côté serveur.

## Règles de gestion

| Règle | Ce que fait l'interface |
| --- | --- |
| **RG-03** — quotité entre 10 % et 100 % | Vérifiée à la saisie (pas à pas, étape 3). Saisie en pourcentage, envoyée en fraction. |
| **RG-02** — date de sortie ≥ date d'entrée | Vérifiée à la saisie, en comparant deux champs (étape 7). |
| **RG-11** — client obligatoire pour un projet facturable, absent pour un projet interne | Champ affiché et exigé selon la nature du projet (exercice 1). |
| **RG-12** — date de fin ≥ date de début d'un projet | Comme RG-02 (exercice 1). |
| **RG-01**, **RG-10** — unicité ; **RG-04** — chaîne de managers | L'interface ne peut pas les vérifier : elle **affiche** le refus de l'API (étape 6). |

## Notions

### Un modèle, un schéma

Un Signal Form part d'un **modèle** : un signal qui contient un objet simple, avec **tous** ses champs initialisés — chaîne vide pour un texte, `null` pour un nombre absent, jamais `undefined`. Un champ `undefined` n'existe pas pour le formulaire.

`form(modele, schema)` renvoie un **arbre de champs** qui a la forme du modèle : `formulaire.nom`, `formulaire.email`. Le formulaire ne garde pas de copie des données : écrire dans un champ, c'est écrire dans le modèle.

Le **schéma** est une fonction, exécutée une fois, qui déclare les règles : `required(champs.nom)`. Dans le template, la directive `[formField]` relie un champ à un contrôle, dans les deux sens, et `[formRoot]` relie le formulaire à la balise `<form>`.

Documentation : [vue d'ensemble](https://angular.dev/guide/forms/signals/overview), [modèles](https://angular.dev/guide/forms/signals/models)

### Les règles

| Besoin | Écriture |
| --- | --- |
| Règles courantes | `required`, `email`, `min`, `max`, `minLength`, `maxLength`, `pattern`, chacune avec `{ message: '…' }` |
| Règle sur mesure | `validate(champ, ({ value }) => …)` : renvoie `{ kind, message }` ou `null` |
| Règle qui lit un autre champ | dans la fonction, `valueOf(champs.autre)` |
| Règle sous condition | l'option `when` : `required(champ, { when: ({ valueOf }) => … })` |
| Champ sans objet dans certains cas | `hidden(champ, { when: condition })` |
| Champ visible mais non modifiable | `disabled(champ, { when: condition })`, `readonly(champ, { when: condition })` |

Sans `when`, la règle s'applique toujours : `disabled(champs.id)`. Passer la condition directement en second argument, `disabled(champ, condition)`, est **déprécié** depuis la v22.

Toutes les règles d'un champ sont évaluées, et réévaluées dès qu'une valeur lue change : une règle qui compare deux dates se met à jour quand l'une **ou** l'autre est modifiée.

Un champ masqué, désactivé ou en lecture seule **n'est plus validé**. Attention : `hidden` ne retire rien de l'écran. Il donne un état, `formulaire.champ().hidden()`, et c'est à votre template de ne pas afficher le champ.

Signal Forms n'utilise pas la validation native du navigateur : `[formRoot]` pose d'ailleurs `novalidate` sur le formulaire.

Documentation : [validation](https://angular.dev/guide/forms/signals/validation), [règles entre champs](https://angular.dev/guide/forms/signals/cross-field-logic), [logique de formulaire](https://angular.dev/guide/forms/signals/form-logic)

### L'état d'un champ

Appeler un champ comme une fonction donne son état, fait de signals :

| Signal | Sens |
| --- | --- |
| `value()` | La valeur. |
| `errors()` | Les erreurs du champ : `{ kind, message }`. |
| `invalid()` | Au moins une erreur. |
| `touched()` | L'utilisateur est passé par le champ et en est sorti. |
| `dirty()` | L'utilisateur a modifié la valeur. |
| `hidden()`, `disabled()`, `readonly()` | Ce que les règles du même nom ont décidé. |
| `submitting()` | Un envoi est en cours (se lit surtout sur le formulaire entier). |

Le formulaire entier est lui-même un champ : `formulaire().invalid()`, `formulaire().dirty()`.

Un formulaire vide est invalide dès l'ouverture. Afficher dix messages rouges avant la première frappe serait hostile : on ne montre l'erreur d'un champ qu'une fois celui-ci **touché**.

Documentation : [état des champs](https://angular.dev/guide/forms/signals/field-state-management)

### Soumettre

On déclare ce que fait la soumission dans les options de `form` : `submission: { action: async (champs) => … }`. Quand l'utilisateur valide, `[formRoot]` déroule toujours la même séquence :

1. tous les champs sont marqués touchés : les erreurs cachées apparaissent ;
2. si une règle échoue, on s'arrête là : l'action n'est pas appelée ;
3. sinon l'action s'exécute, et `submitting()` vaut `true` jusqu'à sa fin ;
4. ce que l'action renvoie décide de la suite : rien, c'est un succès ; des erreurs, elles sont rattachées au formulaire.

Une erreur renvoyée par l'action porte un `kind`, un `message` et, si elle concerne un champ précis, un `fieldTree` qui le désigne. Sans `fieldTree`, elle est rattachée au formulaire entier.

L'essai a montré un comportement à connaître. Une erreur de soumission rend le formulaire invalide, et **empêche tout nouvel envoi** jusqu'à ce que l'utilisateur modifie quelque chose : le champ concerné si elle en désigne un, n'importe quel champ sinon. C'est le bon comportement pour un refus de l'API — renvoyer la même saisie donnerait la même réponse. Ce serait un piège pour une panne passagère : on ne renvoie donc **pas** les pannes comme erreurs de formulaire.

Documentation : [soumission](https://angular.dev/guide/forms/signals/form-submission)

### Signal Forms et Angular Material

Le référentiel demandait un essai avant d'écrire ce module. Résultat, avec Angular Material 22.2.7 : **l'association fonctionne, sans adaptation**.

| Contrôle | Constat |
| --- | --- |
| `<input matInput [formField]>` | Valeur liée dans les deux sens. Un champ `required` reçoit l'astérisque et `aria-required`. Un champ `disabled` est grisé. |
| `<input matInput type="number">` | Le modèle reçoit un `number` ; champ vidé, il reçoit `null`. Type du modèle : `number \| null`. |
| `<input matInput type="date">` | Le modèle reçoit la chaîne `'2026-05-04'` ; champ vidé, la chaîne vide. |
| `<mat-select [formField]>` | Valeurs chaînes ou nombres ; état désactivé repris. |
| `<mat-error>` | Affiché, et le champ passe en rouge, quand le champ est invalide **et** touché — donc aussi après une tentative de soumission. |
| `<input matInput [matDatepicker]>` | Fonctionne avec un modèle `Date \| null`. Voir la notion suivante. |

La documentation de Material le confirme pour `matInput`, qui « prend en charge `FormField` ». La seule chose qui reste à votre charge est d'écrire les `<mat-error>` : Material décide **quand** les montrer, pas **quoi** montrer.

Documentation : [champ de formulaire](https://material.angular.dev/components/form-field/overview), [`matInput`](https://material.angular.dev/components/input/overview), [liste déroulante](https://material.angular.dev/components/select/overview)

### Les dates

L'API attend `"2026-05-04"`. Deux contrôles sont possibles.

**`<input type="date">`**, que `matInput` habille. Sa valeur est toujours une chaîne `aaaa-mm-jj`, alors que le navigateur l'**affiche** dans la langue de l'utilisateur (`04/05/2026`) et fournit son propre calendrier. Le modèle contient donc exactement ce que l'API attend. Deux chaînes de ce format se comparent même directement : `'2026-05-03' < '2026-05-04'`.

**`mat-datepicker`** manipule des objets `Date` et a besoin d'un adaptateur. L'essai, avec l'adaptateur natif (`provideNativeDateAdapter()`) et la locale `fr-FR`, a mis en évidence deux pièges :

- la date s'affiche bien `04/05/2026`, mais « 08/05/2026 » **tapé au clavier** est compris comme le 5 août : l'adaptateur natif ne sait pas lire le format français ;
- convertir la `Date` obtenue par `toISOString()` donne la veille pour un utilisateur à l'est de Greenwich, car la date est à minuit, heure locale.

Le cours retient donc `<input type="date">`. Si un projet exige le calendrier de Material, il faut un autre adaptateur (date-fns ou Luxon) et une conversion écrite avec soin.

Documentation : [`<input type="date">`](https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/input/date), [sélecteur de date de Material](https://material.angular.dev/components/datepicker/overview)

### Ce que l'on saisit n'est pas ce que l'on envoie

L'utilisateur tape « 80 » dans un champ suivi de « % » ; l'API attend `0.8`. Il laisse la date de sortie vide ; l'API attend `null`. Le modèle du formulaire décrit **la saisie** ; la requête décrit **le contrat de l'API**. Entre les deux, une conversion explicite, écrite à un seul endroit. C'est la même séparation qu'entre entité et DTO au module 08.

### La validation de l'interface ne remplace pas celle de l'API

Elle sert l'utilisateur : il apprend tout de suite que la quotité ne peut pas dépasser 100 %. Elle ne protège rien : n'importe qui peut appeler l'API sans passer par le formulaire. Et certaines règles lui échappent par nature — l'unicité d'un matricule (RG-01) ne se décide qu'en base. Les règles vivent donc **dans l'API** ; l'interface en répète quelques-unes, par confort, et doit savoir afficher toutes les autres.

### Et les Reactive Forms ?

`FormGroup`, `FormControl`, `FormBuilder` et `ReactiveFormsModule` restent pris en charge, et vous les rencontrerez dans presque tout code Angular antérieur à 2026. Pour une nouvelle application en v22, la documentation oriente vers Signal Forms ; pour un code existant, elle recommande de garder son approche et fournit des passerelles pour migrer formulaire par formulaire.

Documentation : [comparaison des approches](https://angular.dev/guide/forms/signals/comparison), [migrer depuis les Reactive Forms](https://angular.dev/guide/forms/signals/migration)

## Pas à pas

### 1. Les requêtes et le service

Dans `collaborateur.ts`, ajoutez les miroirs des deux requêtes de l'API :

```ts
export type TypeContrat = 'Cdi' | 'Cdd' | 'Alternance' | 'Stage';

/** Miroir de CreerCollaborateurRequete. */
export interface CreerCollaborateurRequete {
  matricule: string;
  nom: string;
  prenom: string;
  email: string;
  categorie: CategorieCollaborateur;
  poste: string;
  dateEntree: string;
  typeContrat: TypeContrat;
  /** Fraction : 0.8 pour 80 %. */
  quotite: number;
  managerId: number | null;
}

/** Miroir de ModifierCollaborateurRequete. */
export interface ModifierCollaborateurRequete {
  nom: string;
  prenom: string;
  email: string;
  poste: string;
  dateSortie: string | null;
  managerId: number | null;
  version: string;
}
```

Les deux listes diffèrent : le matricule, la date d'entrée et le premier contrat se fixent à la création ; la date de sortie et la `version` n'existent qu'en modification. Dans `CollaborateursApi` :

```ts
creer(requete: CreerCollaborateurRequete): Observable<Collaborateur> {
  return this.http.post<Collaborateur>(this.url, requete);
}

modifier(id: number, requete: ModifierCollaborateurRequete): Observable<Collaborateur> {
  return this.http.put<Collaborateur>(`${this.url}/${id}`, requete);
}
```

### 2. Le modèle et un premier champ

Dans `formulaire-collaborateur.ts` :

```ts
/** Ce que l'utilisateur saisit. Tous les champs sont initialisés, jamais `undefined`. */
interface ModeleCollaborateur {
  matricule: string;
  nom: string;
  prenom: string;
  email: string;
  categorie: CategorieCollaborateur;
  poste: string;
  dateEntree: string;
  /** Chaîne vide : pas de date de sortie. */
  dateSortie: string;
  typeContrat: TypeContrat;
  /** Saisie en pourcentage (80) ; l'API attend une fraction (0.8). */
  quotite: number | null;
}

function versModele(c: Collaborateur | undefined): ModeleCollaborateur {
  return {
    matricule: c?.matricule ?? '',
    nom: c?.nom ?? '',
    prenom: c?.prenom ?? '',
    email: c?.email ?? '',
    categorie: c?.categorie ?? 'Consultant',
    poste: c?.poste ?? '',
    dateEntree: c?.dateEntree ?? '',
    dateSortie: c?.dateSortie ?? '',
    typeContrat: 'Cdi',
    // Création : 100 % par défaut. Fiche sans quotité (lue par un autre que le gestionnaire RH,
    // module 12) : champ vide plutôt qu'une valeur inventée.
    quotite: c === undefined ? 100 : c.quotite === undefined ? null : Math.round(c.quotite * 100),
  };
}
```

`versModele(undefined)` donne le formulaire vide de la création ; l'étape 7 lui passera un collaborateur. Un formulaire ne doit jamais afficher une valeur fausse : si l'API n'a pas envoyé la quotité, le champ reste vide. `Math.round` n'est pas une coquetterie : en JavaScript, `0.55 * 100` vaut `55.00000000000001`. Dans la classe, pour l'instant :

```ts
protected readonly modele = signal(versModele(undefined));
protected readonly formulaire = form(this.modele);
```

Et dans le template, après avoir ajouté `FormField`, `MatFormFieldModule` et `MatInputModule` aux `imports` :

```html
<mat-form-field>
  <mat-label>Nom</mat-label>
  <input matInput [formField]="formulaire.nom" />
</mat-form-field>

<pre>{{ modele() | json }}</pre>
```

`form` et `FormField` s'importent de `@angular/forms/signals`, `JsonPipe` de `@angular/common`. Tapez dans le champ : le modèle affiché suit chaque frappe. Aucun événement, aucun `subscribe`. Gardez ce `<pre>` le temps du module ; vous le retirerez à la fin.

### 3. Les règles

Donnez un schéma à `form` :

```ts
protected readonly formulaire = form(this.modele, (champs) => {
  required(champs.matricule, { message: 'Le matricule est obligatoire.' });
  required(champs.nom, { message: 'Le nom est obligatoire.' });
  required(champs.prenom, { message: 'Le prénom est obligatoire.' });
  required(champs.email, { message: "L'adresse électronique est obligatoire." });
  email(champs.email, { message: "L'adresse électronique est invalide." });
  required(champs.poste, { message: 'Le poste est obligatoire.' });
  required(champs.dateEntree, { message: "La date d'entrée est obligatoire." });

  // RG-03 : la quotité est comprise entre 10 % et 100 %.
  required(champs.quotite, { message: 'La quotité est obligatoire.' });
  min(champs.quotite, 10, { message: 'La quotité est au minimum de 10 %.' });
  max(champs.quotite, 100, { message: 'La quotité est au maximum de 100 %.' });
});
```

Le champ « Nom » porte maintenant un astérisque : `[formField]` a transmis à Material qu'il est obligatoire. Rien d'autre ne s'affiche — le champ n'a pas été touché.

### 4. Les champs et leurs erreurs

Chaque champ suit le même motif. Voici les quatre sortes de contrôles du formulaire :

```html
<mat-form-field>
  <mat-label>Nom</mat-label>
  <input matInput [formField]="formulaire.nom" />
  @for (erreur of formulaire.nom().errors(); track erreur.kind) {
    <mat-error>{{ erreur.message }}</mat-error>
  }
</mat-form-field>

<mat-form-field>
  <mat-label>Catégorie</mat-label>
  <mat-select [formField]="formulaire.categorie">
    <mat-option value="Consultant">Consultant</mat-option>
    <mat-option value="Interne">Interne</mat-option>
  </mat-select>
</mat-form-field>

<mat-form-field>
  <mat-label>Date d'entrée</mat-label>
  <input matInput type="date" [formField]="formulaire.dateEntree" />
  @for (erreur of formulaire.dateEntree().errors(); track erreur.kind) {
    <mat-error>{{ erreur.message }}</mat-error>
  }
</mat-form-field>

<mat-form-field>
  <mat-label>Quotité</mat-label>
  <input matInput type="number" [formField]="formulaire.quotite" />
  <span matTextSuffix>%</span>
  @for (erreur of formulaire.quotite().errors(); track erreur.kind) {
    <mat-error>{{ erreur.message }}</mat-error>
  }
</mat-form-field>
```

Écrivez les autres sur ce modèle (`MatSelectModule` rejoint les `imports`). Il n'y a pas de `@if (touched())` autour des `<mat-error>` : Material ne les montre que si le champ est invalide et touché.

Cliquez dans « Nom », puis ailleurs : le champ passe en rouge, avec son message. Tapez « 5 » dans la quotité et sortez du champ : RG-03.

### 5. Soumettre

Entourez les champs d'un formulaire lié par `[formRoot]` (ajoutez `FormRoot` aux `imports`), terminé par un bouton :

```html
<form [formRoot]="formulaire">
  <!-- les champs -->

  <div class="actions">
    <button matButton="filled" type="submit" [disabled]="formulaire().submitting()">Créer</button>
  </div>
</form>
```

Puis déclarez l'action, en troisième argument de `form` :

```ts
protected readonly formulaire = form(
  this.modele,
  (champs) => {
    // les règles de l'étape 3
  },
  {
    submission: {
      action: async (champs) => {
        const m = champs().value();
        const cree = await firstValueFrom(
          this.api.creer({
            matricule: m.matricule.trim(),
            nom: m.nom.trim(),
            prenom: m.prenom.trim(),
            email: m.email.trim(),
            categorie: m.categorie,
            poste: m.poste.trim(),
            dateEntree: m.dateEntree,
            typeContrat: m.typeContrat,
            quotite: (m.quotite ?? 0) / 100,
            managerId: null,
          }),
        );
        await this.router.navigate(['/collaborateurs', cree.id]);
      },
    },
  },
);
```

- **`firstValueFrom`** (de `rxjs`) transforme l'Observable de `HttpClient` en promesse : l'action attend la réponse avec `await`.
- **La conversion de la quotité** se fait ici, et nulle part ailleurs.
- **Le bouton n'est pas désactivé quand le formulaire est invalide.** Un bouton grisé n'explique rien ; un clic, lui, marque tous les champs touchés et fait apparaître ce qui manque.
- `api` et `router` (`inject(Router)`) doivent être déclarés avant `formulaire`.

Cliquez sur « Créer » sans rien saisir : tous les messages apparaissent, et l'onglet *Réseau* reste vide. Remplissez le formulaire : une requête `POST`, puis la fiche du nouveau collaborateur.

### 6. Les refus de l'API

Créez un second collaborateur avec le même matricule. L'API répond `409`, l'intercepteur du module 16 affiche une notification, et l'action lève une exception que personne ne traite. Un formulaire mérite mieux : l'erreur doit s'afficher **dans** le formulaire, et y rester.

**Compléter le contrat de l'API.** Avant d'afficher ces refus, regardez ce que l'API envoie. Deux manques apparaissent, et c'est ce premier client qui les révèle :

- **Les `409` se ressemblent tous.** Matricule déjà pris, adresse déjà prise, version périmée : même statut, même titre, seul le texte change. L'étape 8 proposera « Recharger la fiche » après un conflit de version, et seulement dans ce cas : recharger ne sert à rien quand l'adresse est prise, et efface la saisie. L'interface ne doit pas lire le texte du message pour décider.
- **Les messages d'un `400` sont en anglais** partout sauf dans `CreerCollaborateurRequete` (exercice 4 du module 08) : « The Nom field is required. » pour `PUT /api/collaborateurs/{id}` ou `POST /api/projets`. Le formulaire les montrera tels quels : ils doivent être en français.

Dans `Chronos.Api`, faites l'aller-retour côté serveur. `Commun/Reponses.cs` :

```csharp
/// <summary>
/// 409 : la demande est correcte, mais l'état de la ressource ne la permet pas.
/// <paramref name="regle"/> : la règle de gestion en cause (unicité), dans l'extension « regle », comme pour un 422.
/// </summary>
public static ProblemHttpResult Conflit(string detail, string? regle = null) =>
    TypedResults.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Conflit",
        detail: detail,
        extensions: regle is null ? null : new Dictionary<string, object?> { ["regle"] = regle });
```

Dans `Erreurs/ConflitExceptionHandler.cs`, le conflit de version reçoit un code stable, l'extension `code` :

```csharp
var (detail, code) = exception switch
{
    DbUpdateConcurrencyException =>
        ("La ressource a été modifiée ou supprimée depuis que vous l'avez lue. Rechargez-la, puis recommencez.",
         "concurrence"),
    DbUpdateException e when ErreursBaseDeDonnees.EstViolationUnicite(e) =>
        ("Une ressource porte déjà cette valeur, qui doit être unique.", (string?)null),
    _ => ((string?)null, (string?)null),
};
```

puis, dans le `ProblemDetails` écrit plus bas : `if (code is not null) { problemDetails.Extensions["code"] = code; }` (sortez l'objet dans une variable `problemDetails` pour pouvoir le compléter).

Qui porte quoi :

| Réponse | Extension |
| --- | --- |
| `409` matricule ou adresse déjà pris, en création et en modification (`CollaborateursEndpoints`) | `"regle": "RG-01"` |
| `409` code de projet déjà pris (`ProjetsEndpoints`) | `"regle": "RG-10"` |
| `409` version périmée (`ConflitExceptionHandler`) | `"code": "concurrence"` |
| autres `409` (transition de statut, client qui a des projets, jour férié en double, compte existant) | aucune |

Donnez enfin un `ErrorMessage` en français à toutes les annotations des requêtes qui n'en ont pas encore : `ModifierCollaborateurRequete`, `CreerProjetRequete`, `ModifierProjetRequete`, et toute autre requête annotée, sur le modèle de `CreerCollaborateurRequete`.

Ajoutez les tests d'intégration : un matricule déjà pris renvoie `409` avec `regle` = `RG-01` et sans `code` ; une adresse déjà prise en modification, de même ; un code de projet déjà pris, `regle` = `RG-10` ; une modification avec une version périmée, `code` = `concurrence` et sans `regle` ; un `PUT /api/collaborateurs/{id}` au nom vide répond « Le nom est obligatoire. ».

Aucune migration : le modèle de données ne change pas. Relancez l'API, puis rejouez le matricule en double : la réponse porte `"regle": "RG-01"`.

Première pièce : permettre à une requête de dire à l'intercepteur « je m'occupe de mes erreurs de saisie ». Dans `erreur-api.ts`, ajoutez à la classe `ErreurApi` :

```ts
/** L'API a refusé les données envoyées : à l'utilisateur de les corriger. */
get estDeSaisie(): boolean {
  return this.statut === 400 || this.statut === 409 || this.statut === 422;
}
```

Profitez-en pour lire l'extension `code` que vous venez d'ajouter à l'API : `"concurrence"` pour un `409` de version périmée. Un dernier paramètre du constructeur, `readonly code: string | null = null`, un champ `code?: string` dans l'interface `ProblemDetails`, et `probleme.code ?? null` en dernier argument dans `versErreurApi`. L'étape 8 s'en servira.

Dans `erreurs-interceptor.ts` :

```ts
/**
 * À mettre à `true` sur la requête d'un formulaire : il affiche lui-même les erreurs de saisie
 * (400, 409, 422), l'intercepteur ne les notifie donc pas.
 */
export const ERREURS_DE_SAISIE_GEREES = new HttpContextToken<boolean>(() => false);
```

```ts
const erreur = versErreurApi(reponse);
const geree = req.context.get(ERREURS_DE_SAISIE_GEREES) && erreur.estDeSaisie;
if (req.method !== 'GET' && !geree) {
  snackBar.open(erreur.message, 'Fermer', { duration: 8000 });
}
```

Un `HttpContextToken` est une étiquette qu'on attache à une requête et que les intercepteurs peuvent lire. Dans `CollaborateursApi`, `creer` et `modifier` la posent :

```ts
/** Les formulaires affichent eux-mêmes les erreurs de saisie. */
private readonly depuisUnFormulaire = {
  context: new HttpContext().set(ERREURS_DE_SAISIE_GEREES, true),
};

creer(requete: CreerCollaborateurRequete): Observable<Collaborateur> {
  return this.http.post<Collaborateur>(this.url, requete, this.depuisUnFormulaire);
}
```

Une panne ou une erreur 500 reste notifiée par l'intercepteur : le formulaire n'a rien à en dire.

Deuxième pièce : traduire une `ErreurApi` en erreurs de formulaire. `src/app/commun/erreurs-de-formulaire.ts` :

```ts
import { FieldTree, isFieldTree, ValidationError } from '@angular/forms/signals';
import { ErreurApi } from './erreur-api';

/**
 * Traduit une erreur de l'API en erreurs de formulaire.
 * - 400 : chaque message est rattaché à son champ ;
 * - 409, 422, ou 400 dont aucun champ n'est reconnu : une erreur sur le formulaire entier, avec
 *   le message général ; son `kind` est la règle (`RG-xx`) ou le code (`concurrence`) quand l'API
 *   les donne, sinon `http-400`, `http-409` ou `http-422` ;
 * - autre chose (panne, 500) : aucune erreur de formulaire, l'intercepteur a déjà notifié.
 */
export function erreursDeFormulaire(
  erreur: unknown,
  formulaire: FieldTree<unknown>,
): ValidationError.WithOptionalFieldTree[] {
  if (!(erreur instanceof ErreurApi) || !erreur.estDeSaisie) {
    return [];
  }

  const champs = formulaire as unknown as Record<string, unknown>;
  const erreurs: ValidationError.WithOptionalFieldTree[] = [];

  for (const [nom, messages] of Object.entries(erreur.champs)) {
    // L'API écrit « Nom » ou « nom » selon sa configuration : on accepte les deux.
    const champ = champs[nom.charAt(0).toLowerCase() + nom.slice(1)];
    if (isFieldTree(champ)) {
      for (const message of messages) {
        erreurs.push({ kind: 'serveur', message, fieldTree: champ });
      }
    }
  }

  if (erreurs.length === 0) {
    erreurs.push({
      kind: erreur.regle ?? erreur.code ?? `http-${erreur.statut}`,
      message: erreur.message,
    });
  }
  return erreurs;
}
```

- **Les clés de `errors` portent les noms C#** (`Email`, `Quotite`), les champs du formulaire leurs noms TypeScript (`email`, `quotite`) : mettre la première lettre en minuscule suffit à les rapprocher — à condition de nommer les champs du modèle comme ceux de la requête.
- **Ce que la fonction ne sait pas placer va au formulaire entier**, avec le message général : le `409`, qui dit « ce matricule ou cette adresse » sans désigner de champ, et une clé comme `$.version` (un corps JSON illisible), dont le message technique, en anglais, n'apprendrait rien à l'utilisateur.
- **Les messages d'un `400` sont montrés tels quels.** Ce sont ceux des `ErrorMessage` des requêtes de l'API, que vous avez tous écrits en français. Ce cas reste rare : vos propres règles passent avant, et le formulaire n'envoie rien tant qu'elles échouent.

Dernière pièce : l'action attrape l'erreur et renvoie sa traduction.

```ts
action: async (champs) => {
  try {
    await this.enregistrer(champs().value());
    return;
  } catch (erreur) {
    return erreursDeFormulaire(erreur, champs);
  }
},
```

`enregistrer` est une méthode privée qui reprend l'envoi et la navigation de l'étape 5. Les erreurs rattachées à un champ apparaissent dans ses `<mat-error>`, sans rien changer. Celles du formulaire entier demandent un affichage, juste avant les boutons :

```html
@for (erreur of formulaire().errors(); track erreur.kind) {
  <p role="alert">{{ erreur.message }}</p>
}
```

Recommencez l'essai du matricule en double : le message de l'API s'affiche dans le formulaire, sans notification. Cliquez de nouveau sur « Créer » : rien ne part. Corrigez le matricule : le message disparaît et l'envoi redevient possible.

### 7. Créer et modifier avec le même composant

Le composant reçoit maintenant, s'il s'agit d'une modification, le collaborateur à modifier :

```ts
/** Absent : création. Présent : modification de ce collaborateur. */
readonly collaborateur = input<Collaborateur>();
readonly enregistre = output<Collaborateur>();
readonly annule = output<void>();

protected readonly modele = linkedSignal(() => versModele(this.collaborateur()));
```

Le modèle devient un `linkedSignal` : modifiable comme un signal ordinaire, et recalculé si l'entrée `collaborateur` change. Le schéma s'adapte au mode :

```ts
const enCreation = () => this.collaborateur() === undefined;
const enModification = () => this.collaborateur() !== undefined;

// RG-02 : la date de sortie ne précède pas la date d'entrée.
validate(champs.dateSortie, ({ value, valueOf }) => {
  const sortie = value();
  const entree = valueOf(champs.dateEntree);
  return sortie !== '' && entree !== '' && sortie < entree
    ? { kind: 'RG-02', message: "La date de sortie ne peut pas précéder la date d'entrée." }
    : null;
});

// Ce que l'API n'accepte qu'à la création n'est plus modifiable ensuite.
disabled(champs.matricule, { when: enModification });
disabled(champs.categorie, { when: enModification });
disabled(champs.dateEntree, { when: enModification });
disabled(champs.quotite, { when: enModification });
hidden(champs.typeContrat, { when: enModification });
hidden(champs.dateSortie, { when: enCreation });
```

- **`valueOf(champs.dateEntree)`** lit un autre champ ; la règle est réévaluée si l'une des deux dates change.
- **Le `kind` est le code de la règle** : le même vocabulaire que l'API et le cahier des charges.
- **Les conditions sont des fonctions** qui lisent un signal : les règles suivent l'entrée `collaborateur`.

Dans le template, un champ masqué n'est pas affiché :

```html
@if (!formulaire.dateSortie().hidden()) {
  <mat-form-field>
    <mat-label>Date de sortie</mat-label>
    <input matInput type="date" [formField]="formulaire.dateSortie" />
    @for (erreur of formulaire.dateSortie().errors(); track erreur.kind) {
      <mat-error>{{ erreur.message }}</mat-error>
    }
  </mat-form-field>
}
```

Faites de même pour le type de contrat. La quotité, elle, reste visible en modification, mais grisée : elle ne change que par un nouveau contrat. La méthode `enregistrer` choisit la requête :

```ts
private async enregistrer(m: ModeleCollaborateur): Promise<void> {
  const communs = {
    nom: m.nom.trim(),
    prenom: m.prenom.trim(),
    email: m.email.trim(),
    poste: m.poste.trim(),
  };
  const existant = this.collaborateur();

  if (existant) {
    const modifie = await firstValueFrom(
      this.api.modifier(existant.id, {
        ...communs,
        dateSortie: m.dateSortie === '' ? null : m.dateSortie,
        managerId: existant.managerId,
        version: existant.version,
      }),
    );
    this.enregistre.emit(modifie);
  } else {
    const cree = await firstValueFrom(
      this.api.creer({
        ...communs,
        matricule: m.matricule.trim(),
        categorie: m.categorie,
        dateEntree: m.dateEntree,
        typeContrat: m.typeContrat,
        quotite: (m.quotite ?? 0) / 100,
        managerId: null,
      }),
    );
    await this.router.navigate(['/collaborateurs', cree.id]);
  }
}
```

La `version` lue avec la fiche repart telle quelle : c'est le jeton de concurrence du module 08.

Reste à ouvrir le formulaire depuis la fiche. Dans `fiche-collaborateur.ts` :

```ts
protected readonly enEdition = signal(false);

protected terminer(enregistre: Collaborateur): void {
  // L'API a renvoyé la fiche à jour, avec sa nouvelle version : inutile de la redemander.
  this.collaborateur.set(enregistre);
  this.enEdition.set(false);
}
```

```html
@if (enEdition()) {
  <app-formulaire-collaborateur
    [collaborateur]="c"
    (enregistre)="terminer($event)"
    (annule)="enEdition.set(false)"
  />
} @else {
  <!-- la carte, avec un bouton « Modifier » : (click)="enEdition.set(true)" -->
}
```

Une ressource est modifiable : `collaborateur.set(...)` remplace sa valeur sans appeler l'API, et son état devient `'local'`. Dans le formulaire, adaptez le titre et le libellé du bouton au mode, et ajoutez un bouton « Annuler », de type `button`, qui émet `annule`.

### 8. La modification concurrente

Ouvrez la même fiche dans deux onglets, cliquez sur « Modifier » dans chacun. Enregistrez dans le premier, puis dans le second : `409`, « La ressource a été modifiée ou supprimée depuis que vous l'avez lue. Rechargez-la, puis recommencez. » L'étape 6 affiche déjà ce message. Il manque le moyen d'en sortir.

Le statut ne suffit pas à reconnaître ce cas : une adresse électronique déjà prise répond aussi `409`, et recharger la fiche n'y changerait rien — sinon perdre la saisie. Depuis l'étape 6, l'API les distingue : `"regle": "RG-01"` pour l'unicité, `"code": "concurrence"` pour la version périmée, que `erreursDeFormulaire` reporte dans le `kind`. Dans le formulaire :

```ts
/** Demande au parent de recharger la fiche après un conflit (409). */
readonly recharger = output<void>();
```

```html
@for (erreur of formulaire().errors(); track erreur.kind) {
  <p role="alert">
    {{ erreur.message }}
    @if (erreur.kind === 'concurrence') {
      <button matButton type="button" (click)="recharger.emit()">Recharger la fiche</button>
    }
  </p>
}
```

Et dans la fiche : `(recharger)="collaborateur.reload()"`. La ressource redemande la fiche ; l'entrée du formulaire change ; le `linkedSignal` repart de la nouvelle valeur, avec la nouvelle `version`. La saisie en cours est perdue : c'est le prix de la solution la plus simple, à discuter en séance.

Retirez le `<pre>` de l'étape 2.

Un mot sur les tests de ce formulaire. `[formRoot]` lance la soumission sans en garder la promesse, et Angular ne la suit pas : après la réponse d'erreur d'une requête simulée, `await fixture.whenStable()` rend la main **avant** que l'action ait rattaché ses erreurs (constaté, même appelé deux fois). Attendez ce que l'utilisateur voit à la fin d'un envoi — le bouton redevient cliquable quand `submitting()` repasse à `false`, au moment même où les erreurs sont posées :

```ts
await vi.waitFor(() => expect(bouton('Créer').disabled).toBe(false));
```

`vi.waitFor` réessaie l'assertion jusqu'à ce qu'elle passe (une seconde au plus) : c'est une attente sur un état, pas une durée choisie au hasard. Puis vérifiez :

```powershell
npm test
```

```powershell
npm run build
```

## À vous

1. **Le formulaire de projet.** Écrivez `FormulaireProjet` (création, route `/projets/nouveau`). RG-11 : le champ « Client » est une liste déroulante alimentée par `/api/clients` ; il est obligatoire pour un projet facturable, et n'existe pas pour un projet interne — `hidden` et `required` suffisent. Que contient le modèle après le passage de « Facturable » à « Interne » ? Qu'envoyez-vous à l'API ? RG-12 : sur le modèle de RG-02. Affichez le `409` d'un code déjà pris (RG-10).

2. **Le manager.** Ajoutez au formulaire du collaborateur le choix du manager, dans une liste chargée depuis l'API. Provoquez RG-04 (un collaborateur manager de son propre manager) : où s'affiche le refus ? Pourquoi l'interface ne peut-elle pas vérifier cette règle elle-même ?

3. **Voir les erreurs 400.** Votre validation empêche d'envoyer un formulaire invalide : vous ne voyez donc jamais les erreurs par champ de l'API. Désactivez provisoirement vos règles `required` et soumettez un formulaire vide. Les messages arrivent-ils sur les bons champs ? Dans quelle langue ? Retirez un `ErrorMessage` de l'API, le temps d'un essai : que voit l'utilisateur ?

4. **Ne plus se répéter.** La boucle `@for … <mat-error>` est recopiée dans chaque champ. Peut-on la confier à un composant `ErreursDeChamp` ? Essayez, et observez ce que devient l'affichage dans `mat-form-field`. Concluez : factoriser, ou non ?

5. **Le calendrier de Material.** Sur une branche, remplacez un `<input type="date">` par `mat-datepicker`. Quel type prend le champ du modèle ? Tapez « 08/05/2026 » au clavier : quelle date obtenez-vous ? Écrivez la conversion vers `"2026-05-08"` sans `toISOString()`, et dites pourquoi.

6. **Ne pas perdre une saisie.** Un clic sur « Annuler » efface sans prévenir dix champs modifiés. Avec `formulaire().dirty()`, demandez confirmation. Et si l'utilisateur quitte la page par le menu ? Cherchez dans le guide du routage quelle garde répond à ce besoin.

## Critères de réussite

- `/collaborateurs/nouveau` crée un collaborateur ; la requête `POST` contient `"quotite": 0.8` pour une saisie de 80, et ne contient pas `dateSortie`.
- Aucun message d'erreur à l'ouverture ; chacun apparaît quand on quitte son champ, et tous après un clic sur « Créer ».
- Un formulaire invalide n'envoie aucune requête.
- Un matricule déjà pris affiche le message de l'API **dans** le formulaire, sans notification ; API arrêtée, c'est une notification, et un second clic renvoie la requête.
- Depuis la fiche, « Modifier » ouvre le même composant, prérempli ; le matricule, la catégorie, la date d'entrée et la quotité y sont grisés ; la requête `PUT` porte la `version`.
- Une date de sortie antérieure à la date d'entrée affiche RG-02 sans appeler l'API.
- Deux onglets sur la même fiche : le second enregistrement affiche le conflit et « Recharger la fiche ». Une adresse électronique déjà prise affiche son refus, **sans** ce bouton.
- Côté API : un matricule déjà pris répond `409` avec `"regle": "RG-01"`, une version périmée `409` avec `"code": "concurrence"`, un `PUT` au nom vide « Le nom est obligatoire. » ; `dotnet test` réussit.
- `npm test` et `npm run build` réussissent.
- Comparez avec la solution de référence : `git diff module-16 module-17`.

## Pièges

| Ce qu'on voit / ce qu'on fait | Pourquoi c'est un problème | À faire à la place |
| --- | --- | --- |
| `ReactiveFormsModule`, `FormGroup`, `FormBuilder` dans un écran neuf | Pris en charge, mais réservé au code existant. | Signal Forms. |
| `[(ngModel)]` et `FormsModule` | Formulaires pilotés par le template : ni schéma, ni typage du modèle. | `[formField]`. |
| `[field]="…"`, vu dans les articles de 2025 | Nom de la directive pendant la phase expérimentale. | `[formField]`. |
| Un champ du modèle initialisé à `undefined` | Il est absent de l'arbre de champs : `formulaire.x` n'existe pas. | `''` ou `null`. |
| `effect` qui recopie la fiche chargée dans le modèle | Usage à éviter de `effect`. | `linkedSignal`. |
| `[disabled]="formulaire().invalid()"` sur le bouton d'envoi | L'utilisateur ne sait pas ce qui manque. | Laisser cliquer : la soumission montre les erreurs. |
| `@if (champ().touched())` autour de `<mat-error>` | Redondant : Material le fait. | La boucle seule. |
| Attribut `required` ou `min` sur l'`<input>` | Deux sources de vérité ; la validation native est désactivée. | Les règles du schéma. |
| Compter sur `hidden()` pour masquer le champ | La règle ne touche pas à l'affichage. | `@if (!champ().hidden())`. |
| Envoyer le modèle tel quel à l'API | `80` au lieu de `0.8`, `''` au lieu de `null`, champs en trop. | Une conversion explicite vers la requête. |
| `date.toISOString().slice(0, 10)` | Décale d'un jour selon le fuseau. | Garder la chaîne d'`<input type="date">`. |
| Renvoyer une panne réseau comme erreur de formulaire | Bloque tout nouvel envoi tant que rien n'est modifié. | Notification, et rien dans le formulaire. |
| `disabled(champ, condition)`, `hidden(champ, condition)` | Surcharges dépréciées en v22. | `disabled(champ, { when: condition })`. |
| Reconnaître un conflit de version à son seul statut `409` | L'unicité répond aussi `409` : « Recharger » perdrait la saisie pour rien. | Le `code` (`concurrence`) ou la `regle` du ProblemDetails. |
| Dans un test, `whenStable()` puis lire les erreurs d'une soumission refusée | La soumission n'est pas suivie par Angular : l'assertion passe trop tôt. | `vi.waitFor` sur la fin visible de l'envoi (bouton réactivé). |
| « C'est validé côté client » | N'importe qui appelle l'API directement. | L'API valide tout ; l'interface répète pour le confort. |

La liste générale est dans le [référentiel Angular](../docs/references/angular-22.md#4-pièges--ce-quon-trouve-encore-partout-et-quon-nécrit-plus).

## Pour la séance

- RG-03 est écrite trois fois : dans le domaine, dans le schéma du formulaire, dans le cahier des charges. Que se passe-t-il le jour où la borne passe à 5 % ? Peut-on l'écrire une seule fois ?
- Après un conflit, « Recharger la fiche » efface la saisie. Quelles autres réponses imaginez-vous, et que coûteraient-elles ?
- Le `409` d'unicité ne dit pas quel champ est en cause. À qui revient-il de le dire : à l'API, à l'interface ? Que changeriez-vous, et où ?
- Les erreurs 400 arrivent avec les noms de champs C#, et en anglais tant qu'une annotation n'a pas son `ErrorMessage`. Est-ce à l'API ou à l'interface de s'adapter ?
- Pourquoi le formulaire émet-il `enregistre` en modification, mais navigue-t-il lui-même en création ? Est-ce cohérent ? Proposez autre chose.
- Vous rejoignez une équipe dont tous les formulaires sont en Reactive Forms. Que faites-vous du prochain formulaire à écrire ?

## Pour aller plus loin

- [Validation asynchrone](https://angular.dev/guide/forms/signals/validation) : `validateHttp`, pour interroger l'API pendant la saisie — par exemple sur la disponibilité d'un matricule.
- [Logique de formulaire](https://angular.dev/guide/forms/signals/form-logic) : `debounce`, pour retarder la mise à jour du modèle, et `applyWhen`, pour activer un groupe de règles sous condition.
- [Contrôles sur mesure](https://angular.dev/guide/forms/signals/custom-controls) : écrire un composant qui s'utilise avec `[formField]`.
- [Reactive Forms](https://angular.dev/guide/forms/reactive-forms) : pour lire le code existant.
- Suite dans le cours : le module 19 met ces champs en lecture seule selon le rôle (RG-52) ; le module 20 teste ce formulaire avec les harnais de Material.
