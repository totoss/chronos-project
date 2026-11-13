import { HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';
import { versErreurApi } from './erreur-api';

/**
 * À mettre à `true` sur la requête d'un formulaire : il affiche lui-même les erreurs de saisie
 * (400, 409, 422), l'intercepteur ne les notifie donc pas.
 */
export const ERREURS_DE_SAISIE_GEREES = new HttpContextToken<boolean>(() => false);

export const erreursInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);

  return next(req).pipe(
    catchError((reponse: unknown) => {
      const erreur = versErreurApi(reponse);
      const geree = req.context.get(ERREURS_DE_SAISIE_GEREES) && erreur.estDeSaisie;
      // Les refus de connexion et de déconnexion sont signalés par l'écran de connexion et par Session.
      const duCompte = req.url.startsWith('/api/compte/');
      if (req.method !== 'GET' && !geree && !duCompte) {
        snackBar.open(erreur.message, 'Fermer', { duration: 8000 });
      }
      return throwError(() => erreur);
    }),
  );
};
