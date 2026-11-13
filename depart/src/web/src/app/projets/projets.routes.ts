import { Routes } from '@angular/router';

export const projetsRoutes: Routes = [
  // TODO [module 15] Route '' vers `ListeProjets`, avec un `title` (branchée par `loadChildren`
  //    dans app.routes.ts).
  //    — cours/15-routage.md, À vous, exercice 1 « Les autres écrans. »
  // TODO [module 17] Ajouter 'nouveau' vers `FormulaireProjet`, chargé à la demande, avec un
  //    `title`.
  //    — cours/17-formulaires.md, À vous, exercice 1 « Le formulaire de projet. »
  // TODO [module 19] Réserver 'nouveau' aux rôles Manager et GestionnaireRh par `canMatch` avec
  //    `roleGuard`.
  //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 6 « Réserver une route à un rôle : `canMatch` »
  // TODO [module 21] Fournir `MatPaginatorIntl` par `paginatorFrancais` dans les `providers` de la
  //    route de la liste.
  //    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 1 « Passer l'application en français »
];
