import { Routes } from '@angular/router';
import { roleGuardAvecRepli } from '../compte/role-guard';
import { ListeCollaborateurs } from './liste-collaborateurs/liste-collaborateurs';

export const collaborateursRoutes: Routes = [
  {
    path: '',
    component: ListeCollaborateurs,
    title: 'Collaborateurs — Chronos',
  },
  {
    path: 'nouveau',
    // canActivate, et non canMatch : avec canMatch, la route suivante (:id) prendrait le relais
    // et /collaborateurs/nouveau afficherait la fiche d'un collaborateur nommé « nouveau ».
    canActivate: [roleGuardAvecRepli('/collaborateurs', 'GestionnaireRh')],
    loadComponent: () =>
      import('./formulaire-collaborateur/formulaire-collaborateur').then(
        (m) => m.FormulaireCollaborateur,
      ),
    title: 'Nouveau collaborateur — Chronos',
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./fiche-collaborateur/fiche-collaborateur').then((m) => m.FicheCollaborateur),
    title: 'Fiche collaborateur — Chronos',
  },
];
