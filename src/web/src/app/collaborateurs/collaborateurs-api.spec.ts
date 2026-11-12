import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ERREURS_DE_SAISIE_GEREES } from '../commun/erreurs-interceptor';
import { CollaborateursApi } from './collaborateurs-api';

describe('CollaborateursApi', () => {
  let service: CollaborateursApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CollaborateursApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('crée un collaborateur par POST, en signalant que le formulaire gère ses erreurs', () => {
    const requete = {
      matricule: 'M0099',
      nom: 'Petit',
      prenom: 'Léa',
      email: 'lea.petit@chronos.example',
      categorie: 'Consultant' as const,
      poste: 'Développeuse',
      dateEntree: '2026-05-04',
      typeContrat: 'Cdi' as const,
      quotite: 0.8,
      managerId: null,
    };
    service.creer(requete).subscribe();

    const appel = http.expectOne({ method: 'POST', url: '/api/collaborateurs' });
    expect(appel.request.body).toEqual(requete);
    expect(appel.request.context.get(ERREURS_DE_SAISIE_GEREES)).toBe(true);
    appel.flush({});
  });

  it('modifie un collaborateur par PUT, en signalant que le formulaire gère ses erreurs', () => {
    const requete = {
      nom: 'Durand',
      prenom: 'Camille',
      email: 'camille.durand@chronos.example',
      poste: 'Architecte',
      dateSortie: null,
      managerId: 1,
      version: '908c8844-d1fa-4bd9-a0c1-30c94062b887',
    };
    service.modifier(2, requete).subscribe();

    const appel = http.expectOne({ method: 'PUT', url: '/api/collaborateurs/2' });
    expect(appel.request.body).toEqual(requete);
    expect(appel.request.context.get(ERREURS_DE_SAISIE_GEREES)).toBe(true);
    appel.flush({});
  });

  it('supprime un collaborateur par DELETE', () => {
    let termine = false;
    service.supprimer(7).subscribe(() => (termine = true));

    http.expectOne({ method: 'DELETE', url: '/api/collaborateurs/7' }).flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    expect(termine).toBe(true);
  });
});
