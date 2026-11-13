import { Routes } from '@angular/router';

export const collaborateursRoutes: Routes = [
  // TODO [module 15] Route '' vers `ListeCollaborateurs`, référencée directement, avec un `title`.
  //    — cours/15-routage.md, Pas à pas, étape 5 « Les routes des collaborateurs »
  // TODO [module 15] Route 'nouveau' vers `FormulaireCollaborateur` (chargé à la demande), AVANT
  //    ':id' : sinon « nouveau » serait pris pour un identifiant.
  //    — cours/15-routage.md, Pas à pas, étape 5 « Les routes des collaborateurs »
  // TODO [module 15] Route ':id' vers `FicheCollaborateur` (chargée à la demande), avec un `title`.
  //    — cours/15-routage.md, Pas à pas, étape 5 « Les routes des collaborateurs »
  // TODO [module 19] Protéger 'nouveau' par `canActivate` avec `roleGuardAvecRepli` (repli vers
  //    /collaborateurs, rôle GestionnaireRh), et non `canMatch` : voir l'encadré sur `canMatch`
  //    (RG-52).
  //    — cours/19-authentification-cote-interface.md, Pas à pas, étape 6 « Réserver une route à un rôle : `canMatch` »
  // TODO [module 21] Fournir `MatPaginatorIntl` par `paginatorFrancais` dans les `providers` de la
  //    route de la liste.
  //    — cours/21-accessibilite-et-finitions.md, Pas à pas, étape 1 « Passer l'application en français »
];
