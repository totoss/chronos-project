import {
  ajouterJours,
  aujourdhui,
  estWeekEnd,
  finDuMois,
  libelleJour,
  libelleMois,
  lundiDe,
} from './dates';

describe('dates', () => {
  describe('lundiDe', () => {
    it('renvoie le même jour pour un lundi', () => {
      expect(lundiDe('2026-05-04')).toBe('2026-05-04');
    });

    it('recule jusqu’au lundi pour un jour de la semaine', () => {
      expect(lundiDe('2026-05-06')).toBe('2026-05-04');
    });

    it('rattache le dimanche à la semaine qui finit, pas à la suivante', () => {
      expect(lundiDe('2026-05-10')).toBe('2026-05-04');
    });

    it('passe à l’année précédente quand la semaine y commence', () => {
      expect(lundiDe('2026-01-01')).toBe('2025-12-29');
    });
  });

  describe('ajouterJours', () => {
    it('avance malgré le passage à l’heure d’été européenne (29 mars 2026)', () => {
      expect(ajouterJours('2026-03-28', 2)).toBe('2026-03-30');
      expect(ajouterJours('2026-03-23', 7)).toBe('2026-03-30');
    });

    it('avance malgré le passage à l’heure d’hiver européenne (25 octobre 2026)', () => {
      expect(ajouterJours('2026-10-24', 2)).toBe('2026-10-26');
      expect(ajouterJours('2026-10-19', 7)).toBe('2026-10-26');
    });

    it('avance malgré les changements d’heure américains (8 mars et 1er novembre 2026)', () => {
      expect(ajouterJours('2026-03-07', 2)).toBe('2026-03-09');
      expect(ajouterJours('2026-10-31', 2)).toBe('2026-11-02');
    });

    it('change d’année dans les deux sens', () => {
      expect(ajouterJours('2025-12-29', 7)).toBe('2026-01-05');
      expect(ajouterJours('2026-01-01', -1)).toBe('2025-12-31');
    });

    it('connaît les années bissextiles', () => {
      expect(ajouterJours('2028-02-28', 1)).toBe('2028-02-29');
      expect(ajouterJours('2026-02-28', 1)).toBe('2026-03-01');
    });

    it('ne change rien pour zéro jour', () => {
      expect(ajouterJours('2026-05-04', 0)).toBe('2026-05-04');
    });
  });

  describe('estWeekEnd', () => {
    it('reconnaît le samedi et le dimanche', () => {
      expect(estWeekEnd('2026-05-09')).toBe(true);
      expect(estWeekEnd('2026-05-10')).toBe(true);
    });

    it('ne reconnaît ni le lundi ni le vendredi', () => {
      expect(estWeekEnd('2026-05-04')).toBe(false);
      expect(estWeekEnd('2026-05-08')).toBe(false);
    });
  });

  describe('libelleJour', () => {
    it('écrit le jour en français, sans décalage', () => {
      expect(libelleJour('2026-05-04')).toBe('lun. 4 mai');
      expect(libelleJour('2026-01-01')).toBe('jeu. 1 janv.');
    });
  });

  describe('finDuMois', () => {
    it('donne le dernier jour du mois de la date', () => {
      expect(finDuMois('2026-05-04')).toBe('2026-05-31');
      expect(finDuMois('2026-04-30')).toBe('2026-04-30');
    });

    it('connaît février, bissextile ou non', () => {
      expect(finDuMois('2026-02-10')).toBe('2026-02-28');
      expect(finDuMois('2028-02-10')).toBe('2028-02-29');
    });

    it('décale de quelques mois, et d’une année quand il le faut', () => {
      expect(finDuMois('2026-05-04', 3)).toBe('2026-08-31');
      expect(finDuMois('2026-12-30', 3)).toBe('2027-03-31');
      expect(finDuMois('2026-01-31', 1)).toBe('2026-02-28');
    });
  });

  describe('libelleMois', () => {
    it('écrit le mois en toutes lettres, avec l’année', () => {
      expect(libelleMois('2026-05-04')).toBe('mai 2026');
      expect(libelleMois('2026-12-31')).toBe('décembre 2026');
    });
  });

  describe('aujourdhui', () => {
    afterEach(() => {
      vi.useRealTimers();
    });

    it('donne le jour du calendrier local, même peu après minuit', () => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date(2026, 4, 6, 0, 30));

      expect(aujourdhui()).toBe('2026-05-06');
    });

    it('donne le jour du calendrier local, même peu avant minuit', () => {
      vi.useFakeTimers();
      vi.setSystemTime(new Date(2026, 11, 31, 23, 45));

      expect(aujourdhui()).toBe('2026-12-31');
    });
  });
});
