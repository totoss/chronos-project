import { httpResource } from '@angular/common/http';
import { Service } from '@angular/core';
import { Page } from '../commun/page';
import { Projet, StatutProjet } from './projet';

export interface CriteresProjets {
  page: number;
  taille: number;
  recherche: string;
  /** Chaîne vide : tous les statuts. */
  statut: StatutProjet | '';
}

@Service()
export class ProjetsApi {
  /** À appeler dans un contexte d'injection : l'initialiseur d'un champ de composant. */
  liste(criteres: () => CriteresProjets) {
    return httpResource<Page<Projet>>(() => {
      const { page, taille, recherche, statut } = criteres();
      return {
        url: '/api/projets',
        params: { page, taille, recherche, tri: 'code', ...(statut === '' ? {} : { statut }) },
      };
    });
  }
}
