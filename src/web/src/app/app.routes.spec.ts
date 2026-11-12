import { Location } from '@angular/common';
import { Title } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';

describe('Routes de l’application', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes, withComponentInputBinding())],
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

  it('affiche la fiche de Camille Martin à /collaborateurs/1', async () => {
    const { page, titre } = await ouvrir('/collaborateurs/1');

    expect(page.textContent).toContain('Camille Martin');
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
});
