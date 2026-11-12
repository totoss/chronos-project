import { HttpClient, HttpContext, httpResource } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { ERREURS_DE_SAISIE_GEREES } from '../commun/erreurs-interceptor';
import { Page } from '../commun/page';
import { CreerProjetRequete, Projet, StatutProjet } from './projet';

export interface CriteresProjets {
  page: number;
  taille: number;
  recherche: string;
  /** Chaîne vide : tous les statuts. */
  statut: StatutProjet | '';
}

@Service()
export class ProjetsApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/projets';

  /** À appeler dans un contexte d'injection : l'initialiseur d'un champ de composant. */
  liste(criteres: () => CriteresProjets) {
    return httpResource<Page<Projet>>(() => {
      const { page, taille, recherche, statut } = criteres();
      return {
        url: this.url,
        params: { page, taille, recherche, tri: 'code', ...(statut === '' ? {} : { statut }) },
      };
    });
  }

  /** Le formulaire affiche lui-même les erreurs de saisie. */
  creer(requete: CreerProjetRequete): Observable<Projet> {
    return this.http.post<Projet>(this.url, requete, {
      context: new HttpContext().set(ERREURS_DE_SAISIE_GEREES, true),
    });
  }
}
