import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Page } from '../../commun/page';
import { Projet } from '../projet';
import { ListeProjets } from './liste-projets';

const refonte: Projet = {
  id: 1,
  code: 'BDL-REFONTE',
  libelle: 'Refonte du portail client',
  nature: 'Facturable',
  clientId: 1,
  clientNom: 'Banque du Littoral',
  dateDebut: '2026-03-02',
  dateFin: '2026-12-18',
  budgetJours: 240,
  responsableId: 1,
  responsableNom: 'Sophie Martin',
  statut: 'EnPreparation',
  version: '622c338c-9138-4e6b-8c53-e66f45944cc5',
  joursValides: 0,
};

function page(elements: Projet[], total = elements.length): Page<Projet> {
  return { elements, page: 1, taille: 20, total };
}

describe('ListeProjets', () => {
  let component: ListeProjets;
  let fixture: ComponentFixture<ListeProjets>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListeProjets],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ListeProjets);
    component = fixture.componentInstance;
    TestBed.tick();
  });

  afterEach(() => http.verify());

  function requeteListe() {
    return http.expectOne((r) => r.url === '/api/projets');
  }

  it('should create', () => {
    requeteListe().flush(page([]));

    expect(component).toBeTruthy();
  });

  it('demande la première page, triée par code, sans filtre de statut', () => {
    const { params } = requeteListe().request;

    expect(params.get('page')).toBe('1');
    expect(params.get('taille')).toBe('20');
    expect(params.get('tri')).toBe('code');
    expect(params.has('statut')).toBe(false);
  });

  it('affiche les projets et le total renvoyés par l’API', async () => {
    requeteListe().flush(page([refonte], 12));
    await fixture.whenStable();

    const texte = (fixture.nativeElement as HTMLElement).textContent;
    expect(texte).toContain('BDL-REFONTE');
    expect(texte).toContain('1 projet sur 12');
  });

  it('filtre par statut côté API', async () => {
    requeteListe().flush(page([refonte]));
    await fixture.whenStable();

    const choix = (fixture.nativeElement as HTMLElement).querySelector('select')!;
    choix.value = 'EnCours';
    choix.dispatchEvent(new Event('change'));

    TestBed.tick();
    const requete = requeteListe();
    expect(requete.request.params.get('statut')).toBe('EnCours');
    requete.flush(page([]));
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Aucun projet ne correspond',
    );
  });
});
