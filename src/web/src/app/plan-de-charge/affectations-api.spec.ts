import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ERREURS_DE_SAISIE_GEREES } from '../commun/erreurs-interceptor';
import { AffectationsApi } from './affectations-api';

describe('AffectationsApi', () => {
  let service: AffectationsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AffectationsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('crée une affectation par POST, en signalant que le formulaire gère ses erreurs', () => {
    const requete = {
      collaborateurId: 2,
      projetId: 1,
      dateDebut: '2026-06-01',
      dateFin: '2026-06-30',
      tauxAllocation: 0.3,
      surchargeConfirmee: true,
    };
    service.creer(requete).subscribe();

    const appel = http.expectOne({ method: 'POST', url: '/api/affectations' });
    expect(appel.request.body).toEqual(requete);
    expect(appel.request.context.get(ERREURS_DE_SAISIE_GEREES)).toBe(true);
    appel.flush({});
  });
});
