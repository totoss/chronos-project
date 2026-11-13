import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';
import { Session } from './compte/session';
import { Role } from './compte/utilisateur';

describe('Routes de l’application', () => {
  const connecte = signal(true);
  const roles = signal<Role[]>([]);
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    connecte.set(true);
    roles.set(['Collaborateur', 'Manager', 'GestionnaireRh']);
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: Session,
          useValue: {
            estConnecte: connecte,
            aLeRole: (...attendus: Role[]) => roles().some((r) => attendus.includes(r)),
          },
        },
      ],
    });
    harness = await RouterTestingHarness.create();
  });

  async function ouvrir(url: string): Promise<{ page: HTMLElement; titre: string }> {
    await harness.navigateByUrl(url);
    return {
      page: harness.routeNativeElement as HTMLElement,
      titre: TestBed.inject(Title).getTitle(),
    };
  }

  it('redirige l’adresse vide vers les collaborateurs', async () => {
    const { titre } = await ouvrir('/');

    expect(TestBed.inject(Location).path()).toBe('/collaborateurs');
    expect(titre).toBe('Collaborateurs — Chronos');
  });

  it('affiche la fiche de Camille Durand à /collaborateurs/2', async () => {
    const { page, titre } = await ouvrir('/collaborateurs/2');
    TestBed.inject(HttpTestingController).expectOne('/api/collaborateurs/2').flush({
      id: 2,
      matricule: 'M0002',
      nom: 'Durand',
      prenom: 'Camille',
      poste: 'Développeuse',
    });
    await harness.fixture.whenStable();

    expect(page.textContent).toContain('Camille Durand');
    expect(titre).toBe('Fiche collaborateur — Chronos');
  });

  it('affiche le formulaire, et non une fiche, à /collaborateurs/nouveau', async () => {
    const { page, titre } = await ouvrir('/collaborateurs/nouveau');

    expect(page.textContent).toContain('Nouveau collaborateur');
    expect(titre).toBe('Nouveau collaborateur — Chronos');
  });

  it('affiche la page introuvable pour une adresse inconnue', async () => {
    const { page, titre } = await ouvrir('/nimporte/quoi');

    expect(page.textContent).toContain('Page introuvable');
    expect(titre).toBe('Page introuvable — Chronos');
  });

  it('renvoie un visiteur non connecté à la connexion, en retenant l’adresse demandée', async () => {
    connecte.set(false);

    const { page } = await ouvrir('/feuille-de-temps?semaine=2026-05-04');

    expect(TestBed.inject(Location).path()).toBe(
      '/connexion?retour=%2Ffeuille-de-temps%3Fsemaine%3D2026-05-04',
    );
    expect(page.textContent).toContain('Connexion');
  });

  it('renvoie aussi à la connexion pour une adresse inconnue quand on n’est pas connecté', async () => {
    connecte.set(false);

    await ouvrir('/nimporte/quoi');

    expect(TestBed.inject(Location).path()).toBe('/connexion?retour=%2Fnimporte%2Fquoi');
  });

  it.each(['/plan-de-charge', '/feuilles-a-valider'])(
    'affiche la page introuvable à %s quand on n’est ni manager ni gestionnaire RH',
    async (url) => {
      roles.set(['Collaborateur']);

      const { page } = await ouvrir(url);

      expect(page.textContent).toContain('Page introuvable');
    },
  );

  it('laisse un manager ouvrir le plan de charge', async () => {
    roles.set(['Collaborateur', 'Manager']);

    const { titre } = await ouvrir('/plan-de-charge');

    expect(titre).toBe('Plan de charge — Chronos');
  });

  it('renvoie à la liste un manager qui charge directement le formulaire de création d’un collaborateur', async () => {
    roles.set(['Collaborateur', 'Manager']);

    const { titre } = await ouvrir('/collaborateurs/nouveau');
    TestBed.inject(HttpTestingController)
      .expectOne((r) => r.url === '/api/collaborateurs')
      .flush({ elements: [], page: 1, taille: 20, total: 0 });
    await harness.fixture.whenStable();

    expect(TestBed.inject(Location).path()).toBe('/collaborateurs');
    expect(titre).toBe('Collaborateurs — Chronos');
  });

  it('affiche la page introuvable à /projets/nouveau quand on n’est ni manager ni gestionnaire RH', async () => {
    roles.set(['Collaborateur']);

    const { page } = await ouvrir('/projets/nouveau');

    expect(page.textContent).toContain('Page introuvable');
  });
});
