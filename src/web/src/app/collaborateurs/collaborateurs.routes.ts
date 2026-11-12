import { Routes } from '@angular/router';
import { ListeCollaborateurs } from './liste-collaborateurs/liste-collaborateurs';

export const collaborateursRoutes: Routes = [
  {
    path: '',
    component: ListeCollaborateurs,
    title: 'Collaborateurs — Chronos',
  },
  {
    path: 'nouveau',
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
