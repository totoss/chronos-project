import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { erreursInterceptor } from '../../commun/erreurs-interceptor';
import { FeuilleDeTemps } from '../feuille-de-temps';
import { FeuillesAValider } from './feuilles-a-valider';

const soumise: FeuilleDeTemps = {
  id: 42,
  collaborateurId: 2,
  semaine: '2026-05-04',
  statut: 'Soumise',
  motifRejet: null,
  saisies: [
    { projetId: 1, jour: '2026-05-04', duree: 0.5 },
    { projetId: 1, jour: '2026-05-05', duree: 1 },
  ],
};

describe('FeuillesAValider', () => {
  let fixture: ComponentFixture<FeuillesAValider>;
  let http: HttpTestingController;
  let ecran: HTMLElement;

  function bouton(libelle: string, racine: ParentNode = ecran): HTMLButtonElement {
    return Array.from(racine.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(libelle),
    )!;
  }

  async function ouvrir(soumises: FeuilleDeTemps[] = [soumise]) {
    fixture = TestBed.createComponent(FeuillesAValider);
    ecran = fixture.nativeElement;
    TestBed.tick();
    http.expectOne('/api/feuilles-de-temps/soumises').flush(soumises);
    http
      .expectOne((r) => r.url === '/api/collaborateurs')
      .flush({
        elements: [{ id: 2, prenom: 'Camille', nom: 'Durand' }],
        page: 1,
        taille: 100,
        total: 1,
      });
    http
      .expectOne((r) => r.url === '/api/projets')
      .flush({
        elements: [{ id: 1, libelle: 'Refonte du portail client' }],
        page: 1,
        taille: 100,
        total: 1,
      });
    http.expectOne('/api/jours-feries').flush([{ date: '2026-05-08', libelle: 'Victoire 1945' }]);
    await fixture.whenStable();
  }

  async function ouvrirLaFeuille(): Promise<void> {
    await ouvrir();
    bouton('Camille Durand').click();
    await fixture.whenStable();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([erreursInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('dit quand aucune feuille n’attend de validation', async () => {
    await ouvrir([]);

    expect(ecran.textContent).toContain("Aucune feuille de temps n'attend votre validation.");
  });

  it('liste les feuilles soumises avec le nom du collaborateur', async () => {
    await ouvrir();

    expect(bouton('Camille Durand').textContent).toContain('semaine du lun. 4 mai');
    expect(ecran.querySelector('app-grille-de-saisie')).toBeNull();
  });

  it('relit la feuille choisie dans la grille, en lecture seule', async () => {
    await ouvrirLaFeuille();

    const listes = Array.from(ecran.querySelectorAll('select'));
    expect(listes).toHaveLength(7);
    expect(listes.every((l) => l.disabled)).toBe(true);
    expect(
      ecran.querySelector<HTMLSelectElement>(
        'select[aria-label="Refonte du portail client, lun. 4 mai"]',
      )!.value,
    ).toBe('0.5');
    expect(ecran.textContent).toContain('État : Soumise');
  });

  it('valide la feuille sans corps, puis redemande les feuilles à valider', async () => {
    await ouvrirLaFeuille();

    bouton('Valider').click();

    const post = http.expectOne({ method: 'POST', url: '/api/feuilles-de-temps/42/validation' });
    expect(post.request.body).toBeNull();
    post.flush({ ...soumise, statut: 'Validee' });
    await vi.waitFor(() => http.expectOne('/api/feuilles-de-temps/soumises').flush([]));
    await fixture.whenStable();

    expect(ecran.querySelector('app-grille-de-saisie')).toBeNull();
    expect(ecran.textContent).toContain("Aucune feuille de temps n'attend votre validation.");
  });

  it('rejette la feuille avec le motif saisi dans le dialogue', async () => {
    await ouvrirLaFeuille();

    bouton('Rejeter').click();
    await vi.waitFor(() => expect(document.querySelector('app-dialogue-de-rejet')).toBeTruthy());
    TestBed.tick();
    const dialogue = document.querySelector('app-dialogue-de-rejet')!;
    const champ = dialogue.querySelector('textarea')!;
    champ.value = 'Il manque le projet Alpha.';
    champ.dispatchEvent(new Event('input'));
    bouton('Rejeter', dialogue.querySelector('mat-dialog-actions')!).click();

    const post = await vi.waitFor(() =>
      http.expectOne({ method: 'POST', url: '/api/feuilles-de-temps/42/rejet' }),
    );
    expect(post.request.body).toEqual({ motif: 'Il manque le projet Alpha.' });
    post.flush({ ...soumise, statut: 'Brouillon', motifRejet: 'Il manque le projet Alpha.' });
    await vi.waitFor(() => http.expectOne('/api/feuilles-de-temps/soumises').flush([]));
  });

  it('ne rejette rien quand on annule le dialogue', async () => {
    await ouvrirLaFeuille();

    bouton('Rejeter').click();
    await vi.waitFor(() => expect(document.querySelector('app-dialogue-de-rejet')).toBeTruthy());
    bouton('Annuler', document.querySelector('app-dialogue-de-rejet')!).click();
    await vi.waitFor(() => expect(document.querySelector('app-dialogue-de-rejet')).toBeNull());

    http.expectNone({ method: 'POST', url: '/api/feuilles-de-temps/42/rejet' });
  });
});
