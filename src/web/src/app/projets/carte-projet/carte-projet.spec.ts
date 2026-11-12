import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Projet } from '../projet';
import { CarteProjet } from './carte-projet';

const refonte: Projet = {
  code: 'BDL-REFONTE',
  libelle: 'Refonte du portail client',
  nature: 'Facturable',
  statut: 'EnCours',
  client: 'Banque du Lac',
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
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Banque du Lac');
  });

  it('signale un projet interne', async () => {
    fixture.componentRef.setInput('projet', { ...refonte, nature: 'Interne', client: null });
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Projet interne');
  });
});
