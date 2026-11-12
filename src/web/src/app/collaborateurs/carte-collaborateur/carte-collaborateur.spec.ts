import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Collaborateur } from '../collaborateur';
import { CarteCollaborateur } from './carte-collaborateur';

/** Un élément de la liste : l'API n'y met ni dates ni quotité (RG-52). */
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
  estSorti: false,
  version: '908c8844-d1fa-4bd9-a0c1-30c94062b887',
};

describe('CarteCollaborateur', () => {
  let component: CarteCollaborateur;
  let fixture: ComponentFixture<CarteCollaborateur>;

  function texte(): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

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

  it('n’affiche pas « entrée le » quand la date est absente', () => {
    expect(texte()).toContain('M0002');
    expect(texte()).not.toContain('entrée le');
  });

  it('signale par une mention textuelle un collaborateur qui a quitté la société', async () => {
    expect(texte()).not.toContain('A quitté la société');

    fixture.componentRef.setInput('collaborateur', { ...camille, estSorti: true });
    await fixture.whenStable();

    expect(texte()).toContain('A quitté la société');
  });

  it('émet « supprimer » au clic sur le bouton', () => {
    let emissions = 0;
    component.supprimer.subscribe(() => emissions++);

    const bouton = Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('button'),
    ).find((b) => b.textContent?.includes('Supprimer'));
    bouton!.click();

    expect(emissions).toBe(1);
  });
});
