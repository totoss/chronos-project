import { Charge, LIBELLES_NIVEAU, niveauDeCharge } from './charge';

const normale: Charge = {
  debut: '2026-05-11',
  fin: '2026-05-17',
  capacite: 4,
  chargePlanifiee: 4,
  tauxDeCharge: 1,
  surcharge: false,
  intercontrat: false,
};

describe('niveauDeCharge', () => {
  it('traduit un taux ordinaire en charge normale', () => {
    expect(niveauDeCharge(normale)).toBe('normale');
  });

  it('signale une sous-charge sous 80 %', () => {
    expect(niveauDeCharge({ ...normale, tauxDeCharge: 0.79 })).toBe('sous-charge');
    expect(niveauDeCharge({ ...normale, tauxDeCharge: 0.8 })).toBe('normale');
  });

  it('relaie la surcharge calculée par l’API, sans comparer le taux (RG-24)', () => {
    expect(niveauDeCharge({ ...normale, tauxDeCharge: 1.33, surcharge: true })).toBe('surcharge');
    expect(niveauDeCharge({ ...normale, tauxDeCharge: 1.33 })).toBe('normale');
  });

  it('relaie l’intercontrat calculé par l’API (RG-25), même avec un taux non nul', () => {
    expect(niveauDeCharge({ ...normale, tauxDeCharge: 0.4, intercontrat: true })).toBe(
      'intercontrat',
    );
  });

  it('affiche « absent » quand la capacité est nulle', () => {
    expect(niveauDeCharge({ ...normale, capacite: 0, tauxDeCharge: null })).toBe('indisponible');
  });

  it('donne un libellé à chaque niveau', () => {
    expect(LIBELLES_NIVEAU.surcharge).toBe('Surcharge');
    expect(LIBELLES_NIVEAU.intercontrat).toBe('Intercontrat');
  });
});
