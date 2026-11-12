import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FicheCollaborateur } from './fiche-collaborateur';

describe('FicheCollaborateur', () => {
  let component: FicheCollaborateur;
  let fixture: ComponentFixture<FicheCollaborateur>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FicheCollaborateur],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(FicheCollaborateur);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('id', '1');
    await fixture.whenStable();
  });

  async function afficher(id: string): Promise<string> {
    fixture.componentRef.setInput('id', id);
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('affiche la date d’entrée et la quotité', () => {
    const texte = (fixture.nativeElement as HTMLElement).textContent;

    expect(texte).toContain('01/09/2021');
    expect(texte).toContain('80%');
  });

  it('annonce un identifiant inconnu', async () => {
    expect(await afficher('999')).toContain('Aucun collaborateur ne porte l\'identifiant « 999 »');
  });

  it('annonce un identifiant qui n’est pas un nombre', async () => {
    const texte = await afficher('abc');

    expect(texte).toContain('l\'identifiant doit être un nombre');
    expect(texte).not.toContain('NaN');
  });
});
