import { SelectionSemaine } from './selection-semaine';

describe('SelectionSemaine', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 4, 6, 10, 30));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("démarre sur le lundi de la semaine d'aujourd'hui", () => {
    const selection = new SelectionSemaine();

    expect(selection.semaine()).toBe('2026-05-04');
    expect(selection.jours()).toHaveLength(7);
    expect(selection.jours()[6]).toBe('2026-05-10');
  });

  it('recalcule les jours quand on avance', () => {
    const selection = new SelectionSemaine();

    selection.decaler(1);

    expect(selection.semaine()).toBe('2026-05-11');
    expect(selection.jours()[0]).toBe('2026-05-11');
  });

  it('recule aussi, et change de mois ou d’année sans se tromper', () => {
    const selection = new SelectionSemaine();

    selection.decaler(-1);
    expect(selection.semaine()).toBe('2026-04-27');

    selection.allerA('2026-12-30');
    selection.decaler(1);
    expect(selection.semaine()).toBe('2027-01-04');
    expect(selection.jours()[0]).toBe('2027-01-04');
  });

  it('se cale sur le lundi de la date demandée', () => {
    const selection = new SelectionSemaine();

    selection.allerA('2026-05-13');

    expect(selection.semaine()).toBe('2026-05-11');
  });

  it('se cale sur le lundi précédent pour un dimanche', () => {
    const selection = new SelectionSemaine();

    selection.allerA('2026-05-17');

    expect(selection.semaine()).toBe('2026-05-11');
  });
});
