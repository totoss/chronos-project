import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FeuillesDeTempsApi } from './feuilles-de-temps-api';

describe('FeuillesDeTempsApi', () => {
  let service: FeuillesDeTempsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FeuillesDeTempsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('enregistre les saisies par PUT, dans un objet { saisies }', () => {
    const saisies = [{ projetId: 1, jour: '2026-05-04', duree: 0.5 }];
    service.enregistrer(42, saisies).subscribe();

    const appel = http.expectOne({ method: 'PUT', url: '/api/feuilles-de-temps/42/saisies' });
    expect(appel.request.body).toEqual({ saisies });
    appel.flush({});
  });

  it('soumet par POST, sans corps', () => {
    service.soumettre(42).subscribe();

    const appel = http.expectOne({ method: 'POST', url: '/api/feuilles-de-temps/42/soumission' });
    expect(appel.request.body).toBeNull();
    appel.flush({});
  });

  it('valide par POST, sans corps : le valideur est l’utilisateur connecté', () => {
    service.valider(42).subscribe();

    const appel = http.expectOne({ method: 'POST', url: '/api/feuilles-de-temps/42/validation' });
    expect(appel.request.body).toBeNull();
    appel.flush({});
  });

  it('rejette par POST, avec le motif', () => {
    service.rejeter(42, 'Il manque un projet.').subscribe();

    const appel = http.expectOne({ method: 'POST', url: '/api/feuilles-de-temps/42/rejet' });
    expect(appel.request.body).toEqual({ motif: 'Il manque un projet.' });
    appel.flush({});
  });

  it('demande la feuille d’un collaborateur pour un lundi', () => {
    TestBed.runInInjectionContext(() => {
      const feuille = service.deLaSemaine(
        () => 2,
        () => '2026-05-04',
      );
      TestBed.tick();

      const appel = http.expectOne((r) => r.url === '/api/feuilles-de-temps');
      expect(appel.request.params.get('collaborateurId')).toBe('2');
      expect(appel.request.params.get('semaine')).toBe('2026-05-04');
      appel.flush({ id: 42, collaborateurId: 2, semaine: '2026-05-04', saisies: [] });
      expect(feuille).toBeTruthy();
    });
  });
});
