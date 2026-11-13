export type CategorieCollaborateur = 'Consultant' | 'Interne';

/** Miroir de CollaborateurDto et de CollaborateurRhDto (modules 08 et 12). */
export interface Collaborateur {
  id: number;
  matricule: string;
  nom: string;
  prenom: string;
  email: string;
  categorie: CategorieCollaborateur;
  poste: string;
  managerId: number | null;
  managerNom: string | null;
  /**
   * Informations contractuelles (RG-52), réservées au gestionnaire RH et à l'intéressé.
   * Dates au format ISO ('2026-05-04') ; quotité du contrat en cours, en fraction : 0.8 pour 80 %.
   */
  dateEntree?: string;
  dateSortie?: string | null;
  quotite?: number;
  /** Vrai quand la date de sortie est strictement passée ; présent dans la liste comme dans la fiche. */
  estSorti: boolean;
  /** Jeton de concurrence, à renvoyer tel quel dans une modification. */
  version: string;
}

export type TypeContrat = 'Cdi' | 'Cdd' | 'Alternance' | 'Stage';

/** Miroir de CreerCollaborateurRequete. */
export interface CreerCollaborateurRequete {
  matricule: string;
  nom: string;
  prenom: string;
  email: string;
  categorie: CategorieCollaborateur;
  poste: string;
  dateEntree: string;
  typeContrat: TypeContrat;
  /** Fraction : 0.8 pour 80 %. */
  quotite: number;
  managerId: number | null;
}

/** Miroir de ModifierCollaborateurRequete. */
export interface ModifierCollaborateurRequete {
  nom: string;
  prenom: string;
  email: string;
  poste: string;
  dateSortie: string | null;
  managerId: number | null;
  version: string;
}
