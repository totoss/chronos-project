import { MatPaginatorIntl } from '@angular/material/paginator';
import { Routes } from '@angular/router';
import { paginatorFrancais } from '../commun/paginator-francais';
import { roleGuardAvecRepli } from '../compte/role-guard';
import { ListeCollaborateurs } from './liste-collaborateurs/liste-collaborateurs';

export const collaborateursRoutes: Routes = [
  {
    path: '',
    component: ListeCollaborateurs,
    // Les libellés du paginateur sont en anglais dans Material ; fournis ici, pour ne pas charger le paginateur au démarrage.
    providers: [{ provide: MatPaginatorIntl, useFactory: paginatorFrancais }],
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
