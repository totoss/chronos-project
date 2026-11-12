import { HttpClient, HttpContext, httpResource } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { ERREURS_DE_SAISIE_GEREES } from '../commun/erreurs-interceptor';
import { Page } from '../commun/page';
import {
  Collaborateur,
  CreerCollaborateurRequete,
  ModifierCollaborateurRequete,
} from './collaborateur';

/** Les tris acceptés par l'API ; `-nom` est l'ordre décroissant. */
export type TriCollaborateurs = 'nom' | '-nom' | 'matricule';

export interface CriteresCollaborateurs {
  page: number;
  taille: number;
  recherche: string;
  avecSortis: boolean;
  tri: TriCollaborateurs;
}

@Service()
export class CollaborateursApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/collaborateurs';

  /** Les formulaires affichent eux-mêmes les erreurs de saisie. */
  private readonly depuisUnFormulaire = {
    context: new HttpContext().set(ERREURS_DE_SAISIE_GEREES, true),
  };

  /** À appeler dans un contexte d'injection : l'initialiseur d'un champ de composant. */
  liste(criteres: () => CriteresCollaborateurs) {
    return httpResource<Page<Collaborateur>>(() => {
      const { page, taille, recherche, avecSortis, tri } = criteres();
      return {
        url: this.url,
        // Sans `statut`, l'API renvoie les actifs et les sortis.
        params: { page, taille, recherche, tri, ...(avecSortis ? {} : { statut: 'actif' }) },
      };
    });
  }

  /** Tant que `id` renvoie `undefined`, aucune requête ne part. */
  parId(id: () => number | undefined) {
    return httpResource<Collaborateur>(() => {
      const valeur = id();
      return valeur === undefined ? undefined : `${this.url}/${valeur}`;
    });
  }

  creer(requete: CreerCollaborateurRequete): Observable<Collaborateur> {
    return this.http.post<Collaborateur>(this.url, requete, this.depuisUnFormulaire);
  }

  modifier(id: number, requete: ModifierCollaborateurRequete): Observable<Collaborateur> {
    return this.http.put<Collaborateur>(`${this.url}/${id}`, requete, this.depuisUnFormulaire);
  }

  supprimer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
