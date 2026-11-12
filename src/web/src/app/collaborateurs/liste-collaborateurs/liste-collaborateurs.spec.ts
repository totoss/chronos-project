import { Location } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { Page } from '../../commun/page';
import { erreursInterceptor } from '../../commun/erreurs-interceptor';
import { Collaborateur } from '../collaborateur';
import { ListeCollaborateurs } from './liste-collaborateurs';

function collaborateur(id: number, nom: string): Collaborateur {
  return {
    id,
    matricule: `M000${id}`,
    nom,
    prenom: 'Camille',
    email: `${nom.toLowerCase()}@chronos.example`,
    categorie: 'Consultant',
    poste: 'Développeuse',
    managerId: null,
    managerNom: null,
    estSorti: false,
    version: '908c8844-d1fa-4bd9-a0c1-30c94062b887',
  };
}

function page(elements: Collaborateur[], total = elements.length): Page<Collaborateur> {
  return { elements, page: 1, taille: 20, total };
}

describe('ListeCollaborateurs', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let ecran: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(withInterceptors([erreursInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => http.verify());

  /** La requête de liste en attente, ou un échec si elle n'est pas partie (ou partie deux fois). */
  function requeteListe(): TestRequest {
    return http.expectOne((r) => r.url === '/api/collaborateurs');
  }

  /** Ouvre l'écran, répond à la liste, et rend la requête pour qu'on vérifie ses paramètres. */
  async function ouvrir(url: string, reponse = page([collaborateur(2, 'Durand')])) {
    await harness.navigateByUrl(url, ListeCollaborateurs);
    ecran = harness.routeNativeElement as HTMLElement;

    const requete = requeteListe();
    requete.flush(reponse);
    await harness.fixture.whenStable();
    return requete.request.params;
  }

  /** Un clic peut envoyer une requête : on n'attend pas la stabilité, qui attendrait sa réponse. */
  function cliquer(libelle: string): void {
    const bouton = Array.from(ecran.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(libelle),
    );
    bouton!.click();
  }

  /** La requête de liste qu'une action sur un signal (tri, page, rechargement) vient d'envoyer. */
  function requeteApresAction(): TestRequest {
    TestBed.tick();
    return requeteListe();
  }

  /** Attend la requête de liste que provoque une navigation du routeur (filtre des sortis). */
  function prochaineRequete(): Promise<TestRequest> {
    return vi.waitFor(() => requeteListe());
  }

  function cartes(): number {
    return ecran.querySelectorAll('app-carte-collaborateur').length;
  }

  function url(): string {
    return TestBed.inject(Location).path();
  }

  it('demande la première page des collaborateurs actifs, triés par nom', async () => {
    const params = await ouvrir('/collaborateurs');

    expect(params.get('page')).toBe('1');
    expect(params.get('taille')).toBe('20');
    expect(params.get('statut')).toBe('actif');
    expect(params.get('tri')).toBe('nom');
    expect(cartes()).toBe(1);
    expect(ecran.textContent).toContain('1 collaborateur sur 1');
  });

  it('affiche le total renvoyé par l’API, pas le nombre de cartes', async () => {
    await ouvrir('/collaborateurs', page([collaborateur(2, 'Durand')], 45));

    expect(ecran.textContent).toContain('1 collaborateur sur 45');
  });

  it('lit la recherche dans l’adresse et l’envoie à l’API en une seule requête', async () => {
    const params = await ouvrir('/collaborateurs?recherche=dub');

    expect(params.get('recherche')).toBe('dub');
    expect(ecran.querySelector('input')!.value).toBe('dub');
  });

  it('écrit la recherche dans l’adresse après une pause, puis interroge l’API', async () => {
    await ouvrir('/collaborateurs');

    const champ = ecran.querySelector('input')!;
    champ.value = 'mar';
    champ.dispatchEvent(new Event('input'));
    await harness.fixture.whenStable();
    http.expectNone((r) => r.url === '/api/collaborateurs');
    expect(url()).toBe('/collaborateurs');

    const requete = await vi.waitFor(() => requeteListe(), { timeout: 1000 });

    expect(url()).toBe('/collaborateurs?recherche=mar');
    expect(requete.request.params.get('recherche')).toBe('mar');
    requete.flush(page([]));
    await harness.fixture.whenStable();
    expect(ecran.textContent).toContain('Aucun collaborateur ne correspond à « mar »');
  });

  it('envoie le statut dans l’adresse sans perdre la recherche, et n’envoie aucun filtre pour « tous »', async () => {
    await ouvrir('/collaborateurs?recherche=petit');

    cliquer('Afficher les collaborateurs sortis');
    const tous = await prochaineRequete();
    expect(url()).toBe('/collaborateurs?recherche=petit&statut=tous');
    expect(tous.request.params.get('recherche')).toBe('petit');
    expect(tous.request.params.has('statut')).toBe(false);
    tous.flush(page([collaborateur(5, 'Petit')]));
    await harness.fixture.whenStable();

    cliquer('Masquer les collaborateurs sortis');
    const actifs = await prochaineRequete();
    expect(url()).toBe('/collaborateurs?recherche=petit');
    expect(actifs.request.params.get('statut')).toBe('actif');
    actifs.flush(page([]));
  });

  it('confie le tri à l’API', async () => {
    await ouvrir('/collaborateurs');

    cliquer('Trier par matricule');

    const requete = requeteApresAction();
    expect(requete.request.params.get('tri')).toBe('matricule');
    requete.flush(page([]));
  });

  it('retourne à la première page quand le tri change', async () => {
    await ouvrir('/collaborateurs', page([collaborateur(2, 'Durand')], 45));

    ecran.querySelector<HTMLButtonElement>('button.mat-mdc-paginator-navigation-next')!.click();
    const pageDeux = requeteApresAction();
    expect(pageDeux.request.params.get('page')).toBe('2');
    pageDeux.flush(page([collaborateur(3, 'Martin')], 45));
    await harness.fixture.whenStable();

    cliquer('Trier par matricule');
    const requete = requeteApresAction();
    expect(requete.request.params.get('page')).toBe('1');
    requete.flush(page([]));
  });

  it('propose de réessayer quand la liste ne se charge pas', async () => {
    await harness.navigateByUrl('/collaborateurs', ListeCollaborateurs);
    ecran = harness.routeNativeElement as HTMLElement;
    requeteListe().flush({ title: 'Internal Server Error' }, { status: 500, statusText: 'Erreur' });
    await harness.fixture.whenStable();

    expect(ecran.textContent).toContain('La liste n\'a pas pu être chargée');

    cliquer('Réessayer');
    const requete = requeteApresAction();
    requete.flush(page([collaborateur(2, 'Durand')]));
    await harness.fixture.whenStable();

    expect(cartes()).toBe(1);
  });

  /** Le bouton du dialogue de confirmation ouvert par un clic sur « Supprimer ». */
  async function confirmerDansLeDialogue(libelle: string): Promise<void> {
    const bouton = await vi.waitFor(() => {
      const trouve = Array.from(
        document.querySelectorAll<HTMLButtonElement>('app-dialogue-de-confirmation button'),
      ).find((b) => b.textContent?.includes(libelle));
      expect(trouve).toBeTruthy();
      return trouve!;
    });
    bouton.click();
  }

  it('demande confirmation avant de supprimer, et ne supprime rien si on annule', async () => {
    await ouvrir('/collaborateurs');

    cliquer('Supprimer');
    await vi.waitFor(() =>
      expect(document.querySelector('mat-dialog-content')?.textContent).toContain(
        'Supprimer Camille Durand ?',
      ),
    );
    await confirmerDansLeDialogue('Annuler');
    await vi.waitFor(() => expect(document.querySelector('app-dialogue-de-confirmation')).toBeNull());
    await harness.fixture.whenStable();

    http.expectNone({ method: 'DELETE', url: '/api/collaborateurs/2' });
    expect(cartes()).toBe(1);
  });

  it('recharge la liste après une suppression réussie', async () => {
    await ouvrir('/collaborateurs');

    cliquer('Supprimer');
    await confirmerDansLeDialogue('Supprimer');
    const suppression = await vi.waitFor(() =>
      http.expectOne({ method: 'DELETE', url: '/api/collaborateurs/2' }),
    );
    suppression.flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    const requete = requeteApresAction();
    requete.flush(page([]));
    await harness.fixture.whenStable();
    expect(cartes()).toBe(0);
  });

  it('ne recharge pas la liste quand l’API refuse la suppression', async () => {
    await ouvrir('/collaborateurs');

    cliquer('Supprimer');
    await confirmerDansLeDialogue('Supprimer');
    const suppression = await vi.waitFor(() =>
      http.expectOne({ method: 'DELETE', url: '/api/collaborateurs/2' }),
    );
    suppression.flush({ title: 'Not Found' }, { status: 404, statusText: 'Not Found' });
    await harness.fixture.whenStable();

    http.expectNone((r) => r.url === '/api/collaborateurs');
    expect(cartes()).toBe(1);
  });

  it('mène à la fiche et au formulaire par des liens', async () => {
    await ouvrir('/collaborateurs');

    const liens = Array.from(ecran.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(liens).toContain('/collaborateurs/2');
    expect(liens).toContain('/collaborateurs/nouveau');
  });
});
