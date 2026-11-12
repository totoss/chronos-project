import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Session } from './session';

/** Exige d'être connecté ; sinon, renvoie à la connexion en retenant l'adresse demandée. */
export const authGuard: CanActivateFn = (route, state) => {
  if (inject(Session).estConnecte()) {
    return true;
  }
  return inject(Router).createUrlTree(['/connexion'], { queryParams: { retour: state.url } });
};
