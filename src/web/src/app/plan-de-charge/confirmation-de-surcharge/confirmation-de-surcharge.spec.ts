import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ConfirmationDeSurcharge, DonneesDeConfirmation } from './confirmation-de-surcharge';

describe('ConfirmationDeSurcharge', () => {
  it('nomme le collaborateur, le taux et la période', async () => {
    const donnees: DonneesDeConfirmation = {
      collaborateur: 'Camille Durand',
      periode: 'semaine du lun. 4 mai',
      tauxDeCharge: 4 / 3,
    };
    TestBed.configureTestingModule({
      imports: [ConfirmationDeSurcharge],
      providers: [{ provide: MAT_DIALOG_DATA, useValue: donnees }],
    });
    const fixture = TestBed.createComponent(ConfirmationDeSurcharge);
    await fixture.whenStable();

    const texte = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texte).toContain('Camille Durand');
    expect(texte).toContain('133');
    expect(texte).toContain('semaine du lun. 4 mai');
  });
});
