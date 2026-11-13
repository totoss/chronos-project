import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatFormFieldHarness } from '@angular/material/form-field/testing';
import { MatSelectHarness } from '@angular/material/select/testing';
import { provideRouter, Router } from '@angular/router';
import { erreursInterceptor } from '../../commun/erreurs-interceptor';
import { FormulaireProjet } from './formulaire-projet';

describe('FormulaireProjet', () => {
  let fixture: ComponentFixture<FormulaireProjet>;
  let http: HttpTestingController;
  let loader: HarnessLoader;
  let naviguer: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormulaireProjet],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([erreursInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    naviguer = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(FormulaireProjet);
    loader = TestbedHarnessEnvironment.loader(fixture);

    TestBed.tick();
    http.expectOne('/api/clients').flush([{ id: 1, nom: 'Banque du Littoral', projetsEnCours: 0 }]);
    http
      .expectOne((r) => r.url === '/api/collaborateurs' && r.params.get('taille') === '100')
      .flush({
        elements: [{ id: 1, prenom: 'Sophie', nom: 'Martin' }],
        page: 1,
        taille: 100,
        total: 1,
      });
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  const racine = () => fixture.nativeElement as HTMLElement;
  const texte = () => racine().textContent ?? '';

  /** Attend la fin visible de l'envoi : le bouton redevient actif au tour où les erreurs sont posées. */
  async function attendreLaFinDeLEnvoi(): Promise<void> {
    await vi.waitFor(() => {
      const bouton = Array.from(racine().querySelectorAll('button')).find((b) =>
        b.textContent?.includes('Créer'),
      );
      expect(bouton!.disabled).toBe(false);
    });
  }

  async function saisir(libelle: string, valeur: string): Promise<void> {
    const label = Array.from(racine().querySelectorAll('label')).find((l) =>
      l.textContent?.includes(libelle),
    );
    const entree = racine().querySelector<HTMLInputElement>(`#${label!.htmlFor}`)!;
    entree.value = valeur;
    entree.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  async function choisir(libelle: string, option: string): Promise<void> {
    const champ = await loader.getHarness(MatFormFieldHarness.with({ floatingLabelText: libelle }));
    const liste = (await champ.getControl(MatSelectHarness))!;
    await liste.open();
    await liste.clickOptions({ text: option });
  }

  function envoyer(): void {
    Array.from(racine().querySelectorAll('button'))
      .find((b) => b.textContent?.includes('Créer'))!
      .click();
    TestBed.tick();
  }

  async function remplirLesChampsCommuns(): Promise<void> {
    await saisir('Code', 'INT-FORMATION-2');
    await saisir('Libellé', 'Formation interne');
    await saisir('Date de début', '2026-06-01');
    await saisir('Budget', '12');
    await choisir('Responsable', 'Sophie Martin');
  }

  it('exige un client pour un projet facturable', async () => {
    await remplirLesChampsCommuns();

    envoyer();
    await fixture.whenStable();

    expect(texte()).toContain('Un projet facturable a obligatoirement un client.');
    http.expectNone('/api/projets');
  });

  it('applique RG-12 : une fin avant le début est refusée sans appeler l’API', async () => {
    await remplirLesChampsCommuns();
    await saisir('Date de fin', '2026-05-01');

    envoyer();
    await fixture.whenStable();

    expect(texte()).toContain('La date de fin ne peut pas précéder la date de début.');
    http.expectNone('/api/projets');
  });

  it('masque le client d’un projet interne et ne l’envoie pas, même choisi auparavant', async () => {
    await remplirLesChampsCommuns();
    await choisir('Client', 'Banque du Littoral');
    await choisir('Nature', 'Interne');
    expect(texte()).not.toContain('Client');

    envoyer();
    const requete = http.expectOne({ method: 'POST', url: '/api/projets' });

    expect(requete.request.body).toEqual({
      code: 'INT-FORMATION-2',
      libelle: 'Formation interne',
      nature: 'Interne',
      clientId: null,
      dateDebut: '2026-06-01',
      dateFin: null,
      budgetJours: 12,
      responsableId: 1,
    });
    requete.flush({}, { status: 201, statusText: 'Created' });
    await fixture.whenStable();
    expect(naviguer).toHaveBeenCalledWith(['/projets']);
  });

  it('envoie le client d’un projet facturable', async () => {
    await remplirLesChampsCommuns();
    await choisir('Client', 'Banque du Littoral');

    envoyer();
    const requete = http.expectOne({ method: 'POST', url: '/api/projets' });

    expect(requete.request.body.clientId).toBe(1);
    expect(requete.request.body.nature).toBe('Facturable');
    requete.flush({}, { status: 201, statusText: 'Created' });
    await fixture.whenStable();
  });

  it('affiche dans le formulaire le 409 d’un code déjà pris (RG-10)', async () => {
    await remplirLesChampsCommuns();
    await choisir('Nature', 'Interne');

    envoyer();
    http
      .expectOne({ method: 'POST', url: '/api/projets' })
      .flush(
        { title: 'Conflit', detail: 'Un projet porte déjà ce code (RG-10).' },
        { status: 409, statusText: 'Conflict' },
      );
    await attendreLaFinDeLEnvoi();

    expect(racine().querySelector('p[role="alert"]')?.textContent).toContain(
      'Un projet porte déjà ce code (RG-10).',
    );
  });

  it('affiche le 422 de RG-11 renvoyé par l’API', async () => {
    await remplirLesChampsCommuns();
    await choisir('Nature', 'Interne');

    envoyer();
    http
      .expectOne({ method: 'POST', url: '/api/projets' })
      .flush(
        { title: 'Règle', detail: 'Un projet interne n’a pas de client.', regle: 'RG-11' },
        { status: 422, statusText: 'Unprocessable Entity' },
      );
    await attendreLaFinDeLEnvoi();

    expect(texte()).toContain('Un projet interne n’a pas de client.');
  });
});
