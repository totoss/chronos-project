export interface Projet {
  code: string;
  libelle: string;
  nature: 'Facturable' | 'Interne';
  statut: 'EnPreparation' | 'EnCours' | 'Termine' | 'Annule';
  client: string | null;
}
