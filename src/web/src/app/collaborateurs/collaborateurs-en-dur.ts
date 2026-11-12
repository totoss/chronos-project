import { Collaborateur } from './collaborateur';

export const COLLABORATEURS_EN_DUR: Collaborateur[] = [
  { id: 1, matricule: 'C0001', nom: 'Martin', prenom: 'Camille', email: 'camille.martin@chronos.example', categorie: 'Consultant', poste: 'Consultante confirmée', dateEntree: '2021-09-01', dateSortie: null, quotite: 0.8 },
  { id: 2, matricule: 'C0002', nom: 'Bernard', prenom: 'Louis', email: 'louis.bernard@chronos.example', categorie: 'Consultant', poste: 'Consultant junior', dateEntree: '2025-02-03', dateSortie: null, quotite: 1 },
  { id: 3, matricule: 'C0003', nom: 'Dubois', prenom: 'Inès', email: 'ines.dubois@chronos.example', categorie: 'Consultant', poste: 'Manager', dateEntree: '2018-04-16', dateSortie: null, quotite: 1 },
  { id: 4, matricule: 'I0001', nom: 'Marchand', prenom: 'Hugo', email: 'hugo.marchand@chronos.example', categorie: 'Interne', poste: 'Gestionnaire RH', dateEntree: '2019-01-07', dateSortie: null, quotite: 0.5 },
  { id: 5, matricule: 'C0004', nom: 'Petit', prenom: 'Sarah', email: 'sarah.petit@chronos.example', categorie: 'Consultant', poste: 'Consultante senior', dateEntree: '2017-11-02', dateSortie: '2026-03-31', quotite: 1 },
];
