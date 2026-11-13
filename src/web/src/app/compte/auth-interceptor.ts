import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { Session } from './session';

/** Un 401 en cours d'usage : la session a expiré, retour à la connexion. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(Session);
  const router = inject(Router);

  return next(req).pipe(
    tap({
      error: (erreur: unknown) => {
        const sessionPerdue =
          erreur instanceof HttpErrorResponse &&
          erreur.status === 401 &&
          !req.url.startsWith('/api/compte/');
        if (sessionPerdue) {
          session.oublier();
          void router.navigate(['/connexion'], { queryParams: { retour: router.url } });
        }
      },
    }),
  );
};
