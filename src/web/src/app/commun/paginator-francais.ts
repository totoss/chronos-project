import { MatPaginatorIntl } from '@angular/material/paginator';

/** Les libellés du paginateur de Material, qui sont en anglais dans son code. */
export function paginatorFrancais(): MatPaginatorIntl {
  const intl = new MatPaginatorIntl();
  intl.itemsPerPageLabel = 'Lignes par page';
  intl.firstPageLabel = 'Première page';
  intl.previousPageLabel = 'Page précédente';
  intl.nextPageLabel = 'Page suivante';
  intl.lastPageLabel = 'Dernière page';
  intl.getRangeLabel = (page, taille, total) => {
    if (total === 0) {
      return 'Aucun résultat';
    }
    const debut = page * taille + 1;
    const fin = Math.min((page + 1) * taille, total);
    return `${debut} à ${fin} sur ${total}`;
  };
  return intl;
}
