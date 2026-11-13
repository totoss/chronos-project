import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Session } from './session';
import { Utilisateur } from './utilisateur';

const camille: Utilisateur = {
  collaborateurId: 2,
  nom: 'Durand',
  prenom: 'Camille',
  email: 'camille.durand@chronos.example',
  roles: ['Collaborateur', 'Manager'],
};

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

  /** Charge la session de Camille, comme au démarrage de l'application. */
  async function connecterCamille(): Promise<void> {
    const chargement = service.charger();
    http.expectOne('/api/compte/moi').flush(camille);
    await chargement;
  }

  it('est déconnectée tant que rien n’a été chargé', () => {
    expect(service.estConnecte()).toBe(false);
    expect(service.utilisateur()).toBeNull();
    expect(service.roles()).toEqual([]);
    expect(service.collaborateurId()).toBeNull();
    expect(service.aLeRole('Collaborateur')).toBe(false);
  });

  it("charge l'utilisateur courant", async () => {
    await connecterCamille();

    expect(service.estConnecte()).toBe(true);
    expect(service.collaborateurId()).toBe(2);
    expect(service.aLeRole('Manager')).toBe(true);
    expect(service.aLeRole('GestionnaireRh', 'Administrateur')).toBe(false);
  });

  it('reste déconnectée quand l’API répond 401, sans lever d’erreur', async () => {
    const chargement = service.charger();
    http.expectOne('/api/compte/moi').flush(null, { status: 401, statusText: 'Unauthorized' });
    await chargement;

    expect(service.estConnecte()).toBe(false);
  });

  it('connaît un compte sans fiche de collaborateur', async () => {
    const chargement = service.charger();
    http.expectOne('/api/compte/moi').flush({
      collaborateurId: null,
      nom: null,
      prenom: null,
      email: 'admin@chronos.example',
      roles: ['Administrateur'],
    });
    await chargement;

    expect(service.estConnecte()).toBe(true);
    expect(service.collaborateurId()).toBeNull();
  });

  it('se connecte par cookie puis charge le profil', async () => {
    const connexion = service.connecter('camille.durand@chronos.example', 'secret');

    const login = http.expectOne((req) => req.url === '/api/compte/login');
    expect(login.request.method).toBe('POST');
    expect(login.request.params.get('useCookies')).toBe('true');
    expect(login.request.body).toEqual({
      email: 'camille.durand@chronos.example',
      password: 'secret',
    });
    login.flush(null);

    const profil = await vi.waitFor(() => http.expectOne('/api/compte/moi'));
    profil.flush(camille);
    await connexion;

    expect(service.estConnecte()).toBe(true);
  });

  it('laisse passer le refus d’une connexion, et reste déconnectée', async () => {
    const connexion = service.connecter('camille.durand@chronos.example', 'faux');
    const refus = expect(connexion).rejects.toMatchObject({ status: 401 });

    http
      .expectOne((req) => req.url === '/api/compte/login')
      .flush(null, { status: 401, statusText: 'Unauthorized' });
    await refus;

    http.expectNone('/api/compte/moi');
    expect(service.estConnecte()).toBe(false);
  });

  it.each([
    ['la session a déjà expiré (401)', { status: 401, statusText: 'Unauthorized' }],
    ['l’API est injoignable', { status: 0, statusText: 'Unknown Error' }],
  ])('se déconnecte quand même quand %s', async (_cas, reponse) => {
    await connecterCamille();
    expect(service.estConnecte()).toBe(true);

    const deconnexion = service.deconnecter();
    http.expectOne('/api/compte/logout').flush(null, reponse);
    await deconnexion;

    expect(service.estConnecte()).toBe(false);
  });

  it('oublie l’utilisateur quand l’API a répondu 401 ailleurs', async () => {
    await connecterCamille();

    service.oublier();

    expect(service.estConnecte()).toBe(false);
    expect(service.roles()).toEqual([]);
  });
});
