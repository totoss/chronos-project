import { Routes } from '@angular/router';
import { ListeProjets } from './liste-projets/liste-projets';

export const projetsRoutes: Routes = [
  {
    path: '',
    component: ListeProjets,
    title: 'Projets — Chronos',
  },
];
