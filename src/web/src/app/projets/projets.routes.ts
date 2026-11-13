import { MatPaginatorIntl } from '@angular/material/paginator';
import { Routes } from '@angular/router';
import { paginatorFrancais } from '../commun/paginator-francais';
import { roleGuard } from '../compte/role-guard';
import { ListeProjets } from './liste-projets/liste-projets';

export const projetsRoutes: Routes = [
  {
    path: '',
    component: ListeProjets,
    // Les libellés du paginateur sont en anglais dans Material ; fournis ici, pour ne pas charger le paginateur au démarrage.
    providers: [{ provide: MatPaginatorIntl, useFactory: paginatorFrancais }],
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
