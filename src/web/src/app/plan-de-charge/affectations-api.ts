import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import { ERREURS_DE_SAISIE_GEREES } from '../commun/erreurs-interceptor';
import { Affectation } from '../feuilles-de-temps/feuille-de-temps';

/** Miroir de CreerAffectationRequete (module 09). */
export interface CreerAffectationRequete {
  collaborateurId: number;
  projetId: number;
  dateDebut: string;
  dateFin: string;
  /** Une fraction : 0.6 pour 60 %. */
  tauxAllocation: number;
  /** RG-24 : sans elle, l'API refuse (422) une affectation qui met le collaborateur en surcharge. */
  surchargeConfirmee: boolean;
}

@Service()
export class AffectationsApi {
  private readonly http = inject(HttpClient);

  /** Le formulaire affiche lui-même les erreurs de saisie, y compris le 422 de la surcharge (RG-24). */
  creer(requete: CreerAffectationRequete): Observable<Affectation> {
    return this.http.post<Affectation>('/api/affectations', requete, {
      context: new HttpContext().set(ERREURS_DE_SAISIE_GEREES, true),
    });
  }
}
