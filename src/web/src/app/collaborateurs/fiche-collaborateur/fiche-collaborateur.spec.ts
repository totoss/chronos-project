import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Collaborateur } from '../collaborateur';
import { FicheCollaborateur } from './fiche-collaborateur';

const camille: Collaborateur = {
  id: 1,
  matricule: 'C0001',
  nom: 'Martin',
  prenom: 'Camille',
  email: 'camille.martin@chronos.example',
  categorie: 'Consultant',
  poste: 'Consultante confirmée',
  dateEntree: '2021-09-01',
  dateSortie: null,
  quotite: 0.8,
};

describe('FicheCollaborateur', () => {
  let component: FicheCollaborateur;
  let fixture: ComponentFixture<FicheCollaborateur>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FicheCollaborateur],
    }).compileComponents();

    fixture = TestBed.createComponent(FicheCollaborateur);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('collaborateur', camille);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('affiche la date d’entrée et la quotité', () => {
    const texte = (fixture.nativeElement as HTMLElement).textContent;

    expect(texte).toContain('01/09/2021');
    expect(texte).toContain('80%');
  });
});
