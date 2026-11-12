import { Routes } from '@angular/router';
import { roleGuard } from '../compte/role-guard';
import { ListeProjets } from './liste-projets/liste-projets';

export const projetsRoutes: Routes = [
  {
    path: '',
    component: ListeProjets,
    title: 'Projets — Chronos',
  },
  {
    path: 'nouveau',
    canMatch: [roleGuard('Manager', 'GestionnaireRh')],
    loadComponent: () =>
      import('./formulaire-projet/formulaire-projet').then((m) => m.FormulaireProjet),
    title: 'Nouveau projet — Chronos',
  },
];
