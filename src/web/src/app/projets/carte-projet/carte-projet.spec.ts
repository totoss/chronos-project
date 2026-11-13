import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Projet } from '../projet';
import { CarteProjet } from './carte-projet';

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
  statut: 'EnCours',
  version: '622c338c-9138-4e6b-8c53-e66f45944cc5',
  joursValides: 0,
};

describe('CarteProjet', () => {
  let component: CarteProjet;
  let fixture: ComponentFixture<CarteProjet>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CarteProjet],
    }).compileComponents();

    fixture = TestBed.createComponent(CarteProjet);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('projet', refonte);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('affiche le client d’un projet facturable', () => {
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Banque du Littoral');
  });

  it('signale un projet interne', async () => {
    fixture.componentRef.setInput('projet', {
      ...refonte,
      nature: 'Interne',
      clientId: null,
      clientNom: null,
    });
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Projet interne');
  });
});
