import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { erreursInterceptor } from '../../commun/erreurs-interceptor';
import { Collaborateur } from '../collaborateur';
import { FormulaireCollaborateur } from './formulaire-collaborateur';

const camille: Collaborateur = {
  id: 2,
  matricule: 'M0002',
  nom: 'Durand',
  prenom: 'Camille',
  email: 'camille.durand@chronos.example',
  categorie: 'Consultant',
  poste: 'Développeuse',
  managerId: 1,
  managerNom: 'Sophie Martin',
  dateEntree: '2025-09-01',
  dateSortie: null,
  quotite: 0.8,
  estSorti: false,
  version: '908c8844-d1fa-4bd9-a0c1-30c94062b887',
};

describe('FormulaireCollaborateur', () => {
  let fixture: ComponentFixture<FormulaireCollaborateur>;
  let http: HttpTestingController;
  let naviguer: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormulaireCollaborateur],
      providers: [
        provideHttpClient(withInterceptors([erreursInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    naviguer = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(FormulaireCollaborateur);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    http.verify();
  });

  const racine = () => fixture.nativeElement as HTMLElement;
  const texte = () => racine().textContent ?? '';

  /** Affiche le formulaire (en modification si `collaborateur` est donné) et sert la liste des managers. */
  async function afficher(collaborateur?: Collaborateur): Promise<void> {
    if (collaborateur) {
      fixture.componentRef.setInput('collaborateur', collaborateur);
    }
    TestBed.tick();
    http
      .expectOne((r) => r.url === '/api/collaborateurs' && r.params.get('taille') === '100')
      .flush({
        elements: [camille],
        page: 1,
        taille: 100,
        total: 1,
      });
    await fixture.whenStable();
  }

  /** Attend la fin visible de l'envoi : le bouton redevient actif au tour où les erreurs sont posées. */
  async function attendreLaFinDeLEnvoi(libelle: string): Promise<void> {
    await vi.waitFor(() => expect(bouton(libelle).disabled).toBe(false));
  }

  function champ(libelle: string): HTMLInputElement {
    const label = Array.from(racine().querySelectorAll('label')).find((l) =>
      l.textContent?.includes(libelle),
    );
    return racine().querySelector<HTMLInputElement>(`#${label!.htmlFor}`)!;
  }

  async function saisir(libelle: string, valeur: string): Promise<void> {
    const entree = champ(libelle);
    entree.value = valeur;
    entree.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  function bouton(libelle: string): HTMLButtonElement {
    return Array.from(racine().querySelectorAll('button')).find((b) =>
      b.textContent?.includes(libelle),
    )!;
  }

  async function remplirLaCreation(): Promise<void> {
    await saisir('Matricule', 'M0099');
    await saisir('Nom', 'Petit');
    await saisir('Prénom', 'Léa');
    await saisir('Adresse électronique', 'lea.petit@chronos.example');
    await saisir('Poste', 'Développeuse');
    await saisir("Date d'entrée", '2026-05-04');
    await saisir('Quotité', '80');
  }

  /** Clique sur l'envoi, attend que la requête parte, et la rend. */
  async function envoyer(methode: string, url: string): Promise<TestRequest> {
    bouton(methode === 'POST' ? 'Créer' : 'Enregistrer').click();
    TestBed.tick();
    return http.expectOne({ method: methode, url });
  }

  describe('en création', () => {
    it("n'affiche aucune erreur à l'ouverture", async () => {
      await afficher();

      expect(racine().querySelectorAll('mat-error').length).toBe(0);
    });

    it("affiche les erreurs et n'envoie rien quand le formulaire est invalide", async () => {
      await afficher();

      bouton('Créer').click();
      await fixture.whenStable();

      expect(texte()).toContain('Le matricule est obligatoire.');
      expect(texte()).toContain('Le nom est obligatoire.');
      expect(texte()).toContain("La date d'entrée est obligatoire.");
      http.expectNone('/api/collaborateurs');
    });

    it("applique RG-03 : une quotité de 5 % est refusée sans appeler l'API", async () => {
      await afficher();
      await remplirLaCreation();
      await saisir('Quotité', '5');

      bouton('Créer').click();
      await fixture.whenStable();

      expect(texte()).toContain('La quotité est au minimum de 10 %.');
      http.expectNone('/api/collaborateurs');
    });

    it('envoie la quotité en fraction, sans date de sortie, puis ouvre la fiche', async () => {
      await afficher();
      await remplirLaCreation();

      const requete = await envoyer('POST', '/api/collaborateurs');
      expect(requete.request.body).toEqual({
        matricule: 'M0099',
        nom: 'Petit',
        prenom: 'Léa',
        email: 'lea.petit@chronos.example',
        categorie: 'Consultant',
        poste: 'Développeuse',
        dateEntree: '2026-05-04',
        typeContrat: 'Cdi',
        quotite: 0.8,
        managerId: null,
      });
      requete.flush({ ...camille, id: 12 }, { status: 201, statusText: 'Created' });
      await fixture.whenStable();

      expect(naviguer).toHaveBeenCalledWith(['/collaborateurs', 12]);
    });

    it('affiche un 409 dans le formulaire, sans notification, et bloque le renvoi', async () => {
      await afficher();
      await remplirLaCreation();

      const requete = await envoyer('POST', '/api/collaborateurs');
      requete.flush(
        { title: 'Conflit', detail: 'Un collaborateur porte déjà ce matricule (RG-01).' },
        { status: 409, statusText: 'Conflict' },
      );
      await attendreLaFinDeLEnvoi('Créer');

      expect(texte()).toContain('Un collaborateur porte déjà ce matricule (RG-01).');
      expect(document.body.textContent).not.toContain('Fermer');

      bouton('Créer').click();
      await fixture.whenStable();
      http.expectNone('/api/collaborateurs');

      await saisir('Matricule', 'M0100');
      expect(texte()).not.toContain('Un collaborateur porte déjà');
    });

    it('rattache les erreurs 400 à leur champ', async () => {
      await afficher();
      await remplirLaCreation();

      const requete = await envoyer('POST', '/api/collaborateurs');
      requete.flush(
        { errors: { Email: ["L'adresse électronique n'est pas valide."] } },
        { status: 400, statusText: 'Bad Request' },
      );
      await attendreLaFinDeLEnvoi('Créer');

      const erreur = racine().querySelector('mat-error');
      expect(erreur?.textContent).toContain("L'adresse électronique n'est pas valide.");
      expect(erreur?.closest('mat-form-field')?.textContent).toContain('Adresse électronique');
    });

    it('notifie une panne et permet de renvoyer la requête', async () => {
      await afficher();
      await remplirLaCreation();

      const requete = await envoyer('POST', '/api/collaborateurs');
      requete.flush({ title: 'Internal Server Error' }, { status: 500, statusText: 'Erreur' });
      await attendreLaFinDeLEnvoi('Créer');

      expect(racine().querySelector('[role="alert"]')).toBeNull();

      await envoyer('POST', '/api/collaborateurs');
    });

    it('ne propose ni date de sortie en création, mais le type de contrat', async () => {
      await afficher();

      expect(texte()).not.toContain('Date de sortie');
      expect(texte()).toContain('Type de contrat');
    });
  });

  describe('en modification', () => {
    it("préremplit la fiche et grise ce que l'API ne modifie pas", async () => {
      await afficher(camille);

      expect(champ('Nom').value).toBe('Durand');
      expect(champ('Quotité').value).toBe('80');
      expect(champ('Matricule').disabled).toBe(true);
      expect(champ("Date d'entrée").disabled).toBe(true);
      expect(champ('Quotité').disabled).toBe(true);
      expect(texte()).toContain('Date de sortie');
      expect(texte()).not.toContain('Type de contrat');
    });

    it('ne propose pas le collaborateur comme son propre manager', async () => {
      await afficher(camille);

      const options = fixture.componentInstance['candidatsManager']();
      expect(options.map((m) => m.id)).not.toContain(camille.id);
    });

    it('laisse la quotité vide quand l’API ne l’envoie pas (compte non RH)', async () => {
      const { quotite, ...sansQuotite } = camille;
      await afficher(sansQuotite);

      expect(champ('Quotité').value).toBe('');
    });

    it('envoie la version et la date de sortie, et rend la fiche enregistrée', async () => {
      const enregistre = vi.fn();
      fixture.componentInstance.enregistre.subscribe(enregistre);
      await afficher(camille);
      await saisir('Poste', 'Architecte');
      await saisir('Date de sortie', '2026-12-31');

      const requete = await envoyer('PUT', '/api/collaborateurs/2');
      expect(requete.request.body).toEqual({
        nom: 'Durand',
        prenom: 'Camille',
        email: 'camille.durand@chronos.example',
        poste: 'Architecte',
        dateSortie: '2026-12-31',
        managerId: 1,
        version: camille.version,
      });
      const modifiee = { ...camille, poste: 'Architecte', version: 'nouvelle' };
      requete.flush(modifiee);
      await fixture.whenStable();

      expect(enregistre).toHaveBeenCalledWith(modifiee);
    });

    it('envoie une date de sortie vide comme null', async () => {
      await afficher(camille);
      await saisir('Poste', 'Architecte');

      const requete = await envoyer('PUT', '/api/collaborateurs/2');

      expect(requete.request.body.dateSortie).toBeNull();
      requete.flush(camille);
      await fixture.whenStable();
    });

    it("applique RG-02 : une sortie avant l'entrée est refusée sans appeler l'API", async () => {
      await afficher(camille);
      await saisir('Date de sortie', '2025-08-31');

      bouton('Enregistrer').click();
      await fixture.whenStable();

      expect(texte()).toContain("La date de sortie ne peut pas précéder la date d'entrée.");
      http.expectNone('/api/collaborateurs/2');
    });

    it('propose de recharger la fiche après un conflit de version', async () => {
      const recharger = vi.fn();
      fixture.componentInstance.recharger.subscribe(recharger);
      await afficher(camille);
      await saisir('Poste', 'Architecte');

      const requete = await envoyer('PUT', '/api/collaborateurs/2');
      requete.flush(
        {
          title: 'Conflit',
          detail: "La ressource a été modifiée ou supprimée depuis que vous l'avez lue.",
          code: 'concurrence',
        },
        { status: 409, statusText: 'Conflict' },
      );
      await attendreLaFinDeLEnvoi('Enregistrer');

      expect(texte()).toContain('La ressource a été modifiée');
      bouton('Recharger la fiche').click();
      expect(recharger).toHaveBeenCalled();
    });

    it('ne propose pas de recharger la fiche pour un doublon (RG-01)', async () => {
      await afficher(camille);
      await saisir('Poste', 'Architecte');

      const requete = await envoyer('PUT', '/api/collaborateurs/2');
      requete.flush(
        {
          title: 'Conflit',
          detail: 'Un collaborateur porte déjà cette adresse (RG-01).',
          regle: 'RG-01',
        },
        { status: 409, statusText: 'Conflict' },
      );
      await attendreLaFinDeLEnvoi('Enregistrer');

      expect(texte()).toContain('Un collaborateur porte déjà cette adresse (RG-01).');
      expect(texte()).not.toContain('Recharger la fiche');
    });

    it('affiche le refus de RG-04 dans le formulaire', async () => {
      await afficher(camille);
      await saisir('Poste', 'Architecte');

      const requete = await envoyer('PUT', '/api/collaborateurs/2');
      requete.flush(
        {
          title: 'Règle',
          detail: 'Un collaborateur ne peut pas être son propre manager.',
          regle: 'RG-04',
        },
        { status: 422, statusText: 'Unprocessable Entity' },
      );
      await attendreLaFinDeLEnvoi('Enregistrer');

      expect(racine().querySelector('p[role="alert"]')?.textContent).toContain(
        'Un collaborateur ne peut pas être son propre manager.',
      );
    });
  });

  describe('annulation', () => {
    it("revient à la liste sans confirmation quand rien n'a changé", async () => {
      await afficher();

      bouton('Annuler').click();
      await fixture.whenStable();

      expect(naviguer).toHaveBeenCalledWith(['/collaborateurs']);
    });

    it('demande confirmation quand la saisie a changé, et reste si on refuse', async () => {
      const confirmer = vi.spyOn(window, 'confirm').mockReturnValue(false);
      const annule = vi.fn();
      fixture.componentInstance.annule.subscribe(annule);
      await afficher(camille);
      await saisir('Poste', 'Architecte');

      bouton('Annuler').click();
      await fixture.whenStable();
      expect(confirmer).toHaveBeenCalled();
      expect(annule).not.toHaveBeenCalled();

      confirmer.mockReturnValue(true);
      bouton('Annuler').click();
      await fixture.whenStable();
      expect(annule).toHaveBeenCalled();
    });
  });
});
