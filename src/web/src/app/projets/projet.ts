export type NatureProjet = 'Facturable' | 'Interne';
export type StatutProjet = 'EnPreparation' | 'EnCours' | 'Termine' | 'Annule';

/** Miroir de ProjetDto (modules 08 et 09). Les dates sont des chaînes ISO ('2026-03-02'). */
export interface Projet {
  id: number;
  code: string;
  libelle: string;
  nature: NatureProjet;
  /** Absents pour un projet interne (RG-11). */
  clientId: number | null;
  clientNom: string | null;
  dateDebut: string;
  dateFin: string | null;
  budgetJours: number;
  responsableId: number;
  responsableNom: string;
  statut: StatutProjet;
  /** Jeton de concurrence, à renvoyer tel quel dans une modification. */
  version: string;
  /** Jours de travail validés sur le projet, en jours (fractions possibles). */
  joursValides: number;
}
