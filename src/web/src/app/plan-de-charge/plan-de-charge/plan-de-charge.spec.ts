import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Charge, PlanDeChargeDto } from '../charge';
import { PlanDeCharge } from './plan-de-charge';

function charge(debut: string, fin: string, partielle: Partial<Charge> = {}): Charge {
  return {
    debut,
    fin,
    capacite: 4,
    chargePlanifiee: 4,
    tauxDeCharge: 1,
    surcharge: false,
    intercontrat: false,
    ...partielle,
  };
}

const plan: PlanDeChargeDto = {
  du: '2026-05-04',
  au: '2026-05-31',
  pas: 'semaine',
  lignes: [
    {
      collaborateurId: 2,
      nom: 'Durand',
      prenom: 'Camille',
      periodes: [
        charge('2026-05-04', '2026-05-10'),
        charge('2026-05-11', '2026-05-17', { tauxDeCharge: 4 / 3, surcharge: true }),
        charge('2026-05-18', '2026-05-24', { tauxDeCharge: 0.4, intercontrat: true }),
        charge('2026-05-25', '2026-05-31', { tauxDeCharge: 0.5 }),
      ],
    },
    {
      collaborateurId: 1,
      nom: 'Martin',
      prenom: 'Sophie',
      periodes: [
        charge('2026-05-04', '2026-05-10', { capacite: 0, tauxDeCharge: null }),
        charge('2026-05-11', '2026-05-17'),
        charge('2026-05-18', '2026-05-24'),
        charge('2026-05-25', '2026-05-31'),
      ],
    },
  ],
};

describe('PlanDeCharge', () => {
  let fixture: ComponentFixture<PlanDeCharge>;
  let http: HttpTestingController;
  let ecran: HTMLElement;

  async function ouvrir() {
    fixture = TestBed.createComponent(PlanDeCharge);
    ecran = fixture.nativeElement;
    TestBed.tick();
    const requete = http.expectOne((r) => r.url === '/api/plan-de-charge');
    requete.flush(plan);
    await fixture.whenStable();
    return requete.request.params;
  }

  function bouton(libelle: string, racine: ParentNode = ecran): HTMLButtonElement {
    return Array.from(racine.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(libelle),
    )!;
  }

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 4, 6, 10, 0));
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  it('demande quatre semaines à partir du lundi de la semaine en cours', async () => {
    const params = await ouvrir();

    expect(params.get('du')).toBe('2026-05-04');
    expect(params.get('au')).toBe('2026-05-31');
    expect(params.get('pas')).toBe('semaine');
  });

  it('affiche une ligne par collaborateur, une colonne par semaine', async () => {
    await ouvrir();

    expect(
      Array.from(ecran.querySelectorAll('tbody th')).map((th) => th.textContent?.trim()),
    ).toEqual(['Camille Durand', 'Sophie Martin']);
    expect(
      Array.from(ecran.querySelectorAll('thead th')).map((th) => th.textContent?.trim()),
    ).toEqual(['Collaborateur', 'lun. 4 mai', 'lun. 11 mai', 'lun. 18 mai', 'lun. 25 mai']);
  });

  it('nomme le tableau et dit quelle surcharge chaque bouton confirme', async () => {
    await ouvrir();

    expect(ecran.querySelector('caption')?.textContent).toContain('par semaine');
    expect(ecran.querySelector('tbody button')?.getAttribute('aria-label')).toBe(
      'Confirmer la surcharge de Camille Durand, semaine du lun. 11 mai',
    );
  });

  it('écrit un libellé pour chaque niveau de charge, jamais la couleur seule', async () => {
    await ouvrir();

    const texte = ecran.querySelector('tbody')!.textContent;
    expect(texte).toContain('Charge normale');
    expect(texte).toContain('Surcharge');
    expect(texte).toContain('Intercontrat');
    expect(texte).toContain('Sous-charge');
    expect(texte).toContain('Indisponible');
    expect(ecran.querySelectorAll('app-indicateur-charge.surcharge')).toHaveLength(1);
  });

  it('confirme une surcharge dans un dialogue, puis le dit dans la case (RG-24)', async () => {
    await ouvrir();

    bouton('Confirmer').click();
    await vi.waitFor(() =>
      expect(document.querySelector('app-confirmation-de-surcharge')).toBeTruthy(),
    );
    TestBed.tick();
    const dialogue = document.querySelector('app-confirmation-de-surcharge')!;
    expect(dialogue.textContent).toContain('Camille Durand');
    expect(dialogue.textContent).toContain('133\u00a0%');
    expect(dialogue.textContent).toContain('semaine du lun. 11 mai');

    bouton('Confirmer', dialogue).click();
    await vi.waitFor(() =>
      expect(document.querySelector('app-confirmation-de-surcharge')).toBeNull(),
    );
    await fixture.whenStable();

    expect(ecran.textContent).toContain('confirmée');
    expect(ecran.querySelector('tbody button')).toBeNull();
  });

  it('laisse la surcharge à confirmer quand on annule', async () => {
    await ouvrir();

    bouton('Confirmer').click();
    await vi.waitFor(() =>
      expect(document.querySelector('app-confirmation-de-surcharge')).toBeTruthy(),
    );
    bouton('Annuler', document.querySelector('app-confirmation-de-surcharge')!).click();
    await vi.waitFor(() =>
      expect(document.querySelector('app-confirmation-de-surcharge')).toBeNull(),
    );
    await fixture.whenStable();

    expect(ecran.textContent).not.toContain('confirmée');
    expect(bouton('Confirmer')).toBeTruthy();
  });

  it('change de semaine, et redemande le plan', async () => {
    await ouvrir();

    bouton('Semaine suivante').click();
    TestBed.tick();

    const requete = http.expectOne((r) => r.url === '/api/plan-de-charge');
    expect(requete.request.params.get('du')).toBe('2026-05-11');
    requete.flush({ ...plan, lignes: [] });
  });

  it('passe au découpage par mois : quatre mois, en-têtes par mois', async () => {
    await ouvrir();

    bouton('Par mois').click();
    TestBed.tick();

    const requete = http.expectOne((r) => r.url === '/api/plan-de-charge');
    expect(requete.request.params.get('pas')).toBe('mois');
    expect(requete.request.params.get('du')).toBe('2026-05-01');
    expect(requete.request.params.get('au')).toBe('2026-08-31');
    requete.flush({
      ...plan,
      pas: 'mois',
      lignes: [
        {
          collaborateurId: 2,
          nom: 'Durand',
          prenom: 'Camille',
          periodes: [charge('2026-05-04', '2026-05-31'), charge('2026-06-01', '2026-06-30')],
        },
      ],
    });
    await fixture.whenStable();

    expect(
      Array.from(ecran.querySelectorAll('thead th')).map((th) => th.textContent?.trim()),
    ).toEqual(['Collaborateur', 'mai 2026', 'juin 2026']);
  });
});
