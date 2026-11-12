import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
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
