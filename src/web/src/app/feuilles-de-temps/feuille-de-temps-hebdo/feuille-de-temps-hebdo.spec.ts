import { LiveAnnouncer } from '@angular/cdk/a11y';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { erreursInterceptor } from '../../commun/erreurs-interceptor';
import { Session } from '../../compte/session';
import { Absence, Affectation, FeuilleDeTemps } from '../feuille-de-temps';
import { FeuilleDeTempsHebdo } from './feuille-de-temps-hebdo';

function feuille(partielle: Partial<FeuilleDeTemps> = {}): FeuilleDeTemps {
  return {
    id: 42,
    collaborateurId: 2,
    semaine: '2026-05-04',
    statut: 'Brouillon',
    motifRejet: null,
    saisies: [],
    ...partielle,
  };
}

const affectations: Affectation[] = [
  {
    id: 1,
    collaborateurId: 2,
    projetId: 1,
    dateDebut: '2026-04-20',
    dateFin: '2026-12-18',
    tauxAllocation: 0.6,
  },
  {
    id: 2,
    collaborateurId: 2,
    projetId: 2,
    dateDebut: '2026-04-27',
    dateFin: '2026-12-31',
    tauxAllocation: 0.4,
  },
  // Terminée avant la semaine affichée : pas de ligne (RG-33).
  {
    id: 3,
    collaborateurId: 2,
    projetId: 3,
    dateDebut: '2026-01-05',
    dateFin: '2026-03-31',
    tauxAllocation: 0.2,
  },
];

const projets = {
  elements: [
    { id: 1, libelle: 'Refonte du portail client' },
    { id: 2, libelle: 'Formation interne' },
    { id: 3, libelle: 'Ancien projet' },
  ],
  page: 1,
  taille: 100,
  total: 3,
};

describe('FeuilleDeTempsHebdo', () => {
  /** La fiche de l’utilisateur connecté : 2 est Camille ; `null` serait un compte sans fiche. */
  const collaborateurId = signal<number | null>(2);
  const announce = vi.fn<LiveAnnouncer['announce']>();
  let fixture: ComponentFixture<FeuilleDeTempsHebdo>;
  let http: HttpTestingController;
  let ecran: HTMLElement;

  /** Crée l'écran, répond aux cinq lectures, et rend la requête de la feuille. */
  async function ouvrir(reponses: { feuille?: FeuilleDeTemps; absences?: Absence[] } = {}) {
    fixture = TestBed.createComponent(FeuilleDeTempsHebdo);
    ecran = fixture.nativeElement;
    TestBed.tick();

    const requeteFeuille = http.expectOne((r) => r.url === '/api/feuilles-de-temps');
    requeteFeuille.flush(reponses.feuille ?? feuille());
    http.expectOne((r) => r.url === '/api/affectations').flush(affectations);
    http.expectOne((r) => r.url === '/api/absences').flush(reponses.absences ?? []);
    http.expectOne((r) => r.url === '/api/projets').flush(projets);
    http.expectOne('/api/jours-feries').flush([{ date: '2026-05-08', libelle: 'Victoire 1945' }]);
    await fixture.whenStable();
    return requeteFeuille.request;
  }

  function liste(libelle: string): HTMLSelectElement {
    return ecran.querySelector<HTMLSelectElement>(`select[aria-label="${libelle}"]`)!;
  }

  function choisir(libelle: string, valeur: string): void {
    const choix = liste(libelle);
    choix.value = valeur;
    choix.dispatchEvent(new Event('change'));
  }

  function bouton(libelle: string): HTMLButtonElement | undefined {
    return Array.from(ecran.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(libelle),
    );
  }

  beforeEach(() => {
    // Seule la date est figée : le reste du temps (promesses, minuteries) continue de s'écouler.
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 4, 6, 10, 0));
    collaborateurId.set(2);
    announce.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([erreursInterceptor])),
        provideHttpClientTesting(),
        { provide: Session, useValue: { collaborateurId } },
        { provide: LiveAnnouncer, useValue: { announce } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  it('demande la feuille de la semaine en cours pour le collaborateur courant', async () => {
    const requete = await ouvrir();

    expect(requete.params.get('collaborateurId')).toBe('2');
    expect(requete.params.get('semaine')).toBe('2026-05-04');
    expect(ecran.textContent).toContain('Semaine du lun. 4 mai');
  });

  it('demande la feuille du collaborateur de la session, pas d’un identifiant écrit en dur', async () => {
    collaborateurId.set(3);
    const requete = await ouvrir();

    expect(requete.params.get('collaborateurId')).toBe('3');
  });

  it('n’envoie aucune requête de feuille pour un compte sans fiche, et le dit', async () => {
    collaborateurId.set(null);
    fixture = TestBed.createComponent(FeuilleDeTempsHebdo);
    ecran = fixture.nativeElement;
    TestBed.tick();

    http.expectOne((r) => r.url === '/api/projets').flush(projets);
    http.expectOne('/api/jours-feries').flush([]);
    await fixture.whenStable();

    http.expectNone((r) => /feuilles-de-temps|affectations|absences/.test(r.url));
    expect(ecran.textContent).toContain('aucun collaborateur');
  });

  it('affiche une ligne par affectation de la semaine, et pas les autres (RG-33)', async () => {
    await ouvrir();

    const lignes = Array.from(ecran.querySelectorAll('tbody th')).map((th) => th.textContent);
    expect(lignes).toEqual(['Refonte du portail client', 'Formation interne']);
  });

  it('ferme le week-end et le jour férié (RG-32)', async () => {
    await ouvrir();

    expect(liste('Formation interne, sam. 9 mai')).toBeNull();
    expect(liste('Formation interne, ven. 8 mai')).toBeNull();
    expect(liste('Formation interne, jeu. 7 mai')).not.toBeNull();
  });

  it('ferme les jours d’une absence approuvée, et seulement ceux-là (RG-32)', async () => {
    const absence = (id: number, statut: Absence['statut'], jour: string): Absence => ({
      id,
      collaborateurId: 2,
      typeAbsenceId: 1,
      dateDebut: jour,
      dateFin: jour,
      debuteApresMidi: false,
      finitLeMatin: false,
      statut,
    });
    await ouvrir({
      absences: [absence(1, 'Approuvee', '2026-05-05'), absence(2, 'Demandee', '2026-05-06')],
    });

    expect(liste('Formation interne, mar. 5 mai')).toBeNull();
    expect(liste('Formation interne, mer. 6 mai')).not.toBeNull();
  });

  it('affiche l’état de la feuille et le motif d’un rejet (RG-34)', async () => {
    await ouvrir({ feuille: feuille({ motifRejet: 'Il manque le projet Alpha.' }) });

    expect(ecran.textContent).toContain('État : Brouillon');
    expect(ecran.querySelector('[role="status"]')?.textContent).toContain(
      'Feuille rejetée : Il manque le projet Alpha.',
    );
  });

  it('signale un total supérieur à 1 et bloque l’enregistrement (RG-31)', async () => {
    await ouvrir();

    choisir('Refonte du portail client, lun. 4 mai', '0.75');
    choisir('Formation interne, lun. 4 mai', '0.5');
    await fixture.whenStable();

    expect(ecran.querySelector('[role="alert"]')?.textContent?.replace(/\s+/g, ' ').trim()).toBe(
      'Le total dépasse une journée : lun. 4 mai.',
    );
    expect(bouton('Enregistrer le brouillon')!.getAttribute('aria-disabled')).toBe('true');
    expect(bouton('Soumettre')!.getAttribute('aria-disabled')).toBe('true');
  });

  describe('un jour d’absence l’après-midi (RG-32)', () => {
    const demiAbsence: Absence = {
      id: 1,
      collaborateurId: 2,
      typeAbsenceId: 1,
      dateDebut: '2026-05-06',
      dateFin: '2026-05-06',
      debuteApresMidi: true,
      finitLeMatin: false,
      statut: 'Approuvee',
    };

    it('accepte 0,5 au total', async () => {
      await ouvrir({ absences: [demiAbsence] });

      choisir('Refonte du portail client, mer. 6 mai', '0.25');
      choisir('Formation interne, mer. 6 mai', '0.25');
      await fixture.whenStable();

      expect(ecran.querySelector('[role="alert"]')).toBeNull();
      expect(bouton('Enregistrer le brouillon')!.getAttribute('aria-disabled')).not.toBe('true');
    });

    it('bloque l’enregistrement au-delà de 0,5 au total', async () => {
      await ouvrir({ absences: [demiAbsence] });

      choisir('Refonte du portail client, mer. 6 mai', '0.5');
      choisir('Formation interne, mer. 6 mai', '0.25');
      await fixture.whenStable();

      expect(ecran.querySelector('[role="alert"]')).toBeTruthy();
      expect(ecran.querySelector('.alerte')?.textContent).toContain("Plus d'une demi-journée");
      expect(bouton('Enregistrer le brouillon')!.getAttribute('aria-disabled')).toBe('true');
      expect(bouton('Soumettre')!.getAttribute('aria-disabled')).toBe('true');
    });
  });

  it('enregistre le brouillon sans les cases vides, puis recharge la feuille', async () => {
    await ouvrir({
      feuille: feuille({ saisies: [{ projetId: 1, jour: '2026-05-05', duree: 0.25 }] }),
    });

    choisir('Refonte du portail client, lun. 4 mai', '0.5');
    choisir('Refonte du portail client, mar. 5 mai', '0');
    await fixture.whenStable();
    bouton('Enregistrer le brouillon')!.click();

    const put = http.expectOne({ method: 'PUT', url: '/api/feuilles-de-temps/42/saisies' });
    expect(put.request.body).toEqual({
      saisies: [{ projetId: 1, jour: '2026-05-04', duree: 0.5 }],
    });
    put.flush(feuille());
    await vi.waitFor(() =>
      http.expectOne((r) => r.url === '/api/feuilles-de-temps').flush(feuille()),
    );
    await fixture.whenStable();
    expect(bouton('Enregistrer le brouillon')!.getAttribute('aria-disabled')).not.toBe('true');
    expect(announce).toHaveBeenCalledWith('Brouillon enregistré.', 'polite');
  });

  it('soumet en enregistrant d’abord, puis passe en lecture seule (RG-35)', async () => {
    await ouvrir();

    choisir('Formation interne, jeu. 7 mai', '1');
    await fixture.whenStable();
    bouton('Soumettre')!.click();

    http.expectOne({ method: 'PUT', url: '/api/feuilles-de-temps/42/saisies' }).flush(feuille());
    const post = await vi.waitFor(() =>
      http.expectOne({ method: 'POST', url: '/api/feuilles-de-temps/42/soumission' }),
    );
    post.flush(feuille({ statut: 'Soumise' }));
    const rechargement = await vi.waitFor(() =>
      http.expectOne((r) => r.url === '/api/feuilles-de-temps'),
    );
    rechargement.flush(feuille({ statut: 'Soumise' }));
    await fixture.whenStable();

    expect(ecran.textContent).toContain('État : Soumise');
    expect(bouton('Soumettre')).toBeUndefined();
    expect(ecran.querySelectorAll('select')).toHaveLength(0);
    // Les boutons ont disparu avec le brouillon : l'annonce dit ce qui s'est passé, le focus ne s'est pas perdu.
    expect(announce).toHaveBeenCalledWith('Feuille de temps soumise.', 'polite');
    expect(document.activeElement).toBe(ecran.querySelector('h2'));
  });

  it('ne soumet pas quand l’enregistrement est refusé, et garde la saisie à l’écran', async () => {
    await ouvrir();

    choisir('Formation interne, jeu. 7 mai', '1');
    await fixture.whenStable();
    bouton('Soumettre')!.click();
    http
      .expectOne({ method: 'PUT', url: '/api/feuilles-de-temps/42/saisies' })
      .flush(
        { detail: 'Jour non travaillé.', regle: 'RG-32' },
        { status: 422, statusText: 'Unprocessable' },
      );
    await fixture.whenStable();

    http.expectNone({ method: 'POST', url: '/api/feuilles-de-temps/42/soumission' });
    expect(liste('Formation interne, jeu. 7 mai').value).toBe('1');
    expect(bouton('Soumettre')!.getAttribute('aria-disabled')).not.toBe('true');
  });

  it('change de semaine, et redemande la feuille', async () => {
    await ouvrir();

    bouton('Semaine suivante')!.click();
    TestBed.tick();

    const requete = http.expectOne((r) => r.url === '/api/feuilles-de-temps');
    expect(requete.request.params.get('semaine')).toBe('2026-05-11');
    requete.flush(feuille({ semaine: '2026-05-11' }));
    await fixture.whenStable();
    expect(ecran.textContent).toContain('Semaine du lun. 11 mai');
  });
});
