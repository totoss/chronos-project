/**
 * Indique si `texte` contient `recherche`, sans tenir compte de la casse ni des accents.
 * Une recherche vide est contenue dans tout texte.
 */
export function contientSansAccents(texte: string, recherche: string): boolean {
  return normaliser(texte).includes(normaliser(recherche));
}

// "Inès" devient "ines" : `normalize('NFD')` sépare la lettre de son accent,
// puis on retire les accents (marques diacritiques U+0300 à U+036F).
function normaliser(valeur: string): string {
  return valeur
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase('fr');
}
