import { Projet } from '../projets/projet';

export type StatutFeuilleDeTemps = 'Brouillon' | 'Soumise' | 'Validee';

/** L'identifiant de l'API n'est pas un texte pour l'utilisateur. */
export const LIBELLES_STATUT: Record<StatutFeuilleDeTemps, string> = {
  Brouillon: 'Brouillon',
  Soumise: 'Soumise',
  Validee: 'Validée',
};

/** Miroir de SaisieDto (module 10). `jour` est une date ISO ('2026-05-04'), `duree` une fraction de journée. */
export interface SaisieDeTemps {
  projetId: number;
  jour: string;
  duree: number;
}

/** Miroir de FeuilleDeTempsDto (module 10). */
export interface FeuilleDeTemps {
  id: number;
  collaborateurId: number;
  /** Le lundi de la semaine. */
  semaine: string;
  statut: StatutFeuilleDeTemps;
  /** Renseigné quand le manager a rejeté la feuille : elle est alors revenue en brouillon (RG-34). */
  motifRejet: string | null;
  saisies: SaisieDeTemps[];
}

/** Miroir de AffectationDto (module 09). */
export interface Affectation {
  id: number;
  collaborateurId: number;
  projetId: number;
  dateDebut: string;
  dateFin: string;
  /** Une fraction : 0.6 pour 60 %. */
  tauxAllocation: number;
}

/** Miroir de JourFerieDto (module 10). */
export interface JourFerie {
  date: string;
  libelle: string;
}

export type StatutAbsence = 'Demandee' | 'Approuvee' | 'Refusee' | 'Annulee';

/** Miroir de AbsenceDto (module 10). */
export interface Absence {
  id: number;
  collaborateurId: number;
  typeAbsenceId: number;
  dateDebut: string;
  dateFin: string;
  /** Le premier jour, le collaborateur travaille le matin. */
  debuteApresMidi: boolean;
  /** Le dernier jour, le collaborateur travaille l'après-midi. */
  finitLeMatin: boolean;
  statut: StatutAbsence;
}

/** Une ligne de la grille : un projet sur lequel le collaborateur peut saisir cette semaine. */
export interface LigneDeSaisie {
  projetId: number;
  libelle: string;
  /** Période de l'affectation : on ne saisit pas en dehors (RG-33). */
  du: string;
  au: string;
  /** Les périodes d'affectation, quand le projet en a plusieurs : entre deux, on ne saisit pas. */
  periodes?: { du: string; au: string }[];
}

/** Les durées saisies, rangées par case : la clé est « projetId|jour ». */
export type Grille = Record<string, number>;

/** RG-30 : les durées que l'on peut choisir, 0 pour une case vide. */
export const DUREES = [0, 0.25, 0.5, 0.75, 1];

/** Un jour où une demi-journée d'absence approuvée reste à travailler : au plus une demi-journée. */
export const DUREES_D_UNE_DEMI_JOURNEE = DUREES.filter((duree) => duree <= 0.5);

export function cle(projetId: number, jour: string): string {
  return `${projetId}|${jour}`;
}

export function versGrille(saisies: SaisieDeTemps[]): Grille {
  const grille: Grille = {};
  for (const saisie of saisies) {
    grille[cle(saisie.projetId, saisie.jour)] = saisie.duree;
  }
  return grille;
}

export function versSaisies(grille: Grille): SaisieDeTemps[] {
  return Object.entries(grille)
    .filter(([, duree]) => duree > 0)
    .map(([cleDeCase, duree]) => {
      const [projetId, jour] = cleDeCase.split('|');
      return { projetId: Number(projetId), jour, duree };
    });
}

export function totalDuJour(grille: Grille, lignes: LigneDeSaisie[], jour: string): number {
  return lignes.reduce((total, ligne) => total + (grille[cle(ligne.projetId, jour)] ?? 0), 0);
}

export function totalDeLaLigne(grille: Grille, ligne: LigneDeSaisie, jours: string[]): number {
  return jours.reduce((total, jour) => total + (grille[cle(ligne.projetId, jour)] ?? 0), 0);
}

/**
 * RG-32 : la part du jour dont les absences approuvées privent le collaborateur
 * (0 : aucune, 0,5 : une demi-journée, 1 : la journée entière).
 * Seule une absence approuvée compte : une demande en attente ne ferme rien.
 */
export function partAbsente(absences: Absence[], jour: string): number {
  let part = 0;
  for (const absence of absences) {
    if (absence.statut !== 'Approuvee' || jour < absence.dateDebut || jour > absence.dateFin) {
      continue;
    }
    part += 1;
    if (jour === absence.dateDebut && absence.debuteApresMidi) part -= 0.5;
    if (jour === absence.dateFin && absence.finitLeMatin) part -= 0.5;
  }
  return Math.min(part, 1);
}

/** Les jours entièrement couverts par une absence approuvée. */
export function joursAbsents(absences: Absence[], jours: string[]): string[] {
  return jours.filter((jour) => partAbsente(absences, jour) >= 1);
}

/** Les jours où une demi-journée seulement est couverte par une absence approuvée. */
export function joursEnDemiAbsence(absences: Absence[], jours: string[]): string[] {
  return jours.filter((jour) => {
    const part = partAbsente(absences, jour);
    return part > 0 && part < 1;
  });
}

function libelleDuProjet(projets: Projet[], projetId: number): string {
  return projets.find((projet) => projet.id === projetId)?.libelle ?? `Projet n° ${projetId}`;
}

/**
 * RG-33 : une ligne par projet auquel le collaborateur est affecté pendant la semaine (les sept `jours`).
 * Un projet qui a deux affectations dans la semaine n'a qu'une ligne, qui couvre les deux.
 */
export function lignesDeLaSemaine(
  affectations: Affectation[],
  projets: Projet[],
  jours: string[],
): LigneDeSaisie[] {
  const lundi = jours[0];
  const dimanche = jours[jours.length - 1];
  const lignes = new Map<number, LigneDeSaisie>();

  for (const affectation of affectations) {
    if (affectation.dateDebut > dimanche || affectation.dateFin < lundi) continue;
    const periode = { du: affectation.dateDebut, au: affectation.dateFin };
    const ligne = lignes.get(affectation.projetId);
    const periodes = [...(ligne?.periodes ?? []), periode];
    lignes.set(affectation.projetId, {
      projetId: affectation.projetId,
      libelle: libelleDuProjet(projets, affectation.projetId),
      du: ligne && ligne.du < periode.du ? ligne.du : periode.du,
      au: ligne && ligne.au > periode.au ? ligne.au : periode.au,
      periodes,
    });
  }
  return [...lignes.values()];
}

/** RG-33 : le jour est couvert par au moins une affectation du projet. */
export function estAffecte(ligne: LigneDeSaisie, jour: string): boolean {
  return (ligne.periodes ?? [{ du: ligne.du, au: ligne.au }]).some(
    (periode) => periode.du <= jour && jour <= periode.au,
  );
}

/** Ce qui reste à saisir un jour, sur l'ensemble des projets : une journée moins les absences approuvées. */
export function disponible(absences: Absence[], jour: string): number {
  return 1 - partAbsente(absences, jour);
}

/** Les lignes d'une feuille déjà remplie : un projet par projet saisi, sur toute la semaine. */
export function lignesDeLaFeuille(
  saisies: SaisieDeTemps[],
  projets: Projet[],
  jours: string[],
): LigneDeSaisie[] {
  const projetIds = [...new Set(saisies.map((saisie) => saisie.projetId))];
  return projetIds.map((projetId) => ({
    projetId,
    libelle: libelleDuProjet(projets, projetId),
    du: jours[0],
    au: jours[jours.length - 1],
  }));
}
