import { Routes } from '@angular/router';
import { ListeProjets } from './liste-projets/liste-projets';

export const projetsRoutes: Routes = [
  {
    path: '',
    component: ListeProjets,
    title: 'Projets — Chronos',
  },
  {
    path: 'nouveau',
    loadComponent: () =>
      import('./formulaire-projet/formulaire-projet').then((m) => m.FormulaireProjet),
    title: 'Nouveau projet — Chronos',
  },
];
