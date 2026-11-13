import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { Session } from './compte/session';
import { Role, Utilisateur } from './compte/utilisateur';

const camille: Utilisateur = {
  collaborateurId: 2,
  nom: 'Durand',
  prenom: 'Camille',
  email: 'camille.durand@chronos.example',
  roles: ['Collaborateur'],
};

describe('App', () => {
  const utilisateur = signal<Utilisateur | null>(camille);
  const deconnecter = vi.fn<Session['deconnecter']>();

  beforeEach(async () => {
    utilisateur.set(camille);
    deconnecter.mockReset();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter(routes, withComponentInputBinding()),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: Session,
          useValue: {
            utilisateur,
            estConnecte: () => utilisateur() !== null,
            aLeRole: (...attendus: Role[]) =>
              (utilisateur()?.roles ?? []).some((r) => attendus.includes(r)),
            deconnecter,
          },
        },
      ],
    }).compileComponents();
  });

  function liens(fixture: { nativeElement: unknown }): (string | null)[] {
    return Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('mat-nav-list a'),
    ).map((a) => a.getAttribute('href'));
  }

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it("affiche le nom de l'application", async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Chronos');
  });

  it("marque le lien de l'écran courant avec aria-current", async () => {
    const fixture = TestBed.createComponent(App);
    await TestBed.inject(Router).navigateByUrl('/projets');
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .expectOne((r) => r.url === '/api/projets')
      .flush({ elements: [], page: 1, taille: 20, total: 0 });
    await fixture.whenStable();

    const courants = (fixture.nativeElement as HTMLElement).querySelectorAll(
      'a[aria-current="page"]',
    );

    expect(courants.length).toBe(1);
    expect(courants[0].textContent).toContain('Projets');
  });

  it('présente le menu comme une liste, pour que le lecteur d’écran annonce le nombre d’éléments', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const elements = (fixture.nativeElement as HTMLElement).querySelectorAll(
      'mat-nav-list ul > li > a',
    );
    expect(elements).toHaveLength(4);
  });

  describe('le focus', () => {
    function titreEnCours(): string | undefined {
      const actif = document.activeElement;
      return actif?.tagName === 'H2' ? actif.textContent?.trim() : undefined;
    }

    it('va sur le titre du nouvel écran après un changement d’écran', async () => {
      const fixture = TestBed.createComponent(App);
      const routeur = TestBed.inject(Router);
      await routeur.navigateByUrl('/absences'); // la première navigation : le navigateur s'en charge
      await fixture.whenStable();
      expect(titreEnCours()).toBeUndefined();

      await routeur.navigateByUrl('/une-adresse-inconnue');

      await vi.waitFor(() => expect(titreEnCours()).toBe('Page introuvable'));
    });

    it('reste où il est quand seul un paramètre de l’adresse change', async () => {
      const fixture = TestBed.createComponent(App);
      const routeur = TestBed.inject(Router);
      await routeur.navigateByUrl('/absences');
      await fixture.whenStable();
      const bouton = (fixture.nativeElement as HTMLElement).querySelector('header button')!;
      (bouton as HTMLElement).focus();

      await routeur.navigateByUrl('/absences?recherche=dur');
      await fixture.whenStable();

      expect(document.activeElement).toBe(bouton);
    });

    it('va sur le titre quand on suit le lien d’évitement, sans changer d’adresse', async () => {
      const fixture = TestBed.createComponent(App);
      const routeur = TestBed.inject(Router);
      await routeur.navigateByUrl('/absences');
      await fixture.whenStable();

      (fixture.nativeElement as HTMLElement)
        .querySelector<HTMLAnchorElement>('.evitement')!
        .click();

      expect(titreEnCours()).toBe('Absences');
      expect(routeur.url).toBe('/absences');
    });
  });

  it('ne propose à un collaborateur sans autre rôle ni le plan de charge ni les feuilles à valider', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    expect(
      (fixture.nativeElement as HTMLElement)
        .querySelector('mat-sidenav')
        ?.classList.contains('mat-drawer-opened'),
    ).toBe(true);
    expect(liens(fixture)).toEqual([
      '/collaborateurs',
      '/projets',
      '/feuille-de-temps',
      '/absences',
    ]);
  });

  it('propose un lien de menu par écran à un manager, puis le retire à la déconnexion', async () => {
    utilisateur.set({ ...camille, roles: ['Collaborateur', 'Manager'] });
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    expect(liens(fixture)).toEqual([
      '/collaborateurs',
      '/projets',
      '/plan-de-charge',
      '/feuille-de-temps',
      '/feuilles-a-valider',
      '/absences',
    ]);

    utilisateur.set(null);
    await fixture.whenStable();

    expect(liens(fixture)).toEqual([]);
  });

  it('ne propose aucune navigation à qui n’est pas connecté : ni menu, ni bouton du menu', async () => {
    utilisateur.set(null);
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const racine = fixture.nativeElement as HTMLElement;

    expect(racine.querySelector('mat-nav-list')).toBeNull();
    expect(racine.querySelector('header button')).toBeNull();
    expect(racine.querySelector('mat-sidenav')?.classList.contains('mat-drawer-opened')).toBe(
      false,
    );
  });

  it("affiche l'adresse d'un compte sans fiche, et se déconnecte puis revient à la connexion", async () => {
    utilisateur.set({
      collaborateurId: null,
      nom: null,
      prenom: null,
      email: 'admin@chronos.example',
      roles: ['Administrateur'],
    });
    deconnecter.mockResolvedValue();
    const fixture = TestBed.createComponent(App);
    const navigation = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    await fixture.whenStable();
    const racine = fixture.nativeElement as HTMLElement;

    expect(racine.querySelector('.utilisateur')?.textContent).toContain('admin@chronos.example');

    const bouton = Array.from(racine.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Se déconnecter'),
    );
    bouton!.click();

    await vi.waitFor(() => expect(navigation).toHaveBeenCalledWith('/connexion'));
    expect(deconnecter).toHaveBeenCalled();
  });
});
