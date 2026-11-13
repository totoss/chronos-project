import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { erreursInterceptor } from '../../commun/erreurs-interceptor';
import { FormulaireAffectation } from './formulaire-affectation';

const collaborateurs = {
  elements: [{ id: 2, prenom: 'Camille', nom: 'Durand' }],
  page: 1,
  taille: 100,
  total: 1,
};
const projets = {
  elements: [{ id: 1, code: 'BDL-REFONTE', libelle: 'Refonte' }],
  page: 1,
  taille: 100,
  total: 1,
};

describe('FormulaireAffectation', () => {
  let fixture: ComponentFixture<FormulaireAffectation>;
  let http: HttpTestingController;
  let ecran: HTMLElement;
  const fermer = vi.fn();

  const surcharge = {
    detail: 'Cette affectation met Camille Durand en surcharge.',
    regle: 'RG-24',
  };

  function creations() {
    return http.match({ method: 'POST', url: '/api/affectations' });
  }

  function bouton(libelle: string, racine: ParentNode): HTMLButtonElement {
    return Array.from(racine.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(libelle),
    )!;
  }

  /** Remplit le formulaire par son modèle (les listes Material se testent avec les harnais, module 20). */
  function remplir(): void {
    (fixture.componentInstance as unknown as { modele: { set(valeur: unknown): void } }).modele.set(
      {
        collaborateurId: 2,
        projetId: 1,
        dateDebut: '2026-06-01',
        dateFin: '2026-06-30',
        taux: 30,
      },
    );
  }

  beforeEach(async () => {
    fermer.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([erreursInterceptor])),
        provideHttpClientTesting(),
        { provide: MatDialogRef, useValue: { close: fermer } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(FormulaireAffectation);
    ecran = fixture.nativeElement;
    TestBed.tick();
    http.expectOne((r) => r.url === '/api/collaborateurs').flush(collaborateurs);
    http.expectOne((r) => r.url === '/api/projets').flush(projets);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('crée l’affectation et se ferme, quand l’API l’accepte', async () => {
    remplir();
    bouton('Créer', ecran).click();

    const [requete] = await vi.waitFor(() => {
      const envoyees = creations();
      expect(envoyees).toHaveLength(1);
      return envoyees;
    });
    expect(requete.request.body).toEqual({
      collaborateurId: 2,
      projetId: 1,
      dateDebut: '2026-06-01',
      dateFin: '2026-06-30',
      tauxAllocation: 0.3,
      surchargeConfirmee: false,
    });
    requete.flush({ id: 3 });

    await vi.waitFor(() => expect(fermer).toHaveBeenCalledWith(true));
  });

  it('demande confirmation sur un 422 RG-24, puis renvoie la requête confirmée', async () => {
    remplir();
    bouton('Créer', ecran).click();
    const [premiere] = await vi.waitFor(() => {
      const envoyees = creations();
      expect(envoyees).toHaveLength(1);
      return envoyees;
    });
    premiere.flush(surcharge, { status: 422, statusText: 'Unprocessable Entity' });

    await vi.waitFor(() =>
      expect(document.querySelector('app-dialogue-de-confirmation')).toBeTruthy(),
    );
    TestBed.tick();
    const dialogue = document.querySelector('app-dialogue-de-confirmation')!;
    expect(dialogue.textContent).toContain('Camille Durand sera en surcharge');
    bouton('Confirmer', dialogue).click();

    const [seconde] = await vi.waitFor(() => {
      const envoyees = creations();
      expect(envoyees).toHaveLength(1);
      return envoyees;
    });
    expect(seconde.request.body.surchargeConfirmee).toBe(true);
    seconde.flush({ id: 3 });

    await vi.waitFor(() => expect(fermer).toHaveBeenCalledWith(true));
  });

  it('reste ouvert, sans renvoyer, quand le manager refuse la surcharge', async () => {
    remplir();
    bouton('Créer', ecran).click();
    const [premiere] = await vi.waitFor(() => {
      const envoyees = creations();
      expect(envoyees).toHaveLength(1);
      return envoyees;
    });
    premiere.flush(surcharge, { status: 422, statusText: 'Unprocessable Entity' });

    await vi.waitFor(() =>
      expect(document.querySelector('app-dialogue-de-confirmation')).toBeTruthy(),
    );
    bouton('Annuler', document.querySelector('app-dialogue-de-confirmation')!).click();
    await vi.waitFor(() =>
      expect(document.querySelector('app-dialogue-de-confirmation')).toBeNull(),
    );
    await fixture.whenStable();

    expect(creations()).toHaveLength(0);
    expect(fermer).not.toHaveBeenCalled();
  });

  it('affiche dans le formulaire un autre refus de l’API, sans demander de confirmation', async () => {
    remplir();
    bouton('Créer', ecran).click();
    const [requete] = await vi.waitFor(() => {
      const envoyees = creations();
      expect(envoyees).toHaveLength(1);
      return envoyees;
    });
    requete.flush(
      { detail: 'Le projet est terminé.', regle: 'RG-14' },
      { status: 422, statusText: 'Unprocessable Entity' },
    );
    await fixture.whenStable();

    expect(document.querySelector('app-dialogue-de-confirmation')).toBeNull();
    expect(ecran.querySelector('[role="alert"]')?.textContent).toContain('Le projet est terminé.');
    expect(fermer).not.toHaveBeenCalled();
  });

  it('n’envoie rien tant que le formulaire est incomplet', async () => {
    bouton('Créer', ecran).click();
    await fixture.whenStable();

    expect(creations()).toHaveLength(0);
    expect(ecran.textContent).toContain('Le collaborateur est obligatoire.');
  });
});
