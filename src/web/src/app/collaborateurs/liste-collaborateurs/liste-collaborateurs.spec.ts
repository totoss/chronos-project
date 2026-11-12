import { Location } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { ListeCollaborateurs } from './liste-collaborateurs';

describe('ListeCollaborateurs', () => {
  let harness: RouterTestingHarness;
  let page: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes, withComponentInputBinding())],
    });
    harness = await RouterTestingHarness.create();
  });

  async function ouvrir(url: string): Promise<void> {
    await harness.navigateByUrl(url, ListeCollaborateurs);
    page = harness.routeNativeElement as HTMLElement;
  }

  async function saisir(texte: string): Promise<void> {
    const champ = page.querySelector('input')!;
    champ.value = texte;
    champ.dispatchEvent(new Event('input'));
    await harness.fixture.whenStable();
  }

  async function cliquer(libelle: string): Promise<void> {
    const bouton = Array.from(page.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(libelle),
    );
    bouton!.click();
    await harness.fixture.whenStable();
  }

  function url(): string {
    return TestBed.inject(Location).path();
  }

  it('affiche les collaborateurs actifs', async () => {
    await ouvrir('/collaborateurs');

    expect(page.querySelectorAll('app-carte-collaborateur').length).toBe(4);
    expect(page.textContent).toContain('4 collaborateurs sur 5');
  });

  it('écrit la recherche dans l’adresse, sans la dupliquer', async () => {
    await ouvrir('/collaborateurs');
    await saisir('mar');

    expect(url()).toBe('/collaborateurs?recherche=mar');
    expect(page.querySelectorAll('app-carte-collaborateur').length).toBe(2);

    await saisir('');
    expect(url()).toBe('/collaborateurs');
  });

  it('lit la recherche dans l’adresse, après un rechargement', async () => {
    await ouvrir('/collaborateurs?recherche=dub');

    expect(page.querySelector('input')!.value).toBe('dub');
    expect(page.querySelectorAll('app-carte-collaborateur').length).toBe(1);
  });

  it('écrit le statut dans l’adresse sans perdre la recherche, et inversement', async () => {
    await ouvrir('/collaborateurs');
    await saisir('petit');
    expect(page.querySelectorAll('app-carte-collaborateur').length).toBe(0);

    await cliquer('Afficher les collaborateurs sortis');
    expect(url()).toBe('/collaborateurs?recherche=petit&statut=tous');
    expect(page.querySelectorAll('app-carte-collaborateur').length).toBe(1);

    await saisir('');
    expect(url()).toBe('/collaborateurs?statut=tous');

    await cliquer('Masquer les collaborateurs sortis');
    expect(url()).toBe('/collaborateurs');
  });

  it('mène à la fiche et au formulaire par des liens', async () => {
    await ouvrir('/collaborateurs');

    const liens = Array.from(page.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(liens).toContain('/collaborateurs/1');
    expect(liens).toContain('/collaborateurs/nouveau');
  });
});
