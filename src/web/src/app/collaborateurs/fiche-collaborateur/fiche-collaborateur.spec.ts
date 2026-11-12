import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { erreursInterceptor } from '../../commun/erreurs-interceptor';
import { Collaborateur } from '../collaborateur';
import { FicheCollaborateur } from './fiche-collaborateur';

/** La fiche complète, telle que la reçoit le gestionnaire RH (CollaborateurRhDto). */
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
  dateEntree: '2021-09-01',
  dateSortie: null,
  quotite: 0.8,
  estSorti: false,
  version: '908c8844-d1fa-4bd9-a0c1-30c94062b887',
};

describe('FicheCollaborateur', () => {
  let component: FicheCollaborateur;
  let fixture: ComponentFixture<FicheCollaborateur>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FicheCollaborateur],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([erreursInterceptor])),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(FicheCollaborateur);
    component = fixture.componentInstance;
  });

  afterEach(() => http.verify());

  function texte(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  /** Affiche la fiche `id` et répond à la requête qu'elle envoie. */
  async function afficher(id: string, reponse: object, status = 200): Promise<void> {
    fixture.componentRef.setInput('id', id);
    TestBed.tick();

    http.expectOne(`/api/collaborateurs/${id}`).flush(reponse, { status, statusText: 'Réponse' });
    await fixture.whenStable();
  }

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('affiche la date d’entrée et la quotité', async () => {
    await afficher('2', camille);

    expect(texte()).toContain('Camille Durand');
    expect(texte()).toContain('01/09/2021');
    expect(texte()).toContain('80%');
  });

  it('affiche la date de sortie quand l’API l’envoie', async () => {
    await afficher('2', { ...camille, dateSortie: '2026-03-31', estSorti: true });

    expect(texte()).toContain('Date de sortie');
    expect(texte()).toContain('31/03/2026');
  });

  it('n’affiche ni date ni quotité quand l’API ne les envoie pas', async () => {
    const { dateEntree, dateSortie, quotite, ...reduit } = camille;
    await afficher('2', reduit);

    expect(texte()).toContain('Camille Durand');
    expect(texte()).not.toContain('Date d\'entrée');
    expect(texte()).not.toContain('Quotité');
    expect(texte()).not.toContain('Date de sortie');
  });

  it('annonce un identifiant inconnu (404)', async () => {
    await afficher('999', { title: 'Not Found', status: 404 }, 404);

    expect(texte()).toContain('Aucun collaborateur ne porte l\'identifiant « 999 »');
  });

  it('propose de réessayer quand la lecture échoue', async () => {
    await afficher('2', { title: 'Internal Server Error', status: 500 }, 500);
    expect(texte()).toContain('La fiche n\'a pas pu être chargée');

    const bouton = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('button'),
    ).find((b) => b.textContent?.includes('Réessayer'));
    bouton!.click();
    TestBed.tick();
    const requete = http.expectOne('/api/collaborateurs/2');
    requete.flush(camille);
    await fixture.whenStable();

    expect(texte()).toContain('Camille Durand');
  });

  it('annonce un identifiant qui n’est pas un nombre, sans interroger l’API', async () => {
    fixture.componentRef.setInput('id', 'abc');
    await fixture.whenStable();

    expect(texte()).toContain('l\'identifiant doit être un nombre');
    expect(texte()).not.toContain('NaN');
    http.expectNone(() => true);
  });
});
