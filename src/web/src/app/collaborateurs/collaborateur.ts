export interface Collaborateur {
  id: number;
  matricule: string;
  nom: string;
  prenom: string;
  email: string;
  categorie: 'Consultant' | 'Interne';
  poste: string;
  dateEntree: string;
  dateSortie: string | null;
}
