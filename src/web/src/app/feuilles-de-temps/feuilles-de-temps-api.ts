import { HttpClient, httpResource } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { Page } from '../commun/page';
import { Projet } from '../projets/projet';
import { Absence, Affectation, FeuilleDeTemps, JourFerie, SaisieDeTemps } from './feuille-de-temps';

@Service()
export class FeuillesDeTempsApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/feuilles-de-temps';

  /** La feuille d'un collaborateur pour une semaine ; l'API la crée en brouillon si besoin. */
  deLaSemaine(collaborateurId: () => number, semaine: () => string) {
    return httpResource<FeuilleDeTemps>(() => ({
      url: this.url,
      params: { collaborateurId: collaborateurId(), semaine: semaine() },
    }));
  }

  affectations(collaborateurId: () => number) {
    return httpResource<Affectation[]>(
      () => ({ url: '/api/affectations', params: { collaborateurId: collaborateurId() } }),
      { defaultValue: [] },
    );
  }

  /** Toutes les absences du collaborateur : l'écran ne garde que les approuvées (RG-32). */
  absences(collaborateurId: () => number) {
    return httpResource<Absence[]>(
      () => ({ url: '/api/absences', params: { collaborateurId: collaborateurId() } }),
      { defaultValue: [] },
    );
  }

  projets() {
    return httpResource<Page<Projet>>(() => ({ url: '/api/projets', params: { taille: 100 } }));
  }

  joursFeries() {
    return httpResource<JourFerie[]>(() => '/api/jours-feries', { defaultValue: [] });
  }

  /** Les feuilles soumises que l'utilisateur connecté peut valider : son équipe, ou tous si RH. */
  soumises() {
    return httpResource<FeuilleDeTemps[]>(() => `${this.url}/soumises`, { defaultValue: [] });
  }

  enregistrer(id: number, saisies: SaisieDeTemps[]): Observable<FeuilleDeTemps> {
    return this.http.put<FeuilleDeTemps>(`${this.url}/${id}/saisies`, { saisies });
  }

  soumettre(id: number): Observable<FeuilleDeTemps> {
    return this.http.post<FeuilleDeTemps>(`${this.url}/${id}/soumission`, null);
  }

  /** Le valideur est l'utilisateur connecté : la requête n'a pas de corps. */
  valider(id: number): Observable<FeuilleDeTemps> {
    return this.http.post<FeuilleDeTemps>(`${this.url}/${id}/validation`, null);
  }

  rejeter(id: number, motif: string): Observable<FeuilleDeTemps> {
    return this.http.post<FeuilleDeTemps>(`${this.url}/${id}/rejet`, { motif });
  }
}
