import { Projet } from '../projets/projet';
import {
  Absence,
  Affectation,
  disponible,
  estAffecte,
  lignesDeLaFeuille,
  lignesDeLaSemaine,
  cle,
  DUREES,
  DUREES_D_UNE_DEMI_JOURNEE,
  joursAbsents,
  joursEnDemiAbsence,
  LigneDeSaisie,
  partAbsente,
  totalDeLaLigne,
  totalDuJour,
  versGrille,
  versSaisies,
} from './feuille-de-temps';

const lignes: LigneDeSaisie[] = [
  { projetId: 1, libelle: 'Refonte du portail client', du: '2026-04-20', au: '2026-12-18' },
  { projetId: 2, libelle: 'Formation interne', du: '2026-04-27', au: '2026-12-31' },
];

function absence(partiel: Partial<Absence>): Absence {
  return {
    id: 1,
    collaborateurId: 2,
    typeAbsenceId: 1,
    dateDebut: '2026-05-05',
    dateFin: '2026-05-05',
    debuteApresMidi: false,
    finitLeMatin: false,
    statut: 'Approuvee',
    ...partiel,
  };
}

describe('feuille-de-temps', () => {
  it('propose les quarts de journée (RG-30) et, pour une demi-journée, pas au-delà de 0,5', () => {
    expect(DUREES).toEqual([0, 0.25, 0.5, 0.75, 1]);
    expect(DUREES_D_UNE_DEMI_JOURNEE).toEqual([0, 0.25, 0.5]);
  });

  describe('versGrille et versSaisies', () => {
    it('range les saisies par case, puis les rend', () => {
      const saisies = [
        { projetId: 1, jour: '2026-05-04', duree: 0.5 },
        { projetId: 2, jour: '2026-05-05', duree: 1 },
      ];

      const grille = versGrille(saisies);

      expect(grille[cle(1, '2026-05-04')]).toBe(0.5);
      expect(versSaisies(grille)).toEqual(saisies);
    });

    it('n’envoie pas les cases à zéro', () => {
      const grille = { [cle(1, '2026-05-04')]: 0.5, [cle(1, '2026-05-05')]: 0 };

      expect(versSaisies(grille)).toEqual([{ projetId: 1, jour: '2026-05-04', duree: 0.5 }]);
    });
  });

  describe('totaux', () => {
    const grille = {
      [cle(1, '2026-05-04')]: 0.75,
      [cle(2, '2026-05-04')]: 0.5,
      [cle(1, '2026-05-05')]: 1,
    };

    it('additionne un jour sur toutes les lignes', () => {
      expect(totalDuJour(grille, lignes, '2026-05-04')).toBe(1.25);
      expect(totalDuJour(grille, lignes, '2026-05-06')).toBe(0);
    });

    it('additionne une ligne sur les jours donnés', () => {
      const jours = ['2026-05-04', '2026-05-05', '2026-05-06'];

      expect(totalDeLaLigne(grille, lignes[0], jours)).toBe(1.75);
      expect(totalDeLaLigne(grille, lignes[1], jours)).toBe(0.5);
    });

    it('ignore les saisies d’un projet qui n’est pas une ligne', () => {
      expect(totalDuJour({ [cle(9, '2026-05-04')]: 1 }, lignes, '2026-05-04')).toBe(0);
    });
  });

  describe('absences (RG-32)', () => {
    const semaine = ['2026-05-04', '2026-05-05', '2026-05-06', '2026-05-07', '2026-05-08'];

    it('ferme le jour d’une absence approuvée', () => {
      expect(joursAbsents([absence({})], semaine)).toEqual(['2026-05-05']);
    });

    it('ferme chaque jour d’une absence de plusieurs jours', () => {
      const conges = absence({ dateDebut: '2026-05-05', dateFin: '2026-05-07' });

      expect(joursAbsents([conges], semaine)).toEqual(['2026-05-05', '2026-05-06', '2026-05-07']);
    });

    it('ignore une demande en attente, refusée ou annulée', () => {
      for (const statut of ['Demandee', 'Refusee', 'Annulee'] as const) {
        expect(partAbsente([absence({ statut })], '2026-05-05')).toBe(0);
      }
    });

    it('ne laisse qu’une demi-journée le jour où l’absence commence l’après-midi', () => {
      const conges = absence({
        dateDebut: '2026-05-05',
        dateFin: '2026-05-06',
        debuteApresMidi: true,
      });

      expect(partAbsente([conges], '2026-05-05')).toBe(0.5);
      expect(joursEnDemiAbsence([conges], semaine)).toEqual(['2026-05-05']);
      expect(joursAbsents([conges], semaine)).toEqual(['2026-05-06']);
    });

    it('ne laisse qu’une demi-journée le jour où l’absence finit le matin', () => {
      const conges = absence({
        dateDebut: '2026-05-05',
        dateFin: '2026-05-06',
        finitLeMatin: true,
      });

      expect(partAbsente([conges], '2026-05-06')).toBe(0.5);
      expect(joursEnDemiAbsence([conges], semaine)).toEqual(['2026-05-06']);
    });

    it('cumule deux demi-journées d’absence le même jour', () => {
      const apresMidi = absence({ dateFin: '2026-05-05', debuteApresMidi: true });
      const matin = absence({
        id: 2,
        dateDebut: '2026-05-04',
        dateFin: '2026-05-05',
        finitLeMatin: true,
      });

      expect(partAbsente([apresMidi, matin], '2026-05-05')).toBe(1);
    });

    it('ne dépasse jamais une journée', () => {
      expect(partAbsente([absence({}), absence({ id: 2 })], '2026-05-05')).toBe(1);
    });
  });

  describe('disponible', () => {
    it('vaut une journée sans absence, une demi-journée avec une demi-absence, rien un jour d’absence', () => {
      const conges = absence({
        dateDebut: '2026-05-27',
        dateFin: '2026-05-28',
        debuteApresMidi: true,
      });

      expect(disponible([], '2026-05-27')).toBe(1);
      expect(disponible([conges], '2026-05-27')).toBe(0.5);
      expect(disponible([conges], '2026-05-28')).toBe(0);
    });
  });

  describe('lignes de la semaine', () => {
    const semaine = [
      '2026-05-04',
      '2026-05-05',
      '2026-05-06',
      '2026-05-07',
      '2026-05-08',
      '2026-05-09',
      '2026-05-10',
    ];
    const projets = [
      { id: 1, libelle: 'Refonte' },
      { id: 2, libelle: 'Formation' },
    ] as Projet[];
    const affectation = (
      id: number,
      projetId: number,
      dateDebut: string,
      dateFin: string,
    ): Affectation => ({
      id,
      collaborateurId: 2,
      projetId,
      dateDebut,
      dateFin,
      tauxAllocation: 0.5,
    });

    it('garde les projets affectés pendant la semaine, avec leur libellé', () => {
      const lignes = lignesDeLaSemaine(
        [
          affectation(1, 1, '2026-04-20', '2026-12-18'),
          affectation(2, 2, '2026-01-05', '2026-03-31'),
          affectation(3, 9, '2026-05-10', '2026-06-01'),
        ],
        projets,
        semaine,
      );

      expect(lignes.map((l) => [l.projetId, l.libelle])).toEqual([
        [1, 'Refonte'],
        [9, 'Projet n° 9'],
      ]);
    });

    it('fusionne deux affectations d’un projet en une ligne, sans ouvrir le jour qui les sépare (RG-33)', () => {
      const [ligne, ...autres] = lignesDeLaSemaine(
        [
          affectation(1, 1, '2026-04-20', '2026-05-05'),
          affectation(2, 1, '2026-05-07', '2026-12-18'),
        ],
        projets,
        semaine,
      );

      expect(autres).toEqual([]);
      expect([ligne.du, ligne.au]).toEqual(['2026-04-20', '2026-12-18']);
      expect(estAffecte(ligne, '2026-05-05')).toBe(true);
      expect(estAffecte(ligne, '2026-05-06')).toBe(false);
      expect(estAffecte(ligne, '2026-05-07')).toBe(true);
    });

    it('n’ouvre une ligne sans périodes que sur son du et son au', () => {
      const ligne = { projetId: 1, libelle: 'Refonte', du: '2026-05-05', au: '2026-05-06' };

      expect(estAffecte(ligne, '2026-05-04')).toBe(false);
      expect(estAffecte(ligne, '2026-05-06')).toBe(true);
    });

    it('construit les lignes d’une feuille remplie : un projet par projet saisi', () => {
      const lignes = lignesDeLaFeuille(
        [
          { projetId: 2, jour: '2026-05-04', duree: 0.5 },
          { projetId: 2, jour: '2026-05-05', duree: 1 },
          { projetId: 1, jour: '2026-05-05', duree: 0.25 },
        ],
        projets,
        semaine,
      );

      expect(lignes.map((l) => [l.projetId, l.libelle, l.du, l.au])).toEqual([
        [2, 'Formation', '2026-05-04', '2026-05-10'],
        [1, 'Refonte', '2026-05-04', '2026-05-10'],
      ]);
    });
  });
});
