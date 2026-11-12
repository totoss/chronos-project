import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Collaborateur } from '../collaborateur';
import { CarteCollaborateur } from './carte-collaborateur';

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

describe('CarteCollaborateur', () => {
  let component: CarteCollaborateur;
  let fixture: ComponentFixture<CarteCollaborateur>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CarteCollaborateur],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(CarteCollaborateur);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('collaborateur', camille);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
