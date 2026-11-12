import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { versErreurApi } from './erreur-api';

export const erreursInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((reponse: unknown) => {
      const erreur = versErreurApi(reponse);
      if (req.method !== 'GET') {
        snackBar.open(erreur.message, 'Fermer', { duration: 8000 });
      }
      return throwError(() => erreur);
    }),
  );
};
