import { Component, computed, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { Projet } from '../projet';

@Component({
  imports: [MatCardModule],
  selector: 'app-carte-projet',
  styleUrl: './carte-projet.scss',
  templateUrl: './carte-projet.html',
})
export class CarteProjet {
  readonly projet = input.required<Projet>();

  protected readonly estClos = computed(
    () => this.projet().statut === 'Termine' || this.projet().statut === 'Annule',
  );
}
