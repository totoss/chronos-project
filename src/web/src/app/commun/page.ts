/** Miroir de PageDto<T> : une page de résultats, numérotée à partir de 1. */
export interface Page<T> {
  elements: T[];
  page: number;
  taille: number;
  /** Nombre d'éléments de toute la sélection, pas seulement de cette page. */
  total: number;
}
