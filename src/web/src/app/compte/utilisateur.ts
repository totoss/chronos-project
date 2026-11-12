export type Role = 'Collaborateur' | 'Manager' | 'GestionnaireRh' | 'Administrateur';

/** Miroir de la réponse de GET /api/compte/moi (module 12). */
export interface Utilisateur {
  /** `null` pour un compte sans fiche de collaborateur, celui d'un administrateur par exemple. */
  collaborateurId: number | null;
  nom: string | null;
  prenom: string | null;
  email: string;
  roles: Role[];
}
