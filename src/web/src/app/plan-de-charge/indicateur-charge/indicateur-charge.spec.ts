import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Charge } from '../charge';
import { IndicateurCharge } from './indicateur-charge';

const normale: Charge = {
  debut: '2026-05-04',
  fin: '2026-05-10',
  capacite: 4,
  chargePlanifiee: 4,
  tauxDeCharge: 1,
  surcharge: false,
  intercontrat: false,
};
const surcharge: Charge = {
  ...normale,
  chargePlanifiee: 5.33,
  tauxDeCharge: 4 / 3,
  surcharge: true,
};

describe('IndicateurCharge', () => {
  let fixture: ComponentFixture<IndicateurCharge>;
  let ecran: HTMLElement;

  async function afficher(charge: Charge, confirmee = false): Promise<void> {
    fixture.componentRef.setInput('charge', charge);
    fixture.componentRef.setInput('confirmee', confirmee);
    await fixture.whenStable();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [IndicateurCharge] });
    fixture = TestBed.createComponent(IndicateurCharge);
    ecran = fixture.nativeElement;
  });

  it('affiche le taux et le libellé d’une charge normale, sans bouton', async () => {
    await afficher(normale);

    expect(ecran.querySelector('.taux')?.textContent).toContain('100');
    expect(ecran.textContent).toContain('Charge normale');
    expect(ecran.classList.contains('normale')).toBe(true);
    expect(ecran.querySelector('button')).toBeNull();
  });

  it('écrit « Surcharge » en toutes lettres, en plus de la couleur (RG-24)', async () => {
    await afficher(surcharge);

    expect(ecran.querySelector('.taux')?.textContent).toContain('133');
    expect(ecran.textContent).toContain('Surcharge');
    expect(ecran.classList.contains('surcharge')).toBe(true);
  });

  it('propose de confirmer une surcharge, et le dit au parent', async () => {
    const confirmer = vi.fn();
    fixture.componentInstance.confirmer.subscribe(confirmer);
    await afficher(surcharge);

    ecran.querySelector('button')!.click();

    expect(confirmer).toHaveBeenCalledTimes(1);
  });

  it('remplace le bouton par « confirmée » une fois la surcharge confirmée', async () => {
    await afficher(surcharge, true);

    expect(ecran.querySelector('button')).toBeNull();
    expect(ecran.textContent).toContain('confirmée');
  });

  it('écrit « Intercontrat » (RG-25) et « Indisponible » quand il n’y a pas de taux', async () => {
    await afficher({ ...normale, tauxDeCharge: 0.4, intercontrat: true });
    expect(ecran.textContent).toContain('Intercontrat');

    await afficher({ ...normale, capacite: 0, tauxDeCharge: null });
    expect(ecran.textContent).toContain('Indisponible');
    expect(ecran.querySelector('.taux')).toBeNull();
  });
});
