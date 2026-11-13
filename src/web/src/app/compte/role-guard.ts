import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { Session } from './session';
import { Role } from './utilisateur';

/** Pour `canMatch` : sans le rôle, la route n'existe pas, et le routeur essaie les suivantes. */
export function roleGuard(...roles: Role[]): CanMatchFn {
  return () => inject(Session).aLeRole(...roles);
}

/** Pour `canActivate` : sans le rôle, renvoie vers `repli` au lieu d'annuler la navigation. */
export function roleGuardAvecRepli(repli: string, ...roles: Role[]): CanActivateFn {
  return () => inject(Session).aLeRole(...roles) || inject(Router).parseUrl(repli);
}
