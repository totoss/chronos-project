import { ComponentFixture, TestBed } from '@angular/core/testing';
import { cle, Grille, LigneDeSaisie } from '../feuille-de-temps';
import { GrilleDeSaisie } from './grille-de-saisie';

const jours = [
  '2026-05-04',
  '2026-05-05',
  '2026-05-06',
  '2026-05-07',
  '2026-05-08',
  '2026-05-09',
  '2026-05-10',
];
const lignes: LigneDeSaisie[] = [
  { projetId: 1, libelle: 'Refonte du portail client', du: '2026-04-20', au: '2026-12-18' },
  { projetId: 2, libelle: 'Formation interne', du: '2026-05-06', au: '2026-12-31' },
];

describe('GrilleDeSaisie', () => {
  let fixture: ComponentFixture<GrilleDeSaisie>;
  let ecran: HTMLElement;

  async function afficher(grille: Grille = {}): Promise<void> {
    fixture.componentRef.setInput('jours', jours);
    fixture.componentRef.setInput('lignes', lignes);
    fixture.componentRef.setInput('grille', grille);
    await fixture.whenStable();
  }

  function liste(libelle: string): HTMLSelectElement {
    return ecran.querySelector<HTMLSelectElement>(`select[aria-label="${libelle}"]`)!;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [GrilleDeSaisie] });
    fixture = TestBed.createComponent(GrilleDeSaisie);
    ecran = fixture.nativeElement;
  });

  it('affiche une ligne par projet et une liste déroulante par jour', async () => {
    await afficher();

    expect(ecran.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(ecran.querySelectorAll('select')).toHaveLength(14);
    expect(liste('Refonte du portail client, lun. 4 mai')).toBeTruthy();
  });

  it('ne propose que des quarts de journée (RG-30)', async () => {
    await afficher();

    const valeurs = Array.from(liste('Refonte du portail client, lun. 4 mai').options).map(
      (option) => option.value,
    );
    expect(valeurs).toEqual(['0', '0.25', '0.5', '0.75', '1']);
  });

  it('calcule le total de chaque jour et de chaque ligne', async () => {
    await afficher({
      [cle(1, '2026-05-06')]: 0.5,
      [cle(2, '2026-05-06')]: 0.25,
      [cle(1, '2026-05-07')]: 1,
    });

    const totauxDuJour = Array.from(ecran.querySelectorAll('tfoot td')).map((td) =>
      td.textContent?.trim(),
    );
    expect(totauxDuJour[2]).toBe('0.75');
    expect(totauxDuJour[3]).toBe('1');
    const totalLigne1 = ecran.querySelector('tbody tr:first-child td:last-child');
    expect(totalLigne1?.textContent?.trim()).toBe('1.5');
  });

  it('signale par un texte, et pas seulement par la couleur, un total supérieur à 1 (RG-31)', async () => {
    await afficher({ [cle(1, '2026-05-06')]: 0.75, [cle(2, '2026-05-06')]: 0.5 });

    expect(ecran.querySelectorAll('.alerte')).toHaveLength(1);
    expect(ecran.querySelector('.alerte')?.textContent).toContain("Plus d'un jour");
    expect(ecran.querySelector('tfoot .depassement')).toBeTruthy();
  });

  it('signale le dépassement d’une demi-journée un jour d’absence partielle', async () => {
    fixture.componentRef.setInput('joursEnDemiAbsence', new Set(['2026-05-06']));
    await afficher({ [cle(1, '2026-05-06')]: 0.5, [cle(2, '2026-05-06')]: 0.25 });

    expect(ecran.querySelector('.alerte')?.textContent).toContain("Plus d'une demi-journée");
    expect(ecran.querySelector('tfoot .depassement')).toBeTruthy();
  });

  it('accepte une demi-journée un jour d’absence partielle', async () => {
    fixture.componentRef.setInput('joursEnDemiAbsence', new Set(['2026-05-06']));
    await afficher({ [cle(1, '2026-05-06')]: 0.25, [cle(2, '2026-05-06')]: 0.25 });

    expect(ecran.querySelector('.alerte')).toBeNull();
  });

  it('ferme les jours fermés (RG-32) et les jours hors affectation (RG-33)', async () => {
    fixture.componentRef.setInput(
      'joursFermes',
      new Set(['2026-05-08', '2026-05-09', '2026-05-10']),
    );
    await afficher();

    expect(liste('Refonte du portail client, ven. 8 mai').disabled).toBe(true);
    expect(liste('Refonte du portail client, mar. 5 mai').disabled).toBe(false);
    // L'affectation à « Formation interne » ne commence que le mercredi.
    expect(liste('Formation interne, mar. 5 mai').disabled).toBe(true);
    expect(liste('Formation interne, mer. 6 mai').disabled).toBe(false);
  });

  it('ne propose qu’une demi-journée au plus un jour d’absence partielle', async () => {
    fixture.componentRef.setInput('joursEnDemiAbsence', new Set(['2026-05-05']));
    await afficher();

    const valeurs = Array.from(liste('Refonte du portail client, mar. 5 mai').options).map(
      (option) => option.value,
    );
    expect(valeurs).toEqual(['0', '0.25', '0.5']);
  });

  it('ferme toutes les listes en lecture seule (RG-35)', async () => {
    fixture.componentRef.setInput('lectureSeule', true);
    await afficher();

    const listes = Array.from(ecran.querySelectorAll('select'));
    expect(listes.every((l) => l.disabled)).toBe(true);
  });

  it('met à jour la grille quand on choisit une durée, sans modifier l’ancienne', async () => {
    const avant: Grille = {};
    await afficher(avant);

    const choix = liste('Refonte du portail client, lun. 4 mai');
    choix.value = '0.75';
    choix.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    expect(fixture.componentInstance.grille()).toEqual({ [cle(1, '2026-05-04')]: 0.75 });
    expect(avant).toEqual({});
  });

  it('annonce l’absence d’affectation', async () => {
    fixture.componentRef.setInput('jours', jours);
    fixture.componentRef.setInput('lignes', []);
    fixture.componentRef.setInput('grille', {});
    await fixture.whenStable();

    expect(ecran.textContent).toContain('Aucune affectation sur cette semaine.');
  });
});
