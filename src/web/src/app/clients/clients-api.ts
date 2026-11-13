import { httpResource } from '@angular/common/http';
import { Service } from '@angular/core';
import { Client } from './client';

@Service()
export class ClientsApi {
  /** À appeler dans un contexte d'injection : l'initialiseur d'un champ de composant. */
  liste() {
    // L'API renvoie ici un tableau simple, sans pagination.
    return httpResource<Client[]>(() => '/api/clients');
  }
}
