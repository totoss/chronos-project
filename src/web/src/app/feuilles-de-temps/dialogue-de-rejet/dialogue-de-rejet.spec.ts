import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { DialogueDeRejet } from './dialogue-de-rejet';

describe('DialogueDeRejet', () => {
  let fixture: ComponentFixture<DialogueDeRejet>;
  let ecran: HTMLElement;
  const fermer = vi.fn();

  function ecrire(motif: string): void {
    const champ = ecran.querySelector('textarea')!;
    champ.value = motif;
    champ.dispatchEvent(new Event('input'));
  }

  beforeEach(async () => {
    fermer.mockClear();
    TestBed.configureTestingModule({
      imports: [DialogueDeRejet],
      providers: [{ provide: MatDialogRef, useValue: { close: fermer } }],
    });
    fixture = TestBed.createComponent(DialogueDeRejet);
    ecran = fixture.nativeElement;
    await fixture.whenStable();
  });

  it('rend le motif saisi, sans espaces autour', async () => {
    ecrire('  Il manque le projet Alpha.  ');
    ecran.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    expect(fermer).toHaveBeenCalledWith('Il manque le projet Alpha.');
  });

  it('refuse un motif de plus de 500 caractères, comme l’API', async () => {
    ecrire('x'.repeat(501));
    ecran.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    expect(fermer).not.toHaveBeenCalled();
    expect(ecran.textContent).toContain('Le motif ne dépasse pas 500 caractères.');

    ecrire('x'.repeat(500));
    ecran.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();
    expect(fermer).toHaveBeenCalledWith('x'.repeat(500));
  });

  it('refuse un motif vide, et le dit (RG-34)', async () => {
    ecrire('   ');
    ecran.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    expect(fermer).not.toHaveBeenCalled();
    expect(ecran.textContent).toContain('Le motif du rejet est obligatoire.');
  });
});
