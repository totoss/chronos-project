import {
  HttpClient,
  HttpInterceptorFn,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ErreurApi } from '../commun/erreur-api';
import { erreursInterceptor } from '../commun/erreurs-interceptor';
import { authInterceptor } from './auth-interceptor';
import { Session } from './session';

describe('authInterceptor, avec erreursInterceptor', () => {
  let http: HttpClient;
  let controleur: HttpTestingController;
  const oublier = vi.fn();
  const navigate = vi.fn();
  const open = vi.fn();

  /** Par défaut, les intercepteurs de `app.config.ts`, dans le même ordre. */
  function configurer(intercepteurs: HttpInterceptorFn[] = [erreursInterceptor, authInterceptor]) {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors(intercepteurs)),
        provideHttpClientTesting(),
        { provide: Session, useValue: { oublier } },
        { provide: Router, useValue: { navigate, url: '/feuille-de-temps?semaine=2026-05-04' } },
        { provide: MatSnackBar, useValue: { open } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controleur = TestBed.inject(HttpTestingController);
  }

  /** Envoie la requête, y répond par une erreur, et rend ce que l'appelant reçoit. */
  function recevoir(methode: 'GET' | 'POST', url: string, status: number): { erreur?: unknown } {
    const resultat: { erreur?: unknown } = {};
    http.request(methode, url).subscribe({ error: (e) => (resultat.erreur = e) });
    controleur.expectOne(url).flush({ title: 'Erreur' }, { status, statusText: 'Erreur' });
    return resultat;
  }

  beforeEach(() => {
    oublier.mockReset();
    navigate.mockReset();
    open.mockReset();
  });

  afterEach(() => controleur.verify());

  describe('dans l’ordre de app.config.ts', () => {
    beforeEach(() => configurer());

    it('sur un 401, oublie la session, renvoie à la connexion avec le retour, et relance une ErreurApi', () => {
      const { erreur } = recevoir('GET', '/api/feuilles-de-temps', 401);

      expect(oublier).toHaveBeenCalledOnce();
      expect(navigate).toHaveBeenCalledWith(['/connexion'], {
        queryParams: { retour: '/feuille-de-temps?semaine=2026-05-04' },
      });
      expect(erreur).toBeInstanceOf(ErreurApi);
      expect((erreur as ErreurApi).statut).toBe(401);
    });

    it('sur un 403, affiche le message en français et ne navigue pas', () => {
      const { erreur } = recevoir('POST', '/api/feuilles-de-temps/42/soumission', 403);

      expect(open).toHaveBeenCalledWith(
        "Vous n'avez pas le droit d'effectuer cette action.",
        'Fermer',
        { duration: 8000 },
      );
      expect(oublier).not.toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
      expect((erreur as ErreurApi).statut).toBe(403);
    });

    it('ne fait ni l’un ni l’autre pour une connexion refusée', () => {
      const { erreur } = recevoir('POST', '/api/compte/login', 401);

      expect(oublier).not.toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
      expect(open).not.toHaveBeenCalled();
      expect((erreur as ErreurApi).statut).toBe(401);
    });

    it('ne touche pas à la session sur une panne du serveur', () => {
      recevoir('GET', '/api/projets', 500);

      expect(oublier).not.toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
    });

    it('laisse passer une réponse réussie', () => {
      let reponse: unknown;
      http.get('/api/projets').subscribe((corps) => (reponse = corps));
      controleur.expectOne('/api/projets').flush({ total: 0 });

      expect(reponse).toEqual({ total: 0 });
      expect(oublier).not.toHaveBeenCalled();
    });
  });
});
