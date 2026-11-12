import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ListeCollaborateurs } from './liste-collaborateurs';

describe('ListeCollaborateurs', () => {
  let component: ListeCollaborateurs;
  let fixture: ComponentFixture<ListeCollaborateurs>;
  let page: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListeCollaborateurs],
    }).compileComponents();

    fixture = TestBed.createComponent(ListeCollaborateurs);
    component = fixture.componentInstance;
    page = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  async function saisir(texte: string): Promise<void> {
    const champ = page.querySelector('input')!;
    champ.value = texte;
    champ.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  async function voirLaFiche(): Promise<void> {
    page.querySelector<HTMLButtonElement>('app-carte-collaborateur button')!.click();
    await fixture.whenStable();
  }

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('ouvre la fiche du collaborateur choisi', async () => {
    await saisir('camille');
    await voirLaFiche();

    const fiche = page.querySelector('app-fiche-collaborateur')!;
    expect(fiche.textContent).toContain('01/09/2021');
    expect(fiche.textContent).toContain('80%');
  });

  it('garde la fiche tant que le collaborateur reste dans la liste, puis la ferme', async () => {
    await saisir('camille');
    await voirLaFiche();

    await saisir('mar');
    expect(page.querySelector('app-fiche-collaborateur')).not.toBeNull();

    await saisir('dub');
    expect(page.querySelector('app-fiche-collaborateur')).toBeNull();
  });
});
