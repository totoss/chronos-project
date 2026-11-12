import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'collaborateurs' },
  {
    path: 'collaborateurs',
    loadChildren: () =>
      import('./collaborateurs/collaborateurs.routes').then((m) => m.collaborateursRoutes),
  },
  {
    path: 'projets',
    loadChildren: () => import('./projets/projets.routes').then((m) => m.projetsRoutes),
  },
  {
    path: 'plan-de-charge',
    loadComponent: () =>
      import('./plan-de-charge/plan-de-charge/plan-de-charge').then((m) => m.PlanDeCharge),
    title: 'Plan de charge — Chronos',
  },
  {
    path: 'feuille-de-temps',
    loadComponent: () =>
      import('./feuilles-de-temps/feuille-de-temps-provisoire/feuille-de-temps-provisoire').then(
        (m) => m.FeuilleDeTempsProvisoire,
      ),
    title: 'Feuille de temps — Chronos',
  },
  {
    path: 'absences',
    loadComponent: () =>
      import('./absences/liste-absences/liste-absences').then((m) => m.ListeAbsences),
    title: 'Absences — Chronos',
  },
  {
    path: 'connexion',
    loadComponent: () =>
      import('./compte/connexion-provisoire/connexion-provisoire').then(
        (m) => m.ConnexionProvisoire,
      ),
    title: 'Connexion — Chronos',
  },
  {
    path: '**',
    loadComponent: () =>
      import('./commun/page-introuvable/page-introuvable').then((m) => m.PageIntrouvable),
    title: 'Page introuvable — Chronos',
  },
];
