import { MatPaginatorIntl } from '@angular/material/paginator';
import { paginatorFrancais } from './paginator-francais';

describe('paginatorFrancais', () => {
  let intl: MatPaginatorIntl;

  beforeEach(() => {
    intl = paginatorFrancais();
  });

  it('traduit les libellés du paginateur', () => {
    expect(intl.itemsPerPageLabel).toBe('Lignes par page');
    expect(intl.nextPageLabel).toBe('Page suivante');
    expect(intl.previousPageLabel).toBe('Page précédente');
  });

  it('dit quelles lignes sont affichées', () => {
    expect(intl.getRangeLabel(0, 20, 45)).toBe('1 à 20 sur 45');
    expect(intl.getRangeLabel(2, 20, 45)).toBe('41 à 45 sur 45');
  });

  it('dit qu’il n’y a aucun résultat', () => {
    expect(intl.getRangeLabel(0, 20, 0)).toBe('Aucun résultat');
  });
});
