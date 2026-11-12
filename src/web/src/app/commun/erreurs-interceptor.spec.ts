import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ErreurApi } from './erreur-api';
import { ERREURS_DE_SAISIE_GEREES, erreursInterceptor } from './erreurs-interceptor';

describe('erreursInterceptor', () => {
  let http: HttpClient;
  let controleur: HttpTestingController;
  let notifier: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([erreursInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    controleur = TestBed.inject(HttpTestingController);
    notifier = vi.spyOn(TestBed.inject(MatSnackBar), 'open');
  });

  afterEach(() => controleur.verify());

  /** Envoie la requête, répond par une erreur, et rend ce que l'appelant reçoit. */
  function recevoir(
    methode: 'GET' | 'DELETE' | 'POST',
    corps: object,
    status: number,
    deSaisieGeree = false,
  ): { erreur?: unknown } {
    const resultat: { erreur?: unknown } = {};
    const context = new HttpContext().set(ERREURS_DE_SAISIE_GEREES, deSaisieGeree);
    http.request(methode, '/api/x', { context }).subscribe({ error: (e) => (resultat.erreur = e) });
    controleur.expectOne('/api/x').flush(corps, { status, statusText: 'Erreur' });
    return resultat;
  }

  it('traduit un 422 en ErreurApi avec son détail et sa règle, et notifie une écriture', () => {
    const { erreur } = recevoir(
      'DELETE',
      { title: 'Unprocessable Entity', detail: 'Le projet est clos.', regle: 'RG-11' },
      422,
    );

    expect(erreur).toBeInstanceOf(ErreurApi);
    expect((erreur as ErreurApi).statut).toBe(422);
    expect((erreur as ErreurApi).regle).toBe('RG-11');
    expect(notifier).toHaveBeenCalledWith('Le projet est clos.', 'Fermer', { duration: 8000 });
  });

  it('remplace le titre anglais d’un 400 par un libellé et garde les messages par champ', () => {
    const { erreur } = recevoir(
      'DELETE',
      { title: 'One or more validation errors occurred.', errors: { Matricule: ['Requis.'] } },
      400,
    );

    expect((erreur as ErreurApi).message).toBe('Les données envoyées sont invalides.');
    expect((erreur as ErreurApi).champs).toEqual({ Matricule: ['Requis.'] });
  });

  it.each([400, 409, 422])(
    'ne notifie pas un %i quand la requête dit que le formulaire s’en charge',
    (status) => {
      const { erreur } = recevoir('POST', { detail: 'Refusé.' }, status, true);

      expect((erreur as ErreurApi).statut).toBe(status);
      expect(notifier).not.toHaveBeenCalled();
    },
  );

  it('notifie quand même une erreur 500 sur la requête d’un formulaire', () => {
    recevoir('POST', { title: 'Internal Server Error' }, 500, true);

    expect(notifier).toHaveBeenCalled();
  });

  it('garde le code d’un 409 de concurrence', () => {
    const { erreur } = recevoir('POST', { detail: 'Périmée.', code: 'concurrence' }, 409, true);

    expect((erreur as ErreurApi).code).toBe('concurrence');
  });

  it('relance l’erreur sans notifier quand c’est une lecture', () => {
    const { erreur } = recevoir('GET', { title: 'Not Found' }, 404);

    expect(erreur).toBeInstanceOf(ErreurApi);
    expect((erreur as ErreurApi).statut).toBe(404);
    expect(notifier).not.toHaveBeenCalled();
  });

  it('traduit une API injoignable en statut 0', () => {
    const resultat: { erreur?: unknown } = {};
    http.delete('/api/x').subscribe({ error: (e) => (resultat.erreur = e) });
    controleur.expectOne('/api/x').error(new ProgressEvent('error'));

    expect((resultat.erreur as ErreurApi).statut).toBe(0);
    expect(notifier).toHaveBeenCalled();
  });
});
