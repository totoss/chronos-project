import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CompteurDeResultats } from './compteur-de-resultats';

describe('CompteurDeResultats', () => {
  let fixture: ComponentFixture<CompteurDeResultats>;

  async function afficher(nombre: number, total: number, nom?: string): Promise<string> {
    fixture.componentRef.setInput('nombre', nombre);
    fixture.componentRef.setInput('total', total);
    if (nom !== undefined) {
      fixture.componentRef.setInput('nom', nom);
    }
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement).textContent?.trim() ?? '';
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompteurDeResultats],
    }).compileComponents();

    fixture = TestBed.createComponent(CompteurDeResultats);
  });

  it('accorde le texte au singulier et au pluriel', async () => {
    expect(await afficher(1, 5)).toBe('1 collaborateur sur 5');
    expect(await afficher(2, 5)).toBe('2 collaborateurs sur 5');
  });

  it('écrit « Aucun » quand il n’y a aucun résultat', async () => {
    expect(await afficher(0, 5)).toBe('Aucun collaborateur sur 5');
  });

  it('accepte un autre nom', async () => {
    expect(await afficher(3, 4, 'projet')).toBe('3 projets sur 4');
  });
});
