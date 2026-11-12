import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Session } from './session';

describe('Session', () => {
  let service: Session;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Session);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it.each([
    ['la session a déjà expiré (401)', { status: 401, statusText: 'Unauthorized' }],
    ['l’API est injoignable', { status: 0, statusText: 'Unknown Error' }],
  ])('se déconnecte quand même quand %s', async (_cas, reponse) => {
    const chargement = service.charger();
    http.expectOne('/api/compte/moi').flush({
      collaborateurId: 2,
      nom: 'Durand',
      prenom: 'Camille',
      email: 'camille.durand@chronos.example',
      roles: ['Collaborateur'],
    });
    await chargement;
    expect(service.estConnecte()).toBe(true);

    const deconnexion = service.deconnecter();
    http.expectOne('/api/compte/logout').flush(null, reponse);
    await deconnexion;

    expect(service.estConnecte()).toBe(false);
  });
});
